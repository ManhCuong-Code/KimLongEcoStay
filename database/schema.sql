-- HỆ THỐNG CƠ SỞ DỮ LIỆU: KIM LONG ECOSTAY (HỆ THỐNG ĐẶT PHÒNG TRỰC TUYẾN)
-- CHUẨN ĐÁNH GIÁ (K2.1, K2.2, K2.3) | CHUẨN HÓA: 3NF | HEADER-LINE: Bookings - BookingNights
-- Hệ Quản Trị CSDL: Microsoft SQL Server 2019 / 2022 / Azure SQL

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'KimLongEcoStayDB')
BEGIN
    CREATE DATABASE KimLongEcoStayDB COLLATE Vietnamese_CI_AS;
END
GO

USE KimLongEcoStayDB;
GO

-- ==========================================================================================
-- 0. XÓA BẢNG CŨ NẾU TỒN TẠI (THEO THỨ TỰ PHỤ THUỘ KHÓA NGOẠI)
-- ==========================================================================================
IF OBJECT_ID(N'dbo.BookingNights', N'U') IS NOT NULL DROP TABLE dbo.BookingNights;
IF OBJECT_ID(N'dbo.Bookings', N'U') IS NOT NULL DROP TABLE dbo.Bookings;
IF OBJECT_ID(N'dbo.RatePlans', N'U') IS NOT NULL DROP TABLE dbo.RatePlans;
IF OBJECT_ID(N'dbo.Rooms', N'U') IS NOT NULL DROP TABLE dbo.Rooms;
IF OBJECT_ID(N'dbo.RoomTypes', N'U') IS NOT NULL DROP TABLE dbo.RoomTypes;
IF OBJECT_ID(N'dbo.Guests', N'U') IS NOT NULL DROP TABLE dbo.Guests;
IF OBJECT_ID(N'dbo.AppUsers', N'U') IS NOT NULL DROP TABLE dbo.AppUsers;
GO

-- ==========================================================================================
-- 1. BẢNG TÀI KHOẢN QUẢN TRỊ (Phục vụ K4.1, K4.2: Phân quyền Host / Lễ tân [Authorize])
-- ==========================================================================================
CREATE TABLE dbo.AppUsers (
    Id INT IDENTITY(1,1) CONSTRAINT PK_AppUsers PRIMARY KEY,
    Username VARCHAR(50) NOT NULL CONSTRAINT UQ_AppUsers_Username UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(100) NULL,
    Role VARCHAR(20) NOT NULL CONSTRAINT CK_AppUsers_Role CHECK (Role IN ('Admin', 'Host', 'Staff', 'Customer')),
    IsActive BIT NOT NULL CONSTRAINT DF_AppUsers_IsActive DEFAULT (1),
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppUsers_CreatedAt DEFAULT (SYSUTCDATETIME())
);
GO

-- ==========================================================================================
-- 2. BẢNG KHÁCH HÀNG (Guests) - Chuẩn 3NF (Tách riêng khỏi đơn đặt phòng)
-- ==========================================================================================
CREATE TABLE dbo.Guests (
    Id INT IDENTITY(1,1) CONSTRAINT PK_Guests PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    PhoneNumber VARCHAR(20) NOT NULL,
    Email VARCHAR(100) NULL,
    IdentityCard VARCHAR(30) NULL, -- CCCD / Passport (dùng khi làm thủ tục check-in)
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Guests_CreatedAt DEFAULT (SYSUTCDATETIME())
);
GO

CREATE INDEX IX_Guests_PhoneNumber ON dbo.Guests (PhoneNumber);
GO

-- ==========================================================================================
-- 3. BẢNG LOẠI PHÒNG (RoomTypes)
-- ==========================================================================================
CREATE TABLE dbo.RoomTypes (
    Id INT IDENTITY(1,1) CONSTRAINT PK_RoomTypes PRIMARY KEY,
    TypeName NVARCHAR(100) NOT NULL CONSTRAINT UQ_RoomTypes_TypeName UNIQUE,
    Description NVARCHAR(MAX) NULL,
    MaxGuests INT NOT NULL CONSTRAINT CK_RoomTypes_MaxGuests CHECK (MaxGuests > 0),
    BasePrice DECIMAL(18,2) NOT NULL CONSTRAINT CK_RoomTypes_BasePrice CHECK (BasePrice >= 0),
    Amenities NVARCHAR(MAX) NULL, -- Danh sách tiện nghi (Điều hòa, Bồn tắm sỏi, Trà cung đình...)
    ImageUrl NVARCHAR(500) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_RoomTypes_IsActive DEFAULT (1)
);
GO

-- ==========================================================================================
-- 4. BẢNG PHÒNG CỤ THỂ (Rooms) - Khóa ngoại liên kết RoomTypes
-- ==========================================================================================
CREATE TABLE dbo.Rooms (
    Id INT IDENTITY(1,1) CONSTRAINT PK_Rooms PRIMARY KEY,
    RoomNumber VARCHAR(20) NOT NULL CONSTRAINT UQ_Rooms_RoomNumber UNIQUE, -- Ví dụ: KL-101, KL-102
    RoomTypeId INT NOT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Rooms_Status DEFAULT ('AVAILABLE')
        CONSTRAINT CK_Rooms_Status CHECK (Status IN ('AVAILABLE', 'OCCUPIED', 'CLEANING', 'MAINTENANCE')),
    IsActive BIT NOT NULL CONSTRAINT DF_Rooms_IsActive DEFAULT (1),
    CONSTRAINT FK_Rooms_RoomTypes FOREIGN KEY (RoomTypeId) 
        REFERENCES dbo.RoomTypes(Id) ON DELETE NO ACTION
);
GO

CREATE INDEX IX_Rooms_RoomTypeId ON dbo.Rooms (RoomTypeId);
GO

-- ==========================================================================================
-- 5. BẢNG CHÍNH SÁCH GIÁ THEO NGÀY (RatePlans) - Phục vụ Luật nghiệp vụ 2 (Dynamic Pricing)
-- Phân bổ giá theo Thứ trong tuần (0: CN, 1: T2, ..., 6: T7), phụ thu ngày lễ
-- ==========================================================================================
CREATE TABLE dbo.RatePlans (
    Id INT IDENTITY(1,1) CONSTRAINT PK_RatePlans PRIMARY KEY,
    RoomTypeId INT NOT NULL,
    DayOfWeek TINYINT NOT NULL CONSTRAINT CK_RatePlans_DayOfWeek CHECK (DayOfWeek BETWEEN 0 AND 6),
    Price DECIMAL(18,2) NOT NULL CONSTRAINT CK_RatePlans_Price CHECK (Price >= 0),
    IsWeekend BIT NOT NULL CONSTRAINT DF_RatePlans_IsWeekend DEFAULT (0),
    HolidaySurcharge DECIMAL(18,2) NOT NULL CONSTRAINT DF_RatePlans_HolidaySurcharge DEFAULT (0),
    CONSTRAINT FK_RatePlans_RoomTypes FOREIGN KEY (RoomTypeId) 
        REFERENCES dbo.RoomTypes(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_RatePlans_RoomType_DayOfWeek UNIQUE (RoomTypeId, DayOfWeek)
);
GO

CREATE INDEX IX_RatePlans_RoomTypeId ON dbo.RatePlans (RoomTypeId);
GO

-- ==========================================================================================
-- 6. BẢNG ĐƠN ĐẶT PHÒNG - HEADER ENTITY (Bookings) - Tiêu chí K2.1 & K2.2
-- Đại diện Header cho toàn bộ giao dịch đặt phòng
-- ==========================================================================================
CREATE TABLE dbo.Bookings (
    Id INT IDENTITY(1,1) CONSTRAINT PK_Bookings PRIMARY KEY,
    BookingCode VARCHAR(20) NOT NULL CONSTRAINT UQ_Bookings_BookingCode UNIQUE, -- Ví dụ: BK2026100801
    GuestId INT NOT NULL,
    CheckInDate DATE NOT NULL,
    CheckOutDate DATE NOT NULL,
    TotalGuests INT NOT NULL CONSTRAINT CK_Bookings_TotalGuests CHECK (TotalGuests > 0),
    TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Bookings_TotalAmount DEFAULT (0) 
        CONSTRAINT CK_Bookings_TotalAmount CHECK (TotalAmount >= 0),
    DepositAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Bookings_DepositAmount DEFAULT (0)
        CONSTRAINT CK_Bookings_DepositAmount CHECK (DepositAmount >= 0),
    Status VARCHAR(30) NOT NULL CONSTRAINT DF_Bookings_Status DEFAULT ('PENDING_DEPOSIT')
        CONSTRAINT CK_Bookings_Status CHECK (Status IN ('PENDING_DEPOSIT', 'CONFIRMED', 'CHECKED_IN', 'CHECKED_OUT', 'CANCELLED')),
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Bookings_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CancelledAt DATETIME2(0) NULL,
    CancellationFee DECIMAL(18,2) NULL CONSTRAINT CK_Bookings_CancellationFee CHECK (CancellationFee IS NULL OR CancellationFee >= 0),
    Notes NVARCHAR(500) NULL,
    CONSTRAINT CK_Bookings_Dates CHECK (CheckOutDate > CheckInDate),
    CONSTRAINT FK_Bookings_Guests FOREIGN KEY (GuestId) 
        REFERENCES dbo.Guests(Id) ON DELETE NO ACTION
);
GO

CREATE INDEX IX_Bookings_GuestId ON dbo.Bookings (GuestId);
CREATE INDEX IX_Bookings_Dates ON dbo.Bookings (CheckInDate, CheckOutDate);
CREATE INDEX IX_Bookings_Status ON dbo.Bookings (Status);
GO

-- ==========================================================================================
-- 7. BẢNG CHI TIẾT ĐÊM LƯU TRÚ - LINEITEM ENTITY (BookingNights) - Tiêu chí K2.1 & K2.2
-- Đại diện LineItem: Mỗi đêm phòng là 1 dòng độc lập
-- Thực thi LUẬT 1 (Chống Overbooking) & LUẬT 2 (Tính tiền chính xác từng đêm)
-- ==========================================================================================
CREATE TABLE dbo.BookingNights (
    Id INT IDENTITY(1,1) CONSTRAINT PK_BookingNights PRIMARY KEY,
    BookingId INT NOT NULL,
    RoomId INT NOT NULL,
    NightDate DATE NOT NULL,
    Price DECIMAL(18,2) NOT NULL CONSTRAINT CK_BookingNights_Price CHECK (Price >= 0),
    CONSTRAINT FK_BookingNights_Bookings FOREIGN KEY (BookingId) 
        REFERENCES dbo.Bookings(Id) ON DELETE CASCADE,
    CONSTRAINT FK_BookingNights_Rooms FOREIGN KEY (RoomId) 
        REFERENCES dbo.Rooms(Id) ON DELETE NO ACTION
);
GO

-- Chỉ mục tối ưu hóa truy vấn chống Overbooking (Luật 1 - Tiêu chí K1.3 & K2.3)
CREATE INDEX IX_BookingNights_Room_Date ON dbo.BookingNights (RoomId, NightDate);
CREATE INDEX IX_BookingNights_BookingId ON dbo.BookingNights (BookingId);
GO

-- ==========================================================================================
-- 8. THÊM DỮ LIỆU KHỞI TẠO (SEED DATA) CHO KIM LONG ECOSTAY
-- ==========================================================================================

-- 8.1. Tài khoản Phân Quyền Hệ Thống (Admin, Lễ Tân / Nhân Viên, Khách Hàng)
INSERT INTO dbo.AppUsers (Username, PasswordHash, FullName, Email, Role)
VALUES 
('admin', '123456', N'Quản Trị Viên Kim Long EcoStay', 'admin@kimlongecostay.vn', 'Admin'),
('letan', '123456', N'Lễ Tân Kim Long EcoStay (Nhân Viên)', 'letan@kimlongecostay.vn', 'Staff'),
('khachhang', '123456', N'Nguyễn Văn An (Khách Hàng)', 'an.nguyen@gmail.com', 'Customer');

-- 8.2. Khách hàng mẫu
INSERT INTO dbo.Guests (FullName, PhoneNumber, Email, IdentityCard)
VALUES 
(N'Nguyễn Văn An', '0905123456', 'an.nguyen@gmail.com', '046098001234'),
(N'Trần Thị Mai', '0914654321', 'mai.tran@gmail.com', '046199005678'),
(N'Lê Hoàng Nam', '0988776655', 'nam.le@gmail.com', '046095009988');

-- 8.3. Danh mục Loại phòng thực tế - Dự Án Kim Long EcoStay (MVP 2 Căn Nhà Vườn - 5 Phòng)
INSERT INTO dbo.RoomTypes (TypeName, Description, MaxGuests, BasePrice, Amenities, ImageUrl)
VALUES 
(N'Eco-Nest Ban Mai', N'View trực diện cánh đồng lúa Kim Long & đón bình minh. Ban công kính panorama, đệm organic, xà phòng bồ hòn & dầu gội thảo mộc.', 2, 390000, N'SmartLock IoT 10s, Wifi mesh, Xe đạp trợ lực điện, Trà sen Cung Đình, Ban công kính panorama', '/images/room_econest.jpg'),
(N'Eco-Nest Hoàng Hôn', N'Góc ngắm hoàng hôn ráng vàng trên sông Hương & rặng tre ngà Kim Long. Bồn tắm thoáng mát bồn sứ cao cấp & tinh dầu sả chanh.', 2, 420000, N'SmartLock IoT 10s, View hoàng hôn sông Hương, Tinh dầu thảo mộc, Xe đạp trợ lực điện, Đèn ngủ tự động', '/images/room_hoanghon.jpg'),
(N'Garden Suite Cố Đô Thanh Trà', N'Nhà rường kết hợp tiện nghi đương đại dưới bóng vườn bưởi thanh trà 40 năm tuổi. Hiên trà thưởng nguyệt & bồn ngâm chân gỗ pơ-mu.', 3, 680000, N'SmartLock QR 10s, Bồn ngâm chân thảo mộc, 02 Xe đạp trợ lực GPS Audio Guide, Bánh ướt thịt nướng đón tiếp, Nông sản sạch vườn', '/images/room_gardensuite.jpg'),
(N'Family Eco-Villa Gắn Kết', N'Biệt thự vườn sinh thái dành cho gia đình trẻ, sân vườn cỏ an toàn cho bé, bộ kit nông nghiệp học hái quả và tưới rau.', 4, 820000, N'SmartLock độc lập 24/7, Sân vườn BBQ riêng, Kit trải nghiệm nông nghiệp cho bé, Xe đạp gia đình, Ăn sáng đặc sản Huế nóng', '/images/hero_ecostay.jpg'),
(N'Heritage Studio Trầm Mặc', N'Độc bản văn hóa Cố Đô, view hồ súng thanh tịnh & rặng chuối ngự. Bộ ấm trà Cung Đình, bàn cờ tướng & kệ sách lịch sử triều Nguyễn.', 2, 590000, N'Khóa điện tử IP66, Ấm trà Cung Đình, Kệ sách văn hóa di sản, Trợ giá 50% xe máy điện thông minh, Ẩm thực lò gia truyền', '/images/nha_ruong_2.jpg');

-- 8.4. Danh mục Phòng cụ thể (5 Phòng thuộc 2 Căn Nhà Vườn Kim Long)
INSERT INTO dbo.Rooms (RoomNumber, RoomTypeId, Status)
VALUES 
('KLE-R101', 1, 'AVAILABLE'), -- Nhà Vườn Hương Sen - Phòng Ban Mai
('KLE-R102', 2, 'AVAILABLE'), -- Nhà Vườn Hương Sen - Phòng Hoàng Hôn
('KLE-R201', 3, 'AVAILABLE'), -- Nhà Vườn Nguyệt Biều - Garden Suite Thanh Trà
('KLE-R202', 4, 'AVAILABLE'), -- Nhà Vườn Nguyệt Biều - Family Villa
('KLE-R203', 5, 'AVAILABLE'); -- Nhà Vườn Nguyệt Biều - Heritage Studio

-- 8.5. Thiết lập Bảng giá theo ngày trong tuần (RatePlan: Ngày thường T2-T5 vs Cuối tuần T6-CN)
-- Loại 1: Eco-Nest Ban Mai (Thường: 390k, Cuối tuần: 480k)
INSERT INTO dbo.RatePlans (RoomTypeId, DayOfWeek, Price, IsWeekend, HolidaySurcharge)
VALUES
(1, 0, 480000, 1, 0), (1, 1, 390000, 0, 0), (1, 2, 390000, 0, 0),
(1, 3, 390000, 0, 0), (1, 4, 390000, 0, 0), (1, 5, 480000, 1, 0), (1, 6, 480000, 1, 0);

-- Loại 2: Eco-Nest Hoàng Hôn (Thường: 420k, Cuối tuần: 500k)
INSERT INTO dbo.RatePlans (RoomTypeId, DayOfWeek, Price, IsWeekend, HolidaySurcharge)
VALUES
(2, 0, 500000, 1, 0), (2, 1, 420000, 0, 0), (2, 2, 420000, 0, 0),
(2, 3, 420000, 0, 0), (2, 4, 420000, 0, 0), (2, 5, 500000, 1, 0), (2, 6, 500000, 1, 0);

-- Loại 3: Garden Suite Cố Đô Thanh Trà (Thường: 680k, Cuối tuần: 850k)
INSERT INTO dbo.RatePlans (RoomTypeId, DayOfWeek, Price, IsWeekend, HolidaySurcharge)
VALUES
(3, 0, 850000, 1, 0), (3, 1, 680000, 0, 0), (3, 2, 680000, 0, 0),
(3, 3, 680000, 0, 0), (3, 4, 680000, 0, 0), (3, 5, 850000, 1, 0), (3, 6, 850000, 1, 0);

-- Loại 4: Family Eco-Villa (Thường: 820k, Cuối tuần: 990k)
INSERT INTO dbo.RatePlans (RoomTypeId, DayOfWeek, Price, IsWeekend, HolidaySurcharge)
VALUES
(4, 0, 990000, 1, 0), (4, 1, 820000, 0, 0), (4, 2, 820000, 0, 0),
(4, 3, 820000, 0, 0), (4, 4, 820000, 0, 0), (4, 5, 990000, 1, 0), (4, 6, 990000, 1, 0);

-- Loại 5: Heritage Studio (Thường: 590k, Cuối tuần: 720k)
INSERT INTO dbo.RatePlans (RoomTypeId, DayOfWeek, Price, IsWeekend, HolidaySurcharge)
VALUES
(5, 0, 720000, 1, 0), (5, 1, 590000, 0, 0), (5, 2, 590000, 0, 0),
(5, 3, 590000, 0, 0), (5, 4, 590000, 0, 0), (5, 5, 720000, 1, 0), (5, 6, 720000, 1, 0);

-- 8.6. Mẫu Giao dịch Đặt phòng Header - LineItem (Đáp ứng Tiêu chí K2.2)
-- Đơn #BK20261015-01: Khách Nguyễn Văn An đặt phòng KLE-R201 (2 đêm: 15/10 và 16/10/2026)
INSERT INTO dbo.Bookings (BookingCode, GuestId, CheckInDate, CheckOutDate, TotalGuests, TotalAmount, DepositAmount, Status, Notes)
VALUES ('BK20261015-01', 1, '2026-10-15', '2026-10-17', 2, 1530000, 765000, 'CONFIRMED', N'Khách đến lúc 14h30, cần 02 xe đạp trợ lực điện GPS Audio Guide');

DECLARE @NewBookingId INT = SCOPE_IDENTITY();

-- Đêm 1: Thứ Năm (15/10) giá thường 680.000đ; Đêm 2: Thứ Sáu (16/10) giá cuối tuần 850.000đ -> Tổng 1.530.000đ
INSERT INTO dbo.BookingNights (BookingId, RoomId, NightDate, Price)
VALUES 
(@NewBookingId, 3, '2026-10-15', 680000),
(@NewBookingId, 3, '2026-10-16', 850000);
GO

-- ==========================================================================================
-- 9. THỦ TỤC LƯU GIAO DỊCH ĐẶT PHÒNG TRONG 1 TRANSACTION (Minh họa tiêu chí K2.2)
-- ==========================================================================================
CREATE OR ALTER PROCEDURE dbo.sp_CreateBookingTransaction
    @BookingCode VARCHAR(20),
    @GuestId INT,
    @CheckInDate DATE,
    @CheckOutDate DATE,
    @TotalGuests INT,
    @TotalAmount DECIMAL(18,2),
    @DepositAmount DECIMAL(18,2),
    @Notes NVARCHAR(500) = NULL,
    @RoomId INT,
    @NewBookingId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Kiểm tra Luật 1: Chống Overbooking (Không có đêm nào bị trùng lặp)
        IF EXISTS (
            SELECT 1 
            FROM dbo.BookingNights bn
            JOIN dbo.Bookings b ON bn.BookingId = b.Id
            WHERE bn.RoomId = @RoomId
              AND bn.NightDate >= @CheckInDate
              AND bn.NightDate < @CheckOutDate
              AND b.Status IN ('PENDING_DEPOSIT', 'CONFIRMED', 'CHECKED_IN')
        )
        BEGIN
            RAISERROR(N'Phòng đã có khách đặt trong khoảng ngày đã chọn (Vi phạm Luật Overbooking)!', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- 2. Ghi bản ghi Header (Bookings)
        INSERT INTO dbo.Bookings (BookingCode, GuestId, CheckInDate, CheckOutDate, TotalGuests, TotalAmount, DepositAmount, Status, Notes)
        VALUES (@BookingCode, @GuestId, @CheckInDate, @CheckOutDate, @TotalGuests, @TotalAmount, @DepositAmount, 'PENDING_DEPOSIT', @Notes);

        SET @NewBookingId = SCOPE_IDENTITY();

        -- 3. Tạo các LineItem (BookingNights) cho từng đêm lưu trú
        DECLARE @CurrentNight DATE = @CheckInDate;
        WHILE @CurrentNight < @CheckOutDate
        BEGIN
            DECLARE @DayOfWeek TINYINT = DATEPART(WEEKDAY, @CurrentNight) - 1; -- 0: Sunday ... 6: Saturday
            DECLARE @RoomTypeId INT = (SELECT RoomTypeId FROM dbo.Rooms WHERE Id = @RoomId);
            DECLARE @NightPrice DECIMAL(18,2);

            SELECT TOP 1 @NightPrice = Price
            FROM dbo.RatePlans
            WHERE RoomTypeId = @RoomTypeId AND DayOfWeek = @DayOfWeek;

            IF @NightPrice IS NULL
                SET @NightPrice = (SELECT BasePrice FROM dbo.RoomTypes WHERE Id = @RoomTypeId);

            INSERT INTO dbo.BookingNights (BookingId, RoomId, NightDate, Price)
            VALUES (@NewBookingId, @RoomId, @CurrentNight, @NightPrice);

            SET @CurrentNight = DATEADD(DAY, 1, @CurrentNight);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ==========================================================================================
-- 10. KHUNG VIEW TƯƠNG THÍCH ORM BLAZOR / DAPPER (dbo.Product)
-- Phục vụ tầng Plugin KimLongEcoStay.DataStore.SQL.Dapper truy vấn trực tiếp 5 phòng thực tế
-- ==========================================================================================
CREATE OR ALTER VIEW dbo.Product AS
SELECT 
    r.Id AS ProductId,
    rt.TypeName + ' (' + r.RoomNumber + ')' AS Name,
    CASE 
        WHEN r.Id IN (1, 2) THEN N'Nhà Vườn Hương Sen (Kim Long)'
        ELSE N'Nhà Vườn Nguyệt Biều (Kim Long)'
    END AS Brand,
    rt.BasePrice AS Price,
    rt.ImageUrl AS ImageLink,
    rt.Description AS Description
FROM dbo.Rooms r
JOIN dbo.RoomTypes rt ON r.RoomTypeId = rt.Id;
GO

-- ==========================================================================================
-- 11. TRUY VẤN KIỂM CHỨNG CSDL (VERIFICATION QUERIES)
-- ==========================================================================================
-- 11.1. Danh sách 5 phòng kèm phân loại nhà vườn & giá niêm yết
SELECT 
    r.Id AS RoomId,
    r.RoomNumber,
    rt.TypeName,
    CASE WHEN r.Id IN (1, 2) THEN N'Nhà Vườn Hương Sen' ELSE N'Nhà Vườn Nguyệt Biều' END AS GardenHost,
    rt.BasePrice AS PriceRegular,
    rt.MaxGuests,
    r.Status
FROM dbo.Rooms r
JOIN dbo.RoomTypes rt ON r.RoomTypeId = rt.Id;

-- 11.2. Kiểm tra giao dịch Header - LineItem đơn đặt phòng #BK20261015-01
SELECT 
    b.BookingCode,
    g.FullName AS GuestName,
    g.PhoneNumber,
    b.CheckInDate,
    b.CheckOutDate,
    b.TotalAmount,
    b.DepositAmount,
    b.Status,
    COUNT(bn.Id) AS TotalNightsBooked,
    SUM(bn.Price) AS VerifiedTotal
FROM dbo.Bookings b
JOIN dbo.Guests g ON b.GuestId = g.Id
JOIN dbo.BookingNights bn ON b.Id = bn.BookingId
WHERE b.BookingCode = 'BK20261015-01'
GROUP BY b.BookingCode, g.FullName, g.PhoneNumber, b.CheckInDate, b.CheckOutDate, b.TotalAmount, b.DepositAmount, b.Status;
GO

-- ==========================================================================================
-- 12. BỔ SUNG MODULE QUẢN LÝ NHÂN SỰ, TIỀN LƯƠNG & TÀI CHÍNH THU CHI (KIM LONG ECOSTAY)
-- ==========================================================================================

-- 12.1. BẢNG PHÒNG BAN (Departments)
IF OBJECT_ID(N'dbo.Departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Departments (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Departments PRIMARY KEY,
        Code VARCHAR(20) NOT NULL CONSTRAINT UQ_Departments_Code UNIQUE,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Departments_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

-- 12.2. BẢNG CHỨC VỤ (Positions)
IF OBJECT_ID(N'dbo.Positions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Positions (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Positions PRIMARY KEY,
        Title NVARCHAR(100) NOT NULL,
        DepartmentId INT NOT NULL,
        BaseSalaryRange NVARCHAR(100) NULL,
        Description NVARCHAR(255) NULL,
        CONSTRAINT FK_Positions_Departments FOREIGN KEY (DepartmentId) 
            REFERENCES dbo.Departments(Id) ON DELETE CASCADE
    );
END
GO

-- 12.3. BẢNG HỒ SƠ NHÂN VIÊN / NHÂN SỰ (Employees)
IF OBJECT_ID(N'dbo.Employees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employees (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Employees PRIMARY KEY,
        EmployeeCode VARCHAR(20) NOT NULL CONSTRAINT UQ_Employees_Code UNIQUE,
        FullName NVARCHAR(100) NOT NULL,
        AvatarUrl NVARCHAR(500) NULL,
        DateOfBirth DATE NOT NULL,
        Gender NVARCHAR(10) NOT NULL CONSTRAINT DF_Employees_Gender DEFAULT (N'Nam'),
        PhoneNumber VARCHAR(20) NOT NULL,
        Email VARCHAR(100) NOT NULL,
        Address NVARCHAR(255) NULL,
        DepartmentId INT NOT NULL,
        PositionId INT NOT NULL,
        HireDate DATE NOT NULL,
        ContractType NVARCHAR(50) NOT NULL CONSTRAINT DF_Employees_ContractType DEFAULT (N'Chính thức'),
        ContractStartDate DATE NOT NULL,
        ContractEndDate DATE NULL,
        WorkStatus NVARCHAR(50) NOT NULL CONSTRAINT DF_Employees_WorkStatus DEFAULT (N'Đang làm việc'),
        BaseSalary DECIMAL(18,2) NOT NULL CONSTRAINT DF_Employees_BaseSalary DEFAULT (0),
        Allowance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Employees_Allowance DEFAULT (0),
        Notes NVARCHAR(500) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_Employees_IsDeleted DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Employees_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentId) 
            REFERENCES dbo.Departments(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Employees_Positions FOREIGN KEY (PositionId) 
            REFERENCES dbo.Positions(Id) ON DELETE NO ACTION
    );
    CREATE INDEX IX_Employees_Dept_Pos ON dbo.Employees (DepartmentId, PositionId);
    CREATE INDEX IX_Employees_Status ON dbo.Employees (WorkStatus, IsDeleted);
END
GO

-- 12.4. BẢNG HỢP ĐỒNG LAO ĐỘNG (Contracts)
IF OBJECT_ID(N'dbo.Contracts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Contracts (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Contracts PRIMARY KEY,
        ContractCode VARCHAR(50) NOT NULL CONSTRAINT UQ_Contracts_Code UNIQUE,
        EmployeeId INT NOT NULL,
        ContractType NVARCHAR(50) NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NULL,
        BasicSalary DECIMAL(18,2) NOT NULL,
        Allowance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Contracts_Allowance DEFAULT (0),
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Contracts_Status DEFAULT (N'Còn hiệu lực'),
        SignedDate DATE NOT NULL,
        Notes NVARCHAR(500) NULL,
        CONSTRAINT FK_Contracts_Employees FOREIGN KEY (EmployeeId) 
            REFERENCES dbo.Employees(Id) ON DELETE CASCADE
    );
END
GO

-- 12.5. BẢNG QUẢN LÝ TIỀN LƯƠNG (Payrolls)
IF OBJECT_ID(N'dbo.Payrolls', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payrolls (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Payrolls PRIMARY KEY,
        PayrollCode VARCHAR(50) NOT NULL CONSTRAINT UQ_Payrolls_Code UNIQUE,
        EmployeeId INT NOT NULL,
        Month INT NOT NULL CONSTRAINT CK_Payrolls_Month CHECK (Month BETWEEN 1 AND 12),
        Year INT NOT NULL CONSTRAINT CK_Payrolls_Year CHECK (Year >= 2020),
        BaseSalary DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payrolls_BaseSalary DEFAULT (0),
        Allowance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payrolls_Allowance DEFAULT (0),
        Bonus DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payrolls_Bonus DEFAULT (0),
        OvertimePay DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payrolls_OvertimePay DEFAULT (0),
        Deductions DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payrolls_Deductions DEFAULT (0),
        AdvancePayment DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payrolls_AdvancePayment DEFAULT (0),
        GrossIncome DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payrolls_GrossIncome DEFAULT (0),
        NetIncome DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payrolls_NetIncome DEFAULT (0),
        PaymentDate DATETIME2(0) NULL,
        PaymentMethod NVARCHAR(50) NOT NULL CONSTRAINT DF_Payrolls_PaymentMethod DEFAULT (N'Chuyển khoản VietQR'),
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Payrolls_Status DEFAULT (N'DRAFT'),
        ApprovedBy NVARCHAR(100) NULL,
        ApprovedAt DATETIME2(0) NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Payrolls_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,
        CONSTRAINT FK_Payrolls_Employees FOREIGN KEY (EmployeeId) 
            REFERENCES dbo.Employees(Id) ON DELETE NO ACTION,
        CONSTRAINT UQ_Payrolls_Employee_Period UNIQUE (EmployeeId, Month, Year)
    );
END
GO

-- 12.6. BẢNG DANH MỤC CHI PHÍ (ExpenseCategories)
IF OBJECT_ID(N'dbo.ExpenseCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExpenseCategories (
        Id INT IDENTITY(1,1) CONSTRAINT PK_ExpenseCategories PRIMARY KEY,
        Code VARCHAR(50) NOT NULL CONSTRAINT UQ_ExpenseCategories_Code UNIQUE,
        Name NVARCHAR(100) NOT NULL,
        ColorHex VARCHAR(20) NOT NULL DEFAULT ('#10B981'),
        IconClass VARCHAR(50) NOT NULL DEFAULT ('bi bi-cash-stack'),
        Description NVARCHAR(255) NULL
    );
END
GO

-- 12.7. BẢNG PHIẾU CHI PHÍ (Expenses)
IF OBJECT_ID(N'dbo.Expenses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Expenses (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Expenses PRIMARY KEY,
        VoucherCode VARCHAR(50) NOT NULL CONSTRAINT UQ_Expenses_Code UNIQUE,
        ExpenseDate DATE NOT NULL,
        CategoryId INT NOT NULL,
        Title NVARCHAR(255) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL CONSTRAINT CK_Expenses_Amount CHECK (Amount >= 0),
        PayeeName NVARCHAR(150) NOT NULL,
        PaymentMethod NVARCHAR(50) NOT NULL DEFAULT (N'Chuyển khoản'),
        Status NVARCHAR(50) NOT NULL DEFAULT (N'PAID'),
        CreatedBy NVARCHAR(100) NOT NULL,
        ApprovedBy NVARCHAR(100) NULL,
        PayrollId INT NULL,
        AttachmentUrl NVARCHAR(500) NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Expenses_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Expenses_Categories FOREIGN KEY (CategoryId) 
            REFERENCES dbo.ExpenseCategories(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Expenses_Payrolls FOREIGN KEY (PayrollId) 
            REFERENCES dbo.Payrolls(Id) ON DELETE NO ACTION
    );
END
GO

-- ==========================================================================================
-- 13. MÔ HÌNH DỮ LIỆU ĐẶC SẢN, TRÀ CUNG ĐÌNH, THANH TOÁN CỌC 50% & NHẬT KÝ EMAIL
-- ==========================================================================================

-- 13.1. BẢNG SẢN PHẨM ĐẶC SẢN & ẨM THỰC KIM LONG (SpecialtyProducts)
IF OBJECT_ID(N'dbo.SpecialtyProducts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SpecialtyProducts (
        Id INT IDENTITY(1,1) CONSTRAINT PK_SpecialtyProducts PRIMARY KEY,
        ProductCode VARCHAR(50) NOT NULL CONSTRAINT UQ_SpecialtyProducts_Code UNIQUE,
        Name NVARCHAR(200) NOT NULL,
        Category NVARCHAR(100) NOT NULL, -- Ẩm thực địa phương, Đặc sản & bánh Huế, Trà cung đình & quà tặng, Đặc sản đóng gói
        Price DECIMAL(18,2) NOT NULL CONSTRAINT CK_SpecialtyProducts_Price CHECK (Price >= 0),
        Unit NVARCHAR(50) NOT NULL DEFAULT (N'phần'),
        StockQuantity INT NOT NULL DEFAULT (50) CONSTRAINT CK_SpecialtyProducts_Stock CHECK (StockQuantity >= 0),
        ImageLink NVARCHAR(500) NULL,
        Description NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL DEFAULT (1),
        IsFeatured BIT NOT NULL DEFAULT (0),
        IsSample BIT NOT NULL DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SpecialtyProducts_CreatedAt DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_SpecialtyProducts_Cat ON dbo.SpecialtyProducts (Category, IsActive);

    -- Dữ liệu mẫu chuẩn Cố đô Huế
    INSERT INTO dbo.SpecialtyProducts (ProductCode, Name, Category, Price, Unit, StockQuantity, ImageLink, Description, IsActive, IsFeatured)
    VALUES
    ('SP-BANH-UOT', N'Bánh Ướt Thịt Nướng Kim Long (Suất Đặc Biệt)', N'Ẩm thực địa phương và món ăn đặc trưng Huế', 65000, N'suất', 40, '/images/mon_banhuot.jpg', N'Đặc sản đệ nhất Kim Long: Bánh ướt tráng mỏng nóng hổi cuốn thịt ba chỉ nướng than hoa thơm lừng và nước chấm tương mè gia truyền.', 1, 1),
    ('SP-BUN-BO', N'Bún Bò Giò Heo Cố Đô Kim Long', N'Ẩm thực địa phương và món ăn đặc trưng Huế', 55000, N'tô', 35, '/images/mon_banhuot.jpg', N'Nước dùng ninh xương bò và ruốc Huế truyền thống, dậy mùi sả thơm nồng, chả cua quết tay và tiết mềm.', 1, 1),
    ('SP-COM-HEN', N'Cơm Hến Hoa Đông Cồn Hến', N'Ẩm thực địa phương và món ăn đặc trưng Huế', 40000, N'tô', 30, '/images/mon_banhuot.jpg', N'Hến xào đậm đà, tóp mỡ giòn rụm, môn bạc hà thanh mát kèm nước hến nóng sốt nêm ớt bột Huế.', 1, 0),
    ('SP-MET-BANH-HUE', N'Mẹt 5 Loại Bánh Huế Cố Đô (Bèo, Nậm, Lọc, Ít Trần, Ram Ít)', N'Đặc sản, bánh Huế và các sản phẩm thủ công địa phương', 85000, N'mẹt', 25, '/images/mon_banhuot.jpg', N'Đầy đủ phong vị bánh truyền thống cung đình gói lá chuối tiêu xanh, tôm thịt đậm vị, nước mắm ớt sim cay nồng.', 1, 1),
    ('SP-TRA-SEN', N'Trà Sen Cung Đình Huế (Hộp 10 Túi Ứp Gạo Sen Tịnh Tâm)', N'Trà, sản phẩm trà và quà tặng văn hóa Huế', 180000, N'hộp', 50, '/images/ho_sen_hue.jpg', N'Tuyển chọn búp trà Shan tuyết ủ cùng hạt gạo hoa sen hồ Tịnh Tâm thơm ngát quý phái, thanh tao.', 1, 1),
    ('SP-TRA-16-VI', N'Trà Cung Đình Huế 16 Vị Thảo Mộc Hoàng Gia', N'Trà, sản phẩm trà và quà tặng văn hóa Huế', 120000, N'gói 500g', 60, '/images/nha_ruong_2.jpg', N'Thức uống bồi bổ của bậc đế vương: Atiso, cúc hoa, cỏ ngọt, tim sen, đại táo, hoài sơn, hoa hòe giúp an thần.', 1, 1),
    ('SP-TRAM-THUY-XUAN', N'Tinh Dầu Tràm Gió Thủy Xuân Nguyên Chất (100ml)', N'Đặc sản đóng gói phù hợp để mua mang về hoặc làm quà', 150000, N'chai', 45, '/images/kien_truc_vuon_hue.jpg', N'Chưng cất thủ công từ lá tràm gió tự nhiên, mùi hương ấm áp ngừa cảm lạnh, xua muỗi cho trẻ nhỏ và người lớn tuổi.', 1, 0),
    ('SP-MUT-GUNG', N'Mứt Gừng Kim Long Cay Nồng (Hộp 300g)', N'Đặc sản đóng gói phù hợp để mua mang về hoặc làm quà', 65000, N'hộp', 50, '/images/nha_ruong_3.jpg', N'Làm từ củ gừng dé Kim Long củ nhỏ cay đậm, ngào đường phèn giòn tan, nhâm nhi cùng chén trà sen nóng ngày se lạnh.', 1, 0);
END
GO

-- 13.2. BẢNG DỊCH VỤ TRÀ CUNG ĐÌNH HUẾ (TeaServices)
IF OBJECT_ID(N'dbo.TeaServices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeaServices (
        Id INT IDENTITY(1,1) CONSTRAINT PK_TeaServices PRIMARY KEY,
        ServiceCode VARCHAR(50) NOT NULL CONSTRAINT UQ_TeaServices_Code UNIQUE,
        Name NVARCHAR(200) NOT NULL,
        PackageType NVARCHAR(50) NOT NULL, -- Cá nhân, Cặp đôi, Nhóm bạn, Gói văn hóa
        Price DECIMAL(18,2) NOT NULL CONSTRAINT CK_TeaServices_Price CHECK (Price >= 0),
        PriceUnit NVARCHAR(50) NOT NULL DEFAULT (N'khách'),
        DurationMinutes INT NOT NULL DEFAULT (60),
        MaxGuests INT NOT NULL DEFAULT (8),
        AvailableSlots INT NOT NULL DEFAULT (8),
        ServingSchedule NVARCHAR(200) NOT NULL DEFAULT (N'08:30 - 10:30, 15:30 - 17:30, 19:30 - 21:00'),
        ImageUrl NVARCHAR(500) NULL,
        Description NVARCHAR(MAX) NULL,
        TermsAndCancellation NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL DEFAULT (1),
        RequiresManualConfirmation BIT NOT NULL DEFAULT (0)
    );

    -- Dữ liệu 4 gói trà cấu hình
    INSERT INTO dbo.TeaServices (ServiceCode, Name, PackageType, Price, PriceUnit, DurationMinutes, MaxGuests, AvailableSlots, ServingSchedule, ImageUrl, Description, TermsAndCancellation, IsActive)
    VALUES
    ('TEA-HOMESTAY', N'Thưởng Trà Cung Đình Tại Homestay', N'Cá nhân', 85000, N'người', 45, 8, 8, N'08:30, 15:30, 19:30', '/images/nha_ruong_2.jpg', N'Thưởng thức ấm trà sen Tịnh Tâm hoặc trà thảo mộc 16 vị ngắm hoa cau vườn bưởi thanh trà ven sông Hương kèm mứt gừng Huế nồng ấm.', N'Đặt trước tối thiểu 02 tiếng. Hủy miễn phí trước 04 tiếng.', 1),
    ('TEA-CAKES', N'Trải Nghiệm Thưởng Trà Kết Hợp Bánh Huế', N'Cá nhân', 135000, N'người', 60, 12, 12, N'09:00, 15:00, 19:30', '/images/mon_banhuot.jpg', N'Set trà thượng hạng cung đình đi kèm mẹt 5 loại bánh tiến vua: bánh bèo chén tôm chấy, bánh nậm mộc, bánh bột lọc trong vắt và bánh ít trần.', N'Đặt trước tối thiểu 03 tiếng để chuẩn bị bánh nóng. Hủy miễn phí trước 06 tiếng.', 1),
    ('TEA-COUPLE', N'Gói Thưởng Trà Dành Cho Cặp Đôi & Nhóm Khách', N'Gói', 250000, N'gói 2-4 khách', 75, 4, 4, N'09:30, 16:00, 20:00', '/images/room_gardensuite.jpg', N'Không gian riêng tư bên hiên nhà rường cổ kính, nến thơm tinh dầu sả chanh, ấm trà sen ướp hoa tươi và đĩa bánh mứt tiến vua cao cấp.', N'Đặt trước tối thiểu 04 tiếng. Hủy hoàn tiền 100% trước 12 tiếng.', 1),
    ('TEA-CULTURE', N'Gói Trải Nghiệm Văn Hóa Huế Kết Hợp Giới Thiệu Trà', N'Gói văn hóa', 390000, N'gói 4-6 khách', 90, 6, 6, N'09:00, 15:30', '/images/nha_ruong_1.jpg', N'Nghệ nhân trà Cố đô trực tiếp hướng dẫn nghi thức pha trà triều Nguyễn, kỹ thuật chọn nước giếng Hàm Long, cách ngửi hương sen và đàm đạo lịch sử.', N'Đặt trước tối thiểu 12 tiếng để xếp lịch nghệ nhân. Hủy hoàn cọc trước 24 tiếng.', 1);
END
GO

-- 13.3. BẢNG GIAO DỊCH THANH TOÁN CỌC & SỐ DƯ (PaymentTransactions)
IF OBJECT_ID(N'dbo.PaymentTransactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentTransactions (
        Id INT IDENTITY(1,1) CONSTRAINT PK_PaymentTransactions PRIMARY KEY,
        TransactionCode VARCHAR(50) NOT NULL CONSTRAINT UQ_PaymentTransactions_Code UNIQUE,
        BookingId INT NULL,
        OrderOrBookingCode VARCHAR(50) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL CONSTRAINT CK_PaymentTransactions_Amount CHECK (Amount >= 0),
        TransactionType VARCHAR(30) NOT NULL, -- DEPOSIT_50, REMAINING_BALANCE, FULL_PAYMENT, SPECIALTY_ORDER, TEA_SERVICE
        PaymentMethod VARCHAR(50) NOT NULL DEFAULT ('VIETQR_BANK_TRANSFER'),
        Status VARCHAR(30) NOT NULL DEFAULT ('PENDING_VERIFICATION'), -- PENDING_VERIFICATION, VERIFIED_SUCCESS, REJECTED_FAILED, REFUNDED
        TransferContent VARCHAR(100) NOT NULL, -- KLE <Code> COC50 hoặc DU50
        VerifiedAt DATETIME2(0) NULL,
        VerifiedBy NVARCHAR(100) NULL,
        ExternalRefId VARCHAR(100) NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PaymentTransactions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_PaymentTransactions_Bookings FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_PaymentTransactions_Code ON dbo.PaymentTransactions (OrderOrBookingCode);
    CREATE INDEX IX_PaymentTransactions_Status ON dbo.PaymentTransactions (Status);
END
GO

-- 13.4. BẢNG NHẬT KÝ EMAIL GIAO DỊCH TỰ ĐỘNG (EmailLogs)
IF OBJECT_ID(N'dbo.EmailLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmailLogs (
        Id INT IDENTITY(1,1) CONSTRAINT PK_EmailLogs PRIMARY KEY,
        EmailId VARCHAR(50) NOT NULL CONSTRAINT UQ_EmailLogs_EmailId UNIQUE,
        TemplateType VARCHAR(50) NOT NULL, -- BOOKING_REQUEST_RECEIVED, DEPOSIT_CONFIRMED, REMAINING_BALANCE_REQUEST, FULL_PAYMENT_CONFIRMED, etc.
        RecipientEmail VARCHAR(150) NOT NULL,
        Subject NVARCHAR(300) NOT NULL,
        Status VARCHAR(30) NOT NULL DEFAULT ('SUCCESS'), -- SUCCESS, FAILED, RETRIED
        StatusMessage NVARCHAR(500) NULL,
        AssociatedBookingCode VARCHAR(50) NULL,
        SentAt DATETIME2(0) NOT NULL CONSTRAINT DF_EmailLogs_SentAt DEFAULT (SYSUTCDATETIME()),
        RetryCount INT NOT NULL DEFAULT (0),
        ErrorDetails NVARCHAR(MAX) NULL
    );

    CREATE INDEX IX_EmailLogs_BookingCode ON dbo.EmailLogs (AssociatedBookingCode);
    CREATE INDEX IX_EmailLogs_Template ON dbo.EmailLogs (TemplateType);
END
GO


