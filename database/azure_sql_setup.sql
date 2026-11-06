-- ==========================================================================================
-- KIM LONG ECOSTAY — HỆ THỐNG CƠ SỞ DỮ LIỆU ĐÁM MÂY AZURE SQL DATABASE
-- Chuẩn kiến trúc: Azure SQL Database (Serverless / General Purpose / Single Database)
-- Mã hóa: UTF-8 (Vietnamese_CI_AS) | Mô hình dữ liệu chuẩn hóa: 3NF & Header-Line Architecture
-- Hỗ trợ: Microsoft SQL Server 2019 / 2022 / Azure SQL Database
-- ==========================================================================================
-- HƯỚNG DẪN THỰC THI TRÊN AZURE PORTAL:
-- 1. Mở Azure Portal (https://portal.azure.com) -> Vào 'SQL databases' -> Chọn 'KimLongEcoStay'.
-- 2. Trong menu bên trái, chọn 'Query editor (preview)' -> Đăng nhập bằng tài khoản SQL Server admin.
-- 3. Dán toàn bộ nội dung file này vào khung soạn thảo và nhấn nút 'Run' (Chạy).
-- 4. Toàn bộ bảng, khóa chính, khóa ngoại, chỉ mục và dữ liệu mẫu sẽ được khởi tạo trong 3 giây.
-- ==========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- ==========================================================================================
-- PHẦN 1: KHỞI TẠO CÁC BẢNG CỐT LÕI (CORE TABLES)
-- ==========================================================================================

-- 1. BẢNG TÀI KHOẢN NGƯỜI DÙNG & PHÂN QUYỀN RBAC (AppUsers)
IF OBJECT_ID(N'dbo.AppUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppUsers (
        Id INT IDENTITY(1,1) CONSTRAINT PK_AppUsers PRIMARY KEY,
        Username VARCHAR(50) NOT NULL CONSTRAINT UQ_AppUsers_Username UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        Email VARCHAR(100) NULL,
        Role VARCHAR(20) NOT NULL CONSTRAINT CK_AppUsers_Role CHECK (Role IN ('Admin', 'Host', 'Staff', 'HR', 'Accountant', 'Manager', 'Customer')),
        IsActive BIT NOT NULL CONSTRAINT DF_AppUsers_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppUsers_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

-- 2. BẢNG KHÁCH HÀNG (Guests)
IF OBJECT_ID(N'dbo.Guests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Guests (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Guests PRIMARY KEY,
        FullName NVARCHAR(100) NOT NULL,
        PhoneNumber VARCHAR(20) NOT NULL,
        Email VARCHAR(100) NULL,
        IdentityCard VARCHAR(30) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Guests_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

-- 3. BẢNG LOẠI PHÒNG (RoomTypes)
IF OBJECT_ID(N'dbo.RoomTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoomTypes (
        Id INT IDENTITY(1,1) CONSTRAINT PK_RoomTypes PRIMARY KEY,
        TypeName NVARCHAR(100) NOT NULL CONSTRAINT UQ_RoomTypes_TypeName UNIQUE,
        Description NVARCHAR(MAX) NULL,
        MaxGuests INT NOT NULL CONSTRAINT CK_RoomTypes_MaxGuests CHECK (MaxGuests > 0),
        BasePrice DECIMAL(18,2) NOT NULL CONSTRAINT CK_RoomTypes_BasePrice CHECK (BasePrice >= 0),
        Amenities NVARCHAR(MAX) NULL,
        ImageUrl NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_RoomTypes_IsActive DEFAULT (1)
    );
END
GO

-- 4. BẢNG PHÒNG NGHỈ (Rooms)
IF OBJECT_ID(N'dbo.Rooms', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rooms (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Rooms PRIMARY KEY,
        RoomNumber VARCHAR(20) NOT NULL CONSTRAINT UQ_Rooms_RoomNumber UNIQUE,
        RoomTypeId INT NOT NULL,
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_Rooms_Status DEFAULT ('AVAILABLE')
            CONSTRAINT CK_Rooms_Status CHECK (Status IN ('AVAILABLE', 'OCCUPIED', 'CLEANING', 'MAINTENANCE')),
        IsActive BIT NOT NULL CONSTRAINT DF_Rooms_IsActive DEFAULT (1),
        CONSTRAINT FK_Rooms_RoomTypes FOREIGN KEY (RoomTypeId) REFERENCES dbo.RoomTypes(Id)
    );
END
GO

-- 5. BẢNG CHÍNH SÁCH GIÁ LINH HOẠT (RatePlans)
IF OBJECT_ID(N'dbo.RatePlans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RatePlans (
        Id INT IDENTITY(1,1) CONSTRAINT PK_RatePlans PRIMARY KEY,
        RoomTypeId INT NOT NULL,
        DayOfWeek TINYINT NOT NULL CONSTRAINT CK_RatePlans_DayOfWeek CHECK (DayOfWeek BETWEEN 0 AND 6),
        Price DECIMAL(18,2) NOT NULL CONSTRAINT CK_RatePlans_Price CHECK (Price >= 0),
        IsWeekend BIT NOT NULL CONSTRAINT DF_RatePlans_IsWeekend DEFAULT (0),
        HolidaySurcharge DECIMAL(18,2) NOT NULL CONSTRAINT DF_RatePlans_HolidaySurcharge DEFAULT (0),
        CONSTRAINT FK_RatePlans_RoomTypes FOREIGN KEY (RoomTypeId) REFERENCES dbo.RoomTypes(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_RatePlans_RoomType_DayOfWeek UNIQUE (RoomTypeId, DayOfWeek)
    );
END
GO

-- 6. BẢNG ĐƠN ĐẶT PHÒNG - HEADER (Bookings)
IF OBJECT_ID(N'dbo.Bookings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bookings (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Bookings PRIMARY KEY,
        BookingCode VARCHAR(20) NOT NULL CONSTRAINT UQ_Bookings_BookingCode UNIQUE,
        GuestId INT NOT NULL,
        CheckInDate DATE NOT NULL,
        CheckOutDate DATE NOT NULL,
        TotalGuests INT NOT NULL CONSTRAINT CK_Bookings_TotalGuests CHECK (TotalGuests > 0),
        TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT CK_Bookings_TotalAmount CHECK (TotalAmount >= 0),
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_Bookings_Status DEFAULT ('PENDING')
            CONSTRAINT CK_Bookings_Status CHECK (Status IN ('PENDING', 'CONFIRMED', 'CHECKED_IN', 'CHECKED_OUT', 'CANCELLED')),
        SpecialRequests NVARCHAR(500) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Bookings_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Bookings_Guests FOREIGN KEY (GuestId) REFERENCES dbo.Guests(Id),
        CONSTRAINT CK_Bookings_Dates CHECK (CheckOutDate > CheckInDate)
    );
END
GO

-- 7. BẢNG CHI TIẾT ĐÊM ĐẶT - LINE (BookingNights)
IF OBJECT_ID(N'dbo.BookingNights', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BookingNights (
        Id INT IDENTITY(1,1) CONSTRAINT PK_BookingNights PRIMARY KEY,
        BookingId INT NOT NULL,
        RoomId INT NOT NULL,
        NightDate DATE NOT NULL,
        Price DECIMAL(18,2) NOT NULL CONSTRAINT CK_BookingNights_Price CHECK (Price >= 0),
        CONSTRAINT FK_BookingNights_Bookings FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id) ON DELETE CASCADE,
        CONSTRAINT FK_BookingNights_Rooms FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(Id),
        CONSTRAINT UQ_BookingNights_Room_Date UNIQUE (RoomId, NightDate)
    );
END
GO

-- ==========================================================================================
-- PHẦN 2: NHÂN SỰ, TIỀN LƯƠNG & TÀI CHÍNH (HR, PAYROLL & FINANCE)
-- ==========================================================================================

-- 8. PHÒNG BAN (Departments)
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

-- 9. VỊ TRÍ CHỨC DANH (Positions)
IF OBJECT_ID(N'dbo.Positions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Positions (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Positions PRIMARY KEY,
        Title NVARCHAR(100) NOT NULL,
        DepartmentId INT NOT NULL,
        BaseSalaryRange NVARCHAR(100) NULL,
        Description NVARCHAR(255) NULL,
        CONSTRAINT FK_Positions_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id)
    );
END
GO

-- 10. HỒ SƠ NHÂN VIÊN (Employees)
IF OBJECT_ID(N'dbo.Employees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employees (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Employees PRIMARY KEY,
        EmployeeCode VARCHAR(20) NOT NULL CONSTRAINT UQ_Employees_Code UNIQUE,
        FullName NVARCHAR(100) NOT NULL,
        AvatarUrl NVARCHAR(500) NULL,
        DateOfBirth DATE NULL,
        Gender NVARCHAR(10) NULL,
        Phone VARCHAR(20) NULL,
        Email VARCHAR(100) NULL,
        IdentityNumber VARCHAR(30) NULL,
        Address NVARCHAR(255) NULL,
        DepartmentId INT NOT NULL,
        PositionId INT NOT NULL,
        JoinDate DATE NOT NULL,
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_Employees_Status DEFAULT ('ACTIVE'),
        BaseSalary DECIMAL(18,2) NOT NULL CONSTRAINT DF_Employees_BaseSalary DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Employees_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id),
        CONSTRAINT FK_Employees_Positions FOREIGN KEY (PositionId) REFERENCES dbo.Positions(Id)
    );
END
GO

-- 11. HỢP ĐỒNG LAO ĐỘNG (Contracts)
IF OBJECT_ID(N'dbo.Contracts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Contracts (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Contracts PRIMARY KEY,
        ContractCode VARCHAR(30) NOT NULL CONSTRAINT UQ_Contracts_Code UNIQUE,
        EmployeeId INT NOT NULL,
        ContractType NVARCHAR(50) NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NULL,
        BasicSalary DECIMAL(18,2) NOT NULL,
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_Contracts_Status DEFAULT ('ACTIVE'),
        FileScanUrl NVARCHAR(500) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Contracts_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Contracts_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id)
    );
END
GO

-- 12. BẢNG TÍNH LƯƠNG (Payrolls)
IF OBJECT_ID(N'dbo.Payrolls', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payrolls (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Payrolls PRIMARY KEY,
        PayrollCode VARCHAR(30) NOT NULL CONSTRAINT UQ_Payrolls_Code UNIQUE,
        EmployeeId INT NOT NULL,
        Month INT NOT NULL,
        Year INT NOT NULL,
        BaseSalary DECIMAL(18,2) NOT NULL,
        StandardDays INT NOT NULL CONSTRAINT DF_Payrolls_StandardDays DEFAULT (26),
        ActualDays INT NOT NULL CONSTRAINT DF_Payrolls_ActualDays DEFAULT (26),
        Allowance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payrolls_Allowance DEFAULT (0),
        Bonus DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payrolls_Bonus DEFAULT (0),
        Deduction DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payrolls_Deduction DEFAULT (0),
        NetSalary DECIMAL(18,2) NOT NULL,
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_Payrolls_Status DEFAULT ('PENDING'),
        PaymentDate DATETIME2(0) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Payrolls_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Payrolls_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id),
        CONSTRAINT UQ_Payrolls_Emp_Month_Year UNIQUE (EmployeeId, Month, Year)
    );
END
GO

-- 13. DANH MỤC CHI PHÍ (ExpenseCategories)
IF OBJECT_ID(N'dbo.ExpenseCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExpenseCategories (
        Id INT IDENTITY(1,1) CONSTRAINT PK_ExpenseCategories PRIMARY KEY,
        Code VARCHAR(20) NOT NULL CONSTRAINT UQ_ExpenseCategories_Code UNIQUE,
        Name NVARCHAR(100) NOT NULL,
        ColorHex VARCHAR(10) NULL,
        IconClass VARCHAR(50) NULL,
        Description NVARCHAR(255) NULL
    );
END
GO

-- 14. PHIẾU CHI (Expenses)
IF OBJECT_ID(N'dbo.Expenses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Expenses (
        Id INT IDENTITY(1,1) CONSTRAINT PK_Expenses PRIMARY KEY,
        VoucherCode VARCHAR(30) NOT NULL CONSTRAINT UQ_Expenses_VoucherCode UNIQUE,
        ExpenseDate DATE NOT NULL,
        CategoryId INT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Beneficiary NVARCHAR(100) NULL,
        CreatedBy NVARCHAR(100) NULL,
        Note NVARCHAR(500) NULL,
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_Expenses_Status DEFAULT ('APPROVED'),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Expenses_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Expenses_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.ExpenseCategories(Id)
    );
END
GO

-- 15. GIAO DỊCH TÀI CHÍNH (FinancialTransactions)
IF OBJECT_ID(N'dbo.FinancialTransactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FinancialTransactions (
        Id INT IDENTITY(1,1) CONSTRAINT PK_FinancialTransactions PRIMARY KEY,
        TransactionCode VARCHAR(50) NOT NULL CONSTRAINT UQ_FinTrans_Code UNIQUE,
        TransactionType VARCHAR(10) NOT NULL CONSTRAINT CK_FinTrans_Type CHECK (TransactionType IN ('INCOME', 'EXPENSE')),
        Amount DECIMAL(18,2) NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        PaymentMethod NVARCHAR(50) NOT NULL,
        ReferenceId VARCHAR(50) NULL,
        Description NVARCHAR(500) NULL,
        TransactionDate DATETIME2(0) NOT NULL CONSTRAINT DF_FinTrans_Date DEFAULT (SYSUTCDATETIME()),
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_FinTrans_Status DEFAULT ('SUCCESS')
    );
END
GO

-- ==========================================================================================
-- PHẦN 3: ĐẶC SẢN, ĐỐI TÁC, TRÀ CUNG ĐÌNH & SMARTLOCK
-- ==========================================================================================

-- 16. SẢN PHẨM & ĐẶC SẢN HUẾ (SpecialtyProducts)
IF OBJECT_ID(N'dbo.SpecialtyProducts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SpecialtyProducts (
        Id INT IDENTITY(1,1) CONSTRAINT PK_SpecialtyProducts PRIMARY KEY,
        ProductCode VARCHAR(20) NOT NULL CONSTRAINT UQ_SpecialtyProducts_Code UNIQUE,
        Name NVARCHAR(100) NOT NULL,
        Category VARCHAR(20) NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        Unit NVARCHAR(20) NOT NULL,
        StockQuantity INT NOT NULL CONSTRAINT DF_Specialty_Stock DEFAULT (100),
        PartnerId INT NULL,
        ImageUrl NVARCHAR(500) NULL,
        Description NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Specialty_Active DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Specialty_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

-- 17. ĐỐI TÁC ĐỊA PHƯƠNG (LocalPartners)
IF OBJECT_ID(N'dbo.LocalPartners', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LocalPartners (
        Id INT IDENTITY(1,1) CONSTRAINT PK_LocalPartners PRIMARY KEY,
        PartnerCode VARCHAR(20) NOT NULL CONSTRAINT UQ_LocalPartners_Code UNIQUE,
        Name NVARCHAR(100) NOT NULL,
        Type VARCHAR(20) NOT NULL,
        ContactPerson NVARCHAR(50) NULL,
        PhoneNumber VARCHAR(20) NULL,
        Address NVARCHAR(200) NULL,
        CommissionRate DECIMAL(5,2) NOT NULL CONSTRAINT DF_Partners_Commission DEFAULT (0.10),
        IsActive BIT NOT NULL CONSTRAINT DF_Partners_Active DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Partners_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

-- 18. DỊCH VỤ TRÀ CUNG ĐÌNH (TeaServices)
IF OBJECT_ID(N'dbo.TeaServices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeaServices (
        Id INT IDENTITY(1,1) CONSTRAINT PK_TeaServices PRIMARY KEY,
        ServiceCode VARCHAR(20) NOT NULL CONSTRAINT UQ_TeaServices_Code UNIQUE,
        Name NVARCHAR(100) NOT NULL,
        PackageType VARCHAR(20) NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        PriceUnit NVARCHAR(20) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TeaServices_Active DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_TeaServices_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

-- 19. GIAO NHẬN TẬN PHÒNG (RoomDeliveries)
IF OBJECT_ID(N'dbo.RoomDeliveries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoomDeliveries (
        Id INT IDENTITY(1,1) CONSTRAINT PK_RoomDeliveries PRIMARY KEY,
        DeliveryCode VARCHAR(30) NOT NULL CONSTRAINT UQ_RoomDeliveries_Code UNIQUE,
        BookingId INT NOT NULL,
        RoomId INT NOT NULL,
        ItemType VARCHAR(20) NOT NULL,
        ItemId INT NOT NULL,
        Quantity INT NOT NULL CONSTRAINT DF_Deliveries_Qty DEFAULT (1),
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_Deliveries_Status DEFAULT ('ORDERED'),
        DeliveredAt DATETIME2(0) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Deliveries_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

-- 20. BẢO MẬT KHÓA THÔNG MINH (SmartLockPins)
IF OBJECT_ID(N'dbo.SmartLockPins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SmartLockPins (
        Id INT IDENTITY(1,1) CONSTRAINT PK_SmartLockPins PRIMARY KEY,
        RoomId INT NOT NULL,
        BookingId INT NULL,
        PinHash VARCHAR(64) NOT NULL,
        PinSalt VARCHAR(64) NOT NULL CONSTRAINT DF_SmartLockPins_PinSalt DEFAULT ('KLE_SL_SALT_2026'),
        FailedAttempts INT NOT NULL CONSTRAINT DF_SmartLockPins_FailedAttempts DEFAULT (0),
        LockedUntil DATETIME2(0) NULL,
        ValidFrom DATETIME2(0) NOT NULL,
        ValidTo DATETIME2(0) NOT NULL,
        Status VARCHAR(20) NOT NULL CONSTRAINT DF_SmartLockPins_Status DEFAULT ('ACTIVE'),
        IsSimulationData BIT NOT NULL CONSTRAINT DF_SmartLockPins_IsSim DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SmartLockPins_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_SmartLockPins_Rooms FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(Id)
    );
END
GO

-- 21. NHẬT KÝ KHÓA THÔNG MINH (SmartLockAuditLogs)
IF OBJECT_ID(N'dbo.SmartLockAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SmartLockAuditLogs (
        Id INT IDENTITY(1,1) CONSTRAINT PK_SmartLockAuditLogs PRIMARY KEY,
        RoomId INT NOT NULL,
        Action VARCHAR(30) NOT NULL,
        Actor VARCHAR(50) NOT NULL,
        Timestamp DATETIME2(0) NOT NULL CONSTRAINT DF_SmartLockAudit_Time DEFAULT (SYSUTCDATETIME()),
        Details NVARCHAR(500) NULL,
        CONSTRAINT FK_SmartLockAuditLogs_Rooms FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(Id)
    );
END
GO

-- 22. ĐỒNG BỘ HAI CHIỀU SQL & AI AUDIT (SyncAuditLogs & AiAuditLogs)
IF OBJECT_ID(N'dbo.SyncAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SyncAuditLogs (
        Id BIGINT IDENTITY(1,1) CONSTRAINT PK_SyncAuditLogs PRIMARY KEY,
        EntityName VARCHAR(50) NOT NULL,
        RecordId VARCHAR(50) NOT NULL,
        Action VARCHAR(20) NOT NULL,
        Source VARCHAR(20) NOT NULL,
        ClientVersion INT NOT NULL,
        ServerVersion INT NOT NULL,
        Timestamp DATETIME2(0) NOT NULL CONSTRAINT DF_SyncAudit_Time DEFAULT (SYSUTCDATETIME()),
        PayloadJson NVARCHAR(MAX) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.AiAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AiAuditLogs (
        Id BIGINT IDENTITY(1,1) CONSTRAINT PK_AiAuditLogs PRIMARY KEY,
        Prompt NVARCHAR(MAX) NOT NULL,
        Response NVARCHAR(MAX) NOT NULL,
        TokenCount INT NOT NULL,
        LatencyMs INT NOT NULL,
        Timestamp DATETIME2(0) NOT NULL CONSTRAINT DF_AiAudit_Time DEFAULT (SYSUTCDATETIME())
    );
END
GO

-- 23. NHẬT KÝ EMAIL HỆ THỐNG (EmailLogs)
IF OBJECT_ID(N'dbo.EmailLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmailLogs (
        Id INT IDENTITY(1,1) CONSTRAINT PK_EmailLogs PRIMARY KEY,
        EmailId VARCHAR(50) NOT NULL CONSTRAINT UQ_EmailLogs_EmailId UNIQUE,
        TemplateType VARCHAR(50) NOT NULL,
        RecipientEmail VARCHAR(100) NOT NULL,
        Subject NVARCHAR(200) NOT NULL,
        Status VARCHAR(20) NOT NULL,
        SentAt DATETIME2(0) NOT NULL CONSTRAINT DF_EmailLogs_SentAt DEFAULT (SYSUTCDATETIME()),
        ErrorMessage NVARCHAR(MAX) NULL
    );
END
GO

-- ==========================================================================================
-- PHẦN 4: THIẾT LẬP CHỈ MỤC TỐI ƯU TRUY VẤN (INDEXES)
-- ==========================================================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Guests_Phone')
    CREATE INDEX IX_Guests_Phone ON dbo.Guests (PhoneNumber);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Rooms_Type')
    CREATE INDEX IX_Rooms_Type ON dbo.Rooms (RoomTypeId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bookings_Dates')
    CREATE INDEX IX_Bookings_Dates ON dbo.Bookings (CheckInDate, CheckOutDate);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Employees_Dept')
    CREATE INDEX IX_Employees_Dept ON dbo.Employees (DepartmentId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Payrolls_Lookup')
    CREATE INDEX IX_Payrolls_Lookup ON dbo.Payrolls (EmployeeId, Year, Month);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SmartLockAudit_Room')
    CREATE INDEX IX_SmartLockAudit_Room ON dbo.SmartLockAuditLogs (RoomId, Timestamp DESC);
GO

-- ==========================================================================================
-- PHẦN 5: DỮ LIỆU KHỞI TẠO MẪU (SEED DATA - IDEMPOTENT)
-- ==========================================================================================

-- 1. Tài khoản đăng nhập hệ thống (Mật khẩu mặc định: 123456 mã băm Salted SHA-256)
IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Username = 'admin')
BEGIN
    INSERT INTO dbo.AppUsers (Username, PasswordHash, FullName, Email, Role, IsActive)
    VALUES ('admin', '564C07116D7DED47805641FC46710CA1724F44F37868A56169213CBB65A07DA9|177D7D30D3222D79E85E878362C8AE8B3B2FF1933DF3CB12DF85A27A402970FC', N'Trần Mạnh Cường (Admin CEO)', 'admin@kimlongecostay.vn', 'Admin', 1);
END

IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Username = 'letan')
BEGIN
    INSERT INTO dbo.AppUsers (Username, PasswordHash, FullName, Email, Role, IsActive)
    VALUES ('letan', '1465521F76F3642957E4B412E40319191D256A74E97934123DD3B268FEADA82E|C1FD4E790FD8EBCC9216BE70DC10EAFDC85BD520DAC1820F4B60DC88CB2C7BA5', N'Đặng Thu Thảo (Lễ Tân Trưởng)', 'letan@kimlongecostay.vn', 'Staff', 1);
END

IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Username = 'ketoan')
BEGIN
    INSERT INTO dbo.AppUsers (Username, PasswordHash, FullName, Email, Role, IsActive)
    VALUES ('ketoan', 'DCB6A6365E422125D9A6F92A5D2B674F68F949276610324BD813BD696871A787|9E28CDD21EA2D58E931C16F5C79AAB3F2D2E6C691681A0DF2161218D017C1962', N'Lê Thị Mai (Kế Toán Trưởng)', 'ketoan@kimlongecostay.vn', 'Accountant', 1);
END

IF NOT EXISTS (SELECT 1 FROM dbo.AppUsers WHERE Username = 'nhansu')
BEGIN
    INSERT INTO dbo.AppUsers (Username, PasswordHash, FullName, Email, Role, IsActive)
    VALUES ('nhansu', '3E032EFBC4A078338B88E7E90F2F97284CB57914C26BDAC671035F240815EAB1|AB41C0B964F70E32DA2FCE2748A2B4DEEDDC968D5DACD45F714A27714095C7EE', N'Phạm Văn Dũng (Trưởng Phòng Nhân Sự)', 'nhansu@kimlongecostay.vn', 'HR', 1);
END
GO

-- 2. Loại phòng & Phòng nghỉ Eco Luxury
IF NOT EXISTS (SELECT 1 FROM dbo.RoomTypes WHERE TypeName = N'Eco-Nest Ban Mai')
BEGIN
    INSERT INTO dbo.RoomTypes (TypeName, Description, MaxGuests, BasePrice, Amenities, ImageUrl, IsActive)
    VALUES (N'Eco-Nest Ban Mai', N'Không gian nghỉ dưỡng mộc mạc bên luống thanh trà, đón trọn ánh nắng sớm.', 2, 390000, N'Điều hòa, Ban công sân vườn, Bồn tắm sỏi, Trà cung đình đón khách', 'images/room_econest.jpg', 1);
END

IF NOT EXISTS (SELECT 1 FROM dbo.RoomTypes WHERE TypeName = N'Kim Long Heritage')
BEGIN
    INSERT INTO dbo.RoomTypes (TypeName, Description, MaxGuests, BasePrice, Amenities, ImageUrl, IsActive)
    VALUES (N'Kim Long Heritage', N'Căn phòng gỗ truyền thống Cố đô, view hồ sen ngát hương thơm dịu mát.', 3, 580000, N'Giường King gỗ lim, View hồ sen, Trà thất mini, SmartLock bảo mật', 'images/room_heritage.jpg', 1);
END

IF NOT EXISTS (SELECT 1 FROM dbo.RoomTypes WHERE TypeName = N'Sen Vàng Suite')
BEGIN
    INSERT INTO dbo.RoomTypes (TypeName, Description, MaxGuests, BasePrice, Amenities, ImageUrl, IsActive)
    VALUES (N'Sen Vàng Suite', N'Phòng Suite thượng lưu phong cách Cung đình Huế với đầy đủ tiện nghi cao cấp.', 4, 850000, N'Phòng khách riêng, Bồn sục sỏi ngâm hoa quả, Tiệc trà chiều hoàng gia', 'images/room_suite.jpg', 1);
END
GO

-- Gắn phòng nghỉ
IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE RoomNumber = 'KLE-R101')
    INSERT INTO dbo.Rooms (RoomNumber, RoomTypeId, Status, IsActive) VALUES ('KLE-R101', 1, 'AVAILABLE', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE RoomNumber = 'KLE-R102')
    INSERT INTO dbo.Rooms (RoomNumber, RoomTypeId, Status, IsActive) VALUES ('KLE-R102', 1, 'AVAILABLE', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE RoomNumber = 'KLE-R201')
    INSERT INTO dbo.Rooms (RoomNumber, RoomTypeId, Status, IsActive) VALUES ('KLE-R201', 2, 'AVAILABLE', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE RoomNumber = 'KLE-R202')
    INSERT INTO dbo.Rooms (RoomNumber, RoomTypeId, Status, IsActive) VALUES ('KLE-R202', 2, 'AVAILABLE', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE RoomNumber = 'KLE-R301')
    INSERT INTO dbo.Rooms (RoomNumber, RoomTypeId, Status, IsActive) VALUES ('KLE-R301', 3, 'AVAILABLE', 1);
GO

-- 3. Phòng ban & Chức danh
IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Code = 'BGD')
    INSERT INTO dbo.Departments (Code, Name, Description) VALUES ('BGD', N'Ban Giám Đốc', N'Điều hành chiến lược toàn diện Kim Long EcoStay');

IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Code = 'FO')
    INSERT INTO dbo.Departments (Code, Name, Description) VALUES ('FO', N'Lễ Tân & Dịch Vụ Khách Hàng', N'Tiếp đón, check-in, check-out và chăm sóc khách hàng');

IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Code = 'ACC')
    INSERT INTO dbo.Departments (Code, Name, Description) VALUES ('ACC', N'Tài Chính & Kế Toán', N'Quản lý thu chi, công nợ, đối soát và tính lương');

IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Code = 'HR')
    INSERT INTO dbo.Departments (Code, Name, Description) VALUES ('HR', N'Nhân Sự & Hành Chính', N'Tuyển dụng, hồ sơ nhân sự, đào tạo và phúc lợi');
GO

-- 4. Đặc sản Huế & Trà Cung Đình
IF NOT EXISTS (SELECT 1 FROM dbo.SpecialtyProducts WHERE ProductCode = 'SP-MEXUNG')
    INSERT INTO dbo.SpecialtyProducts (ProductCode, Name, Category, Price, Unit, StockQuantity, ImageUrl, Description)
    VALUES ('SP-MEXUNG', N'Mè Xửng Giòn Cung Đình', 'FOOD', 45000, N'Gói 250g', 85, 'images/qua_mexung.jpg', N'Mè xửng giòn truyền thống Kim Long, ngọt thanh thơm mè vừng xứ Huế.');

IF NOT EXISTS (SELECT 1 FROM dbo.SpecialtyProducts WHERE ProductCode = 'SP-TOMCHUA')
    INSERT INTO dbo.SpecialtyProducts (ProductCode, Name, Category, Price, Unit, StockQuantity, ImageUrl, Description)
    VALUES ('SP-TOMCHUA', N'Tôm Chua Trọng Kiệt Cố Đô', 'SPECIALTY', 75000, N'Hũ 450g', 60, 'images/mon_tomchua.jpg', N'Tôm đầm Cầu Hai muối gừng ớt lên men truyền thống đậm vị Cố đô.');

IF NOT EXISTS (SELECT 1 FROM dbo.TeaServices WHERE ServiceCode = 'TEA-DE-VUONG')
    INSERT INTO dbo.TeaServices (ServiceCode, Name, PackageType, Price, PriceUnit, Description)
    VALUES ('TEA-DE-VUONG', N'Trà Cung Đình Nhất Dạ Đế Vương', 'POT', 120000, N'Ấm thưởng thức', N'Đại bổ khí huyết, an thần ngủ ngon từ 16 vị thảo mộc tiến vua.');
GO

-- ==========================================================================================
-- PHẦN 6: TRUY VẤN XÁC NHẬN TRIỂN KHAI THÀNH CÔNG (VERIFICATION)
-- ==========================================================================================
SELECT 
    'Kim Long EcoStay - Azure SQL Database Setup Complete!' AS [Status],
    (SELECT COUNT(*) FROM dbo.AppUsers) AS [TotalUsers],
    (SELECT COUNT(*) FROM dbo.RoomTypes) AS [TotalRoomTypes],
    (SELECT COUNT(*) FROM dbo.Rooms) AS [TotalRooms],
    (SELECT COUNT(*) FROM dbo.Departments) AS [TotalDepartments],
    (SELECT COUNT(*) FROM dbo.SpecialtyProducts) AS [TotalSpecialties],
    (SELECT COUNT(*) FROM dbo.TeaServices) AS [TotalTeas],
    SYSUTCDATETIME() AS [ExecutedAtUtc];
GO
