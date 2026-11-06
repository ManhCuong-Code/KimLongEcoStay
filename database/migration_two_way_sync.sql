-- ==========================================================================================
-- HỆ THỐNG ĐỒNG BỘ CƠ SỞ DỮ LIỆU SQL HAI CHIỀU (TWO-WAY SQL SYNCHRONIZATION & AI CONTEXT)
-- DỰ ÁN: KIM LONG ECOSTAY - NHÀ VƯỜN SINH THÁI CỐ ĐÔ HUẾ
-- Hỗ trợ: Website ↔ ASP.NET Core API / SignalR ↔ SQL Server (SSMS / Stored Procedures)
-- ==========================================================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'KimLongEcoStayDB')
BEGIN
    CREATE DATABASE KimLongEcoStayDB COLLATE Vietnamese_CI_AS;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_databases WHERE database_id = DB_ID(N'KimLongEcoStayDB'))
BEGIN
    ALTER DATABASE KimLongEcoStayDB SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 7 DAYS, AUTO_CLEANUP = ON);
END
GO

USE KimLongEcoStayDB;
GO

-- ==========================================================================================
-- 1. BẢNG NHẬT KÝ ĐỒNG BỘ HAI CHIỀU (SyncAuditLogs) & NHẬT KÝ TRỢ LÝ AI (AiAuditLogs)
-- ==========================================================================================
IF OBJECT_ID(N'dbo.SyncAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SyncAuditLogs (
        Id BIGINT IDENTITY(1,1) CONSTRAINT PK_SyncAuditLogs PRIMARY KEY,
        EntityType VARCHAR(50) NOT NULL,      -- products, rooms, bookings, deliveries, smartlock, partners, employees, payrolls, finance
        EntityKey VARCHAR(100) NOT NULL,      -- SP001, KLE-R101, KLE-20261018-01...
        ActionType VARCHAR(30) NOT NULL,      -- INSERT, UPDATE, DELETE, RECONCILE, REISSUE_PIN, RESYNC
        Source VARCHAR(50) NOT NULL DEFAULT ('SQL_OR_SSMS'), -- WEB_API, SQL_OR_SSMS, STORED_PROC
        ChangedBy NVARCHAR(100) NOT NULL DEFAULT (SUSER_SNAME()),
        Summary NVARCHAR(500) NULL,
        ChangeVersion BIGINT NOT NULL DEFAULT (ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0)),
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_SyncAuditLogs_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_SyncAuditLogs_Created ON dbo.SyncAuditLogs (Id DESC, CreatedAt DESC);
    CREATE INDEX IX_SyncAuditLogs_Entity ON dbo.SyncAuditLogs (EntityType, EntityKey);
END
GO

IF OBJECT_ID(N'dbo.AiAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AiAuditLogs (
        Id BIGINT IDENTITY(1,1) CONSTRAINT PK_AiAuditLogs PRIMARY KEY,
        ConversationId VARCHAR(80) NOT NULL,
        UserRole VARCHAR(50) NOT NULL,
        UserName NVARCHAR(100) NOT NULL,
        Provider VARCHAR(50) NOT NULL,
        Model VARCHAR(50) NOT NULL,
        PromptText NVARCHAR(1000) NOT NULL,
        SqlToolsUsed VARCHAR(250) NULL,
        TokensEstimated INT NOT NULL DEFAULT (0),
        ResponseTimeMs INT NOT NULL DEFAULT (0),
        Status VARCHAR(30) NOT NULL DEFAULT ('SUCCESS'),
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_AiAuditLogs_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_AiAuditLogs_Created ON dbo.AiAuditLogs (CreatedAt DESC);
END
GO

-- ==========================================================================================
-- 2. CẬP NHẬT BẢNG PHÒNG NGHỈ (dbo.Rooms) HỖ TRỢ 8 TRẠNG THÁI VẬN HÀNH & OPTIMISTIC CONCURRENCY
-- ==========================================================================================
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Rooms_Status')
BEGIN
    ALTER TABLE dbo.Rooms DROP CONSTRAINT CK_Rooms_Status;
END
GO

ALTER TABLE dbo.Rooms ADD CONSTRAINT CK_Rooms_Status
    CHECK (Status IN ('AVAILABLE', 'RESERVED', 'OCCUPIED', 'DELIVERING_SERVICE', 'CHECKOUT_PENDING', 'CLEANING', 'CLEANED_READY', 'MAINTENANCE'));
GO

IF COL_LENGTH('dbo.Rooms', 'GuestName') IS NULL
    ALTER TABLE dbo.Rooms ADD GuestName NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.Rooms', 'BookingCode') IS NULL
    ALTER TABLE dbo.Rooms ADD BookingCode VARCHAR(50) NULL;
IF COL_LENGTH('dbo.Rooms', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.Rooms ADD UpdatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Rooms_UpdatedAt DEFAULT (SYSUTCDATETIME());
IF COL_LENGTH('dbo.Rooms', 'SyncVersion') IS NULL
    ALTER TABLE dbo.Rooms ADD SyncVersion INT NOT NULL CONSTRAINT DF_Rooms_SyncVersion DEFAULT (1);
GO

-- Cập nhật trạng thái mẫu thực tế cho 5 buồng phòng sinh thái Kim Long
UPDATE dbo.Rooms SET Status = 'OCCUPIED', GuestName = N'Nguyễn Văn An', BookingCode = 'KLE-20261018-01', UpdatedAt = SYSUTCDATETIME() WHERE RoomNumber = 'KLE-R101';
UPDATE dbo.Rooms SET Status = 'RESERVED', GuestName = N'Hoàng Minh Tuấn', BookingCode = 'KLE-20261015-88', UpdatedAt = SYSUTCDATETIME() WHERE RoomNumber = 'KLE-R102';
UPDATE dbo.Rooms SET Status = 'DELIVERING_SERVICE', GuestName = N'Đặng Thanh Nga', BookingCode = 'KLE-20261014-42', UpdatedAt = SYSUTCDATETIME() WHERE RoomNumber = 'KLE-R201';
UPDATE dbo.Rooms SET Status = 'AVAILABLE', GuestName = NULL, BookingCode = NULL, UpdatedAt = SYSUTCDATETIME() WHERE RoomNumber = 'KLE-R202';
UPDATE dbo.Rooms SET Status = 'CLEANED_READY', GuestName = NULL, BookingCode = NULL, UpdatedAt = SYSUTCDATETIME() WHERE RoomNumber = 'KLE-R203';
GO

-- ==========================================================================================
-- 3. BẢNG SẢN PHẨM & ĐẶC SẢN KIM LONG (dbo.SpecialtyProducts) - ĐẦY ĐỦ 30 SẢN PHẨM THỰC TẾ
-- ==========================================================================================
IF OBJECT_ID(N'dbo.SpecialtyProducts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SpecialtyProducts (
        Id INT IDENTITY(1,1) CONSTRAINT PK_SpecialtyProducts PRIMARY KEY,
        ProductCode VARCHAR(50) NOT NULL CONSTRAINT UQ_SpecialtyProducts_Code UNIQUE,
        Name NVARCHAR(200) NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        Price DECIMAL(18,2) NOT NULL CONSTRAINT CK_SpecialtyProducts_Price CHECK (Price >= 0),
        Unit NVARCHAR(50) NOT NULL DEFAULT (N'phần'),
        StockQuantity INT NOT NULL DEFAULT (30) CONSTRAINT CK_SpecialtyProducts_Stock CHECK (StockQuantity >= 0),
        ImageLink NVARCHAR(500) NULL,
        Origin NVARCHAR(250) NULL,
        PartnerId VARCHAR(50) NULL,
        PartnerName NVARCHAR(200) NULL,
        Description NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL DEFAULT (1),
        IsFeatured BIT NOT NULL DEFAULT (0),
        IsSample BIT NOT NULL DEFAULT (0),
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_SpecialtyProducts_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_SpecialtyProducts_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        SyncVersion INT NOT NULL CONSTRAINT DF_SpecialtyProducts_SyncVersion DEFAULT (1)
    );
    CREATE INDEX IX_SpecialtyProducts_Cat ON dbo.SpecialtyProducts (Category, IsActive);
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.SpecialtyProducts', 'Origin') IS NULL
        ALTER TABLE dbo.SpecialtyProducts ADD Origin NVARCHAR(250) NULL;
    IF COL_LENGTH('dbo.SpecialtyProducts', 'PartnerId') IS NULL
        ALTER TABLE dbo.SpecialtyProducts ADD PartnerId VARCHAR(50) NULL;
    IF COL_LENGTH('dbo.SpecialtyProducts', 'PartnerName') IS NULL
        ALTER TABLE dbo.SpecialtyProducts ADD PartnerName NVARCHAR(200) NULL;
    IF COL_LENGTH('dbo.SpecialtyProducts', 'UpdatedAt') IS NULL
        ALTER TABLE dbo.SpecialtyProducts ADD UpdatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_SpecialtyProducts_UpdatedAt2 DEFAULT (SYSUTCDATETIME());
    IF COL_LENGTH('dbo.SpecialtyProducts', 'SyncVersion') IS NULL
        ALTER TABLE dbo.SpecialtyProducts ADD SyncVersion INT NOT NULL CONSTRAINT DF_SpecialtyProducts_SyncVersion2 DEFAULT (1);
END
GO

-- Đồng bộ đầy đủ 30 sản phẩm chuẩn từ Website Kim Long EcoStay vào dbo.SpecialtyProducts
MERGE dbo.SpecialtyProducts AS target
USING (VALUES
    ('SP001', N'Mứt Gừng Kim Long Cổ Truyền', N'Đặc sản Huế', 65000, N'hộp 250g', 45, 'images/qua_mutgung.jpg', N'Cơ sở chế biến truyền thống Phường Kim Long, TP. Huế', 'PARTNER-DAC-SAN-KIM-LONG', N'Cơ Sở Đặc Sản & Mứt Gừng Kim Long O Lành', N'Mứt gừng lát mỏng đều từ củ gừng sẻ trồng trên đất bãi bồi Kim Long, cay nồng ấm bụng, ngào đường phèn trắng tinh khiết không chất bảo quản.', 1, 1),
    ('SP002', N'Trà Cung Đình Bát Bảo Thảo Mộc', N'Đặc sản Huế', 95000, N'gói 250g', 50, 'images/tra_cung_dinh.jpg', N'Trà thất Kim Long, Phường Kim Long, TP. Huế', 'PARTNER-TRA-CUNG-DINH', N'Trà Thất & Đặc Sản Trà Cung Đình Kim Long', N'Sự hòa quyện của 16 vị thảo mộc cung đình (tim sen, kỷ tử, táo đỏ, cúc hoa, la hán quả, cam thảo...) giúp thanh nhiệt, an thần và ngủ ngon.', 1, 1),
    ('SP003', N'Hộp Bánh Mè Xửng Giòn Kim Long', N'Đặc sản Huế', 55000, N'hộp 300g', 60, 'images/qua_mexung.jpg', N'Làng nghề bánh mứt truyền thống Kim Long, TP. Huế', 'PARTNER-DAC-SAN-KIM-LONG', N'Cơ Sở Đặc Sản & Mứt Gừng Kim Long O Lành', N'Mè xửng giòn kẹp bánh tráng nướng vàng rụm, ngọt thanh mạch nha, bùi béo mè rang và đậu phộng quê hương, rất hợp thưởng thức cùng trà nóng.', 1, 1),
    ('SP004', N'Tôm Chua Cố Đô Kim Long Thượng Hạng', N'Đặc sản Huế', 85000, N'hũ 400g', 35, 'images/mon_tomchua.jpg', N'Cơ sở chế biến đặc sản Phường Kim Long, TP. Huế', 'PARTNER-DAC-SAN-KIM-LONG', N'Cơ Sở Đặc Sản & Mứt Gừng Kim Long O Lành', N'Tôm rằn sông tươi ủ mọng đậm đà cùng măng non, riềng, tỏi, ớt xiêm rừng; ăn kèm thịt ba chỉ luộc và rau thơm vườn Kim Long.', 1, 0),
    ('SP005', N'Mắm Ruốc Huế Truyền Thống Đậm Vị', N'Đặc sản Huế', 45000, N'hũ 350g', 40, 'images/mon_mamruoc.jpg', N'Cơ sở chế biến nước mắm & mắm ruốc cổ truyền Kim Long', 'PARTNER-DAC-SAN-KIM-LONG', N'Cơ Sở Đặc Sản & Mứt Gừng Kim Long O Lành', N'Mắm ruốc Kim Long thơm dịu vị tép tươi lên men truyền thống, gia vị bí truyền cho món bún bò chuẩn vị và món thịt kho ruốc sả.', 1, 0),
    ('SP006', N'Nem Chua & Tré Kim Long Gia Truyền', N'Đặc sản Huế', 70000, N'phần 10 chiếc', 30, 'images/mon_nemchua_tre.jpg', N'Gia đình nghệ nhân nem chả Kim Long, TP. Huế', 'PARTNER-DAC-SAN-KIM-LONG', N'Cơ Sở Đặc Sản & Mứt Gừng Kim Long O Lành', N'Tré bì heo giòn sần sật ủ thính riềng tỏi kết hợp nem chua lá ổi thơm nồng, gói lá chuối tiêu xanh, đặc sản nhắm rượu tao nhã.', 1, 0),
    ('SP007', N'Bánh Ép Huế Giòn Dai Vị Tôm Thịt', N'Đặc sản Huế', 40000, N'bịch 5 cái', 45, 'images/mon_banhep.jpg', N'Cơ sở bánh ép truyền thống Kim Long, TP. Huế', 'FOOD-BANH-BEO', N'Quán Bánh Bèo - Nậm - Lọc Bà Đỏ', N'Bánh ép bột lọc tôm thịt hành hoa ép nhiệt giòn dai cuốn rau răm, dưa leo chua ngọt chấm mắm nêm hoặc nước mắm tỏi ớt cay nồng.', 1, 0),
    ('SP008', N'Bánh Bèo - Nậm - Lọc Khay Cố Đô', N'Đặc sản Huế', 85000, N'mẹt 18 chiếc', 35, 'images/mon_banhbeonamloc.jpg', N'Quán bánh Huế đối tác phục vụ khách lưu trú Kim Long', 'FOOD-BANH-BEO', N'Quán Bánh Bèo - Nậm - Lọc Bà Đỏ', N'Mẹt bánh truyền thống gồm 10 chén bánh bèo tôm chấy da heo giòn, 4 bánh nậm lá dong nhân tôm thịt, 4 bánh bột lọc tôm thịt trong veo chấm mắm ớt xanh.', 1, 1),
    ('SP009', N'Bưởi Thanh Trà Kim Long Tuyển Chọn', N'Sản phẩm nhà vườn', 90000, N'kg (quả ~1kg)', 35, 'images/nongsan_thanhtra.jpg', N'Vườn cây ăn quả cổ thụ Phường Kim Long, TP. Huế', 'PARTNER-NV-THANH-TRA', N'Vườn Sinh Thái & Homestay Thanh Trà Cổ Thụ', N'Giống bưởi Thanh Trà tiến vua đặc hữu vùng phù sa sông Hương tại Kim Long: múi vàng óng, tép ráo giòn, vị ngọt thanh tao và hương thơm dịu mát.', 1, 1),
    ('SP010', N'Giỏ Rau Xanh Hữu Cơ Nhà Vườn Theo Mùa', N'Sản phẩm nhà vườn', 45000, N'giỏ 1kg', 25, 'images/nongsan_rauxanh.jpg', N'Tổ hợp tác nông sản sạch Kim Long, TP. Huế', 'PARTNER-HTX-NONG-SAN', N'HTX Nông Sản Sạch & Thanh Trà Kim Long', N'Rau cải non, xà lách, rau thơm, vả Huế và rau má canh tác hữu cơ tại các hộ nhà vườn liên kết, thu hoạch tươi mới mỗi buổi sáng.', 1, 0),
    ('SP011', N'Hạt Sen Tươi Hồ Tịnh Tâm Bóc Vỏ', N'Sản phẩm nhà vườn', 135000, N'gói 500g', 20, 'images/ho_sen_hue.jpg', N'Nhà vườn Hương Sen Kim Long, TP. Huế', 'PARTNER-NV-HUONG-SEN', N'Nhà Vườn Sinh Thái Hương Sen Kim Long', N'Hạt sen tươi bóc vỏ thông tâm trong ngày, hạt tròn đều, nấu chè hay hầm canh đều bở tơi, thơm ngát đặc trưng giống sen trắng Cố đô.', 1, 1),
    ('SP012', N'Trái Cây Miệt Vườn Sinh Thái Kim Long Theo Mùa', N'Sản phẩm nhà vườn', 50000, N'phần 1.5kg', 28, 'images/nongsan_traicay.jpg', N'Các hộ nhà vườn liên kết Phường Kim Long, TP. Huế', 'PARTNER-HTX-NONG-SAN', N'HTX Nông Sản Sạch & Thanh Trà Kim Long', N'Tập hợp trái cây chín cây tự nhiên từ các hộ nhà vườn Kim Long theo mùa vụ (chuối ngự, ổi sẻ, mít vườn, măng cụt, thanh trà hoặc nhãn lồng xứ Huế).', 1, 0),
    ('SP013', N'Nấm Bào Ngư & Nông Sản Củ Quả Nhà Vườn', N'Sản phẩm nhà vườn', 45000, N'phần 500g', 30, 'images/nongsan_cuqua.jpg', N'Vườn sinh thái Kim Long, TP. Huế', 'PARTNER-HTX-NONG-SAN', N'HTX Nông Sản Sạch & Thanh Trà Kim Long', N'Vả Huế tươi giòn, khoai mỡ tím, bí đỏ nhà vườn và nấm bào ngư tươi thu hoạch từ các hộ nông nghiệp sinh thái tuần hoàn tại Kim Long.', 1, 0),
    ('SP014', N'Giỏ Quà Nông Sản Sạch Sinh Thái Kim Long', N'Sản phẩm nhà vườn', 190000, N'giỏ quà', 18, 'images/gio_nongsan_sach.jpg', N'HTX Nông sản sạch & Mạng lưới nhà vườn Kim Long', 'PARTNER-HTX-NONG-SAN', N'HTX Nông Sản Sạch & Thanh Trà Kim Long', N'Giỏ mây tre đan tinh tế gồm: 1 quả Thanh Trà Kim Long, 1 giỏ rau xanh hữu cơ, 500g củ quả sạch và bó thảo mộc thơm phòng tự nhiên.', 1, 1),
    ('SP015', N'Trà Sen Búp Tươi Sương Mai Kim Long', N'Đồ uống và trà', 180000, N'hộp 10 bông', 20, 'images/qua_trasen.jpg', N'Không gian Trà Cung Đình Kim Long, TP. Huế', 'PARTNER-TRA-CUNG-DINH', N'Trà Thất & Đặc Sản Trà Cung Đình Kim Long', N'Trà xanh thượng hạng ướp thủ công trong búp sen tươi vào buổi sớm mai, giữ trọn hương sen thuần khiết và hậu vị ngọt sâu lắng.', 1, 1),
    ('SP016', N'Trà Thảo Mộc Hoa Cúc Atiso Cố Đô', N'Đồ uống và trà', 75000, N'hộp 20 túi lọc', 35, 'images/tra_thaomoc.jpg', N'Trà Thất & Dược liệu Kim Long, TP. Huế', 'PARTNER-TRA-CUNG-DINH', N'Trà Thất & Đặc Sản Trà Cung Đình Kim Long', N'Búp cúc chi vàng sấy lạnh hòa quyện hoa atiso đỏ, cỏ ngọt và cam thảo, mang lại vị thanh tao, mát gan hạ hỏa, tốt cho giấc ngủ.', 1, 0),
    ('SP017', N'Trà Gừng Sẻ Mật Ong Rừng Ấm Nồng', N'Đồ uống và trà', 60000, N'ấm 2 người', 40, 'images/tra_gung.jpg', N'Trà Thất & Nhà Vườn Kim Long, TP. Huế', 'PARTNER-TRA-CUNG-DINH', N'Trà Thất & Đặc Sản Trà Cung Đình Kim Long', N'Củ gừng sẻ Kim Long nướng thơm giã dập nấu cùng mật ong hoa tràm tự nhiên, ấm lòng ấm bụng trong những chiều mưa Cố Đô.', 1, 0),
    ('SP018', N'Nước Thảo Mộc Lá Sen & Lá Vối Thanh Mát', N'Đồ uống và trà', 25000, N'chai 500ml', 50, 'images/nuoc_thaomoc.jpg', N'Nhà vườn Hương Sen Kim Long, TP. Huế', 'PARTNER-NV-HUONG-SEN', N'Nhà Vườn Sinh Thái Hương Sen Kim Long', N'Nước đun từ lá sen bánh tẻ hồ Kim Long và nụ vối tươi ủ sành, giải khát thanh nhiệt bù khoáng thiên nhiên cực tốt cho cơ thể.', 1, 0),
    ('SP019', N'Nước Ép Bưởi Thanh Trà Tươi Kim Long', N'Đồ uống và trà', 45000, N'ly 350ml', 40, 'images/nuoc_thanhtra.jpg', N'Vườn Sinh Thái Thanh Trà Kim Long, TP. Huế', 'PARTNER-NV-THANH-TRA', N'Vườn Sinh Thái & Homestay Thanh Trà Cổ Thụ', N'Tép bưởi thanh trà mọng nước ép lạnh tươi nguyên chất, không đường phụ gia, giàu vitamin C và thoang thoảng hương tinh dầu hoa bưởi.', 1, 1),
    ('SP020', N'Bộ Ấm Trà Gốm & Danh Trà Cung Đình Quà Tặng', N'Đồ uống và trà', 320000, N'bộ quà', 15, 'images/bo_amtra_quatang.jpg', N'Làng nghề gốm & Trà thất Kim Long, TP. Huế', 'PARTNER-TRA-CUNG-DINH', N'Trà Thất & Đặc Sản Trà Cung Đình Kim Long', N'Bộ ấm chén gốm mộc Kim Long nung củi tinh xảo kèm 2 lon trà sen búp sấy lạnh thượng hạng trong hộp quà lụa vàng hoàng gia trang nhã.', 1, 1),
    ('SP021', N'Bánh Ướt Thịt Nướng Kim Long Trứ Danh', N'Ẩm thực Huế', 65000, N'suất giao nóng', 40, 'images/mon_banhuot.jpg', N'Quán đối tác đường Kim Long, Phường Kim Long, TP. Huế', 'FOOD-BANH-UOT', N'Bánh Ướt Thịt Nướng Huyền Anh (Đệ Nhất Kim Long)', N'Đặc sản trứ danh Kim Long: Bánh ướt tráng mỏng mềm cuốn thịt ba chỉ nướng than hoa thơm lừng, rau sống nhà vườn và nước chấm tương mè béo bùi.', 1, 1),
    ('SP022', N'Bún Bò Giò Heo Cố Đô Kim Long', N'Ẩm thực Huế', 55000, N'tô đặc biệt', 35, 'images/mon_bunbo.jpg', N'Quán bún bò truyền thống Kim Long, TP. Huế', 'FOOD-BUN-BO-HUE', N'Bún Bò Cố Đô Kim Long', N'Nước dùng ninh xương bò và sả tươi thơm lừng thoảng hương ruốc Huế, sợi bún to dai, giò heo giòn béo, chả cua quết mịn và hành hoa xanh mướt.', 1, 1),
    ('SP023', N'Cơm Hến & Bún Hến Hoa Chuối Kim Long', N'Ẩm thực Huế', 35000, N'suất', 40, 'images/mon_comhen.jpg', N'Cơ sở ẩm thực dân dã Kim Long, TP. Huế', 'FOOD-COM-HEN-HOA-DONG', N'Cơm Hến Hoa Đông (Đặc Sản Kim Long)', N'Cơm nguội thơm lá dứa trộn thịt hến xào đậm đà, tóp mỡ giòn rụm, đậu phộng rang, hoa chuối thái mỏng, ớt bột cay xè và chén nước hến ngọt thanh.', 1, 0),
    ('SP024', N'Bánh Khoái Hoàng Gia Kim Long Giòn Tan', N'Ẩm thực Huế', 60000, N'đĩa 2 chiếc', 30, 'images/mon_banhkhoai.jpg', N'Quán bánh Huế đối tác phục vụ khách lưu trú Kim Long', 'FOOD-BANH-BEO', N'Quán Bánh Bèo - Nậm - Lọc Bà Đỏ', N'Vỏ bánh chiên vàng rộm giòn tan nhân tôm đất bóc vỏ, thịt nạc, nấm rơm và giá đỗ tươi; chấm cùng nước lèo gan heo sánh mịn béo ngậy.', 1, 0),
    ('SP025', N'Nem Lụi Nướng Sả Than Hoa Kèm Rau Sống Vườn', N'Ẩm thực Huế', 75000, N'phần 8 cây', 30, 'images/mon_nemlui.jpg', N'Quán đối tác đường Kim Long, TP. Huế', 'FOOD-BANH-UOT', N'Bánh Ướt Thịt Nướng Huyền Anh (Đệ Nhất Kim Long)', N'Nem lụi quấn sả tươi nướng than hồng, ăn kèm bánh tráng mỏng, vả Huế, rau sống vườn Kim Long và nước chấm tương đậu phộng sánh béo.', 1, 0),
    ('SP026', N'Mâm Cơm Gia Đình Huế 5 Món Phục Vụ Tại Phòng', N'Ẩm thực Huế', 165000, N'mâm (2 khách)', 20, 'images/mam_com_hue.jpg', N'Bếp nhà vườn sinh thái Kim Long EcoStay', 'PARTNER-NV-HUONG-SEN', N'Bếp Nhà Vườn Hương Sen Kim Long', N'Suất ăn ấm cúng phục vụ trực tiếp tại buồng phòng gồm: Cá kho tộ, thịt luộc chấm tôm chua, canh rau tập tàng nấu cua đồng, vả trộn tôm thịt và dưa giá Cố đô.', 1, 1),
    ('SP027', N'Set Điểm Tâm Sáng Cố Đô Phục Vụ Tận Phòng', N'Ẩm thực Huế', 85000, N'combo 1 khách', 30, 'images/set_diem_tam.jpg', N'Bếp Sinh Thái Kim Long EcoStay', 'KLE-CENTER', N'Bếp Sinh Thái Kim Long EcoStay', N'Tự chọn Bún bò giò heo hoặc Bánh ướt thịt nướng + 1 ly Nước ép bưởi Thanh Trà tươi hoặc Cà phê pha phin + 1 búp hoa sen thơm ngát dọn tận phòng.', 1, 1),
    ('SP028', N'Hộp Bánh Đậu Xanh Trái Cây Cung Đình & Kẹo Cau', N'Quà lưu niệm và trải nghiệm', 75000, N'hộp 250g', 30, 'images/qua_setquatang.jpg', N'Phường Kim Long, TP. Huế', 'PARTNER-DAC-SAN-KIM-LONG', N'Cơ Sở Đặc Sản & Mứt Gừng Kim Long O Lành', N'Bánh đậu xanh trái cây nặn thủ công từng quả nhỏ sắc màu tự nhiên từ củ dền, gấc, lá dứa; kết hợp kẹo cau truyền thống thơm ngọt làm quà biếu.', 1, 0),
    ('SP029', N'Combo Quà Tặng "Kim Long Tinh Hoa Ngự Phẩm"', N'Quà lưu niệm và trải nghiệm', 350000, N'hộp quà gỗ', 15, 'images/qua_combokimlong.jpg', N'Mạng lưới liên kết đặc sản Kim Long EcoStay, TP. Huế', 'PARTNER-DAC-SAN-KIM-LONG', N'Mạng Lưới Liên Kết Đặc Sản Kim Long', N'Hộp quà gỗ thông sinh thái sang trọng gồm: 1 gói Trà Cung Đình Bát Bảo, 1 hộp Mứt gừng sẻ Kim Long, 1 hộp Kẹo mè xửng giòn và 1 chai Tinh dầu tràm gió Thủy Xuân.', 1, 1),
    ('SP030', N'Gói Trải Nghiệm Văn Hóa Làm Bánh Ướt & Khám Phá Nhà Vườn', N'Quà lưu niệm và trải nghiệm', 250000, N'vé/khách', 12, 'images/trainghiem_nhavuon.jpg', N'Nông trại sinh thái & Nhà vườn Kim Long, TP. Huế', 'EXP-KLE-FARM', N'Nông Trại Trải Nghiệm Sinh Thái Kim Long', N'Trải nghiệm 2 giờ thú vị: Tham quan vườn thanh trà cổ thụ, tự tay tráng bánh ướt Kim Long trên lò than củi, nếm thử mứt gừng ấm nóng và thưởng trà cùng nghệ nhân.', 1, 1)
) AS source (ProductCode, Name, Category, Price, Unit, StockQuantity, ImageLink, Origin, PartnerId, PartnerName, Description, IsActive, IsFeatured)
ON target.ProductCode = source.ProductCode
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        Category = source.Category,
        Price = source.Price,
        Unit = source.Unit,
        ImageLink = source.ImageLink,
        Origin = source.Origin,
        PartnerId = source.PartnerId,
        PartnerName = source.PartnerName,
        Description = source.Description
WHEN NOT MATCHED THEN
    INSERT (ProductCode, Name, Category, Price, Unit, StockQuantity, ImageLink, Origin, PartnerId, PartnerName, Description, IsActive, IsFeatured, IsSample, CreatedAt, UpdatedAt, SyncVersion)
    VALUES (source.ProductCode, source.Name, source.Category, source.Price, source.Unit, source.StockQuantity, source.ImageLink, source.Origin, source.PartnerId, source.PartnerName, source.Description, source.IsActive, source.IsFeatured, 0, SYSUTCDATETIME(), SYSUTCDATETIME(), 1);
GO

-- Xóa các mã SP- cũ trùng lặp nếu có từ bản thử nghiệm trước
DELETE FROM dbo.SpecialtyProducts WHERE ProductCode LIKE 'SP-%';
GO

-- ==========================================================================================
-- 4. BẢNG ĐỐI TÁC ĐỊA PHƯƠNG & ĐIỂM BẢN ĐỒ SỐ (dbo.LocalPartners)
-- ==========================================================================================
IF OBJECT_ID(N'dbo.LocalPartners', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LocalPartners (
        PartnerId VARCHAR(50) CONSTRAINT PK_LocalPartners PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        PartnerGroup NVARCHAR(100) NOT NULL,
        Latitude FLOAT NOT NULL,
        Longitude FLOAT NOT NULL,
        Address NVARCHAR(300) NOT NULL,
        DistanceKm FLOAT NOT NULL DEFAULT (0),
        Phone VARCHAR(50) NULL,
        OpenHours NVARCHAR(100) NULL,
        ImageUrl NVARCHAR(500) NULL,
        Description NVARCHAR(MAX) NULL,
        ServicesJson NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL DEFAULT (1),
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_LocalPartners_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_LocalPartners_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        SyncVersion INT NOT NULL CONSTRAINT DF_LocalPartners_SyncVersion DEFAULT (1)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.LocalPartners)
BEGIN
    INSERT INTO dbo.LocalPartners (PartnerId, Name, Category, PartnerGroup, Latitude, Longitude, Address, DistanceKm, Phone, OpenHours, ImageUrl, Description, ServicesJson, IsActive)
    VALUES
    ('KLE-CENTER', N'Kim Long EcoStay (Tâm Điểm Xuất Phát)', N'Nhà vườn và homestay', N'Nhà vườn và homestay', 16.4560, 107.5495, N'Khu vực Nhà vườn Kim Long, Ven Sông Hương, Phường Kim Long, TP. Huế', 0.0, '0905 123 456', N'Mở cửa 24/7 (Smart Check-in không chạm)', 'images/nha_ruong_1.jpg', N'Trung tâm điều phối lưu trú sinh thái, trạm xe đạp điện Eco-Bike và không gian thưởng trà Cung Đình Huế.', N'["Lưu trú sinh thái","SmartLock 10s","Xe đạp Eco-Bike"]', 1),
    ('PARTNER-NV-HUONG-SEN', N'Nhà Vườn Sinh Thái Hương Sen Kim Long', N'Nhà vườn và homestay', N'Nhà vườn và homestay', 16.4572, 107.5482, N'Kiệt 82 Kim Long, Phường Kim Long, TP. Huế', 0.18, '0914 234 567', N'06:30 - 21:00 hàng ngày', 'images/nha_ruong_1.jpg', N'Nhà vườn cổ truyền kết hợp hồ sen trắng và bếp gia đình phục vụ mâm cơm Huế ấm cúng cho khách lưu trú.', N'["Cơm gia đình Huế","Hồ sen Tịnh Tâm","Nước thảo mộc"]', 1),
    ('PARTNER-NV-THANH-TRA', N'Vườn Sinh Thái & Homestay Thanh Trà Cổ Thụ', N'Nhà vườn và homestay', N'Nhà vườn và homestay', 16.4585, 107.5465, N'Đường Nguyễn Hoàng, Phường Kim Long, TP. Huế', 0.35, '0935 345 678', N'07:00 - 18:30 hàng ngày', 'images/nha_ruong_3.jpg', N'Khu vườn bưởi Thanh Trà 40 năm tuổi rợp bóng mát, trải nghiệm hái quả và thưởng thức nước ép thanh trà tươi.', N'["Vườn Thanh Trà 40 năm","Nước ép tươi","Tham quan vườn"]', 1),
    ('HERITAGE-AN-HIEN', N'Nhà Vườn An Hiên (Đệ Nhất Nhà Vườn Xứ Huế)', N'Nhà vườn và homestay', N'Nhà vườn và homestay', 16.4542, 107.5471, N'58 Phú Mộng, Phường Kim Long, TP. Huế', 0.45, '0234 3523 456', N'07:30 - 17:30 hàng ngày', 'images/nha_ruong_2.jpg', N'Biểu tượng kiến trúc nhà rường quý tộc Cố Đô với cổng vòm cổ kính, hàng mơ xanh mướt và hồ tiểu cảnh phong thủy.', N'["Di sản kiến trúc","Chụp ảnh cổ phục","Thuyết minh lịch sử"]', 1),
    ('FOOD-BANH-UOT', N'Bánh Ướt Thịt Nướng Huyền Anh (Đệ Nhất Kim Long)', N'Quán ăn và ẩm thực', N'Quán ăn và ẩm thực', 16.4568, 107.5475, N'50/5 Kim Long, Phường Kim Long, TP. Huế', 0.25, '0234 3525 655', N'08:30 - 21:00 hàng ngày', 'images/mon_banhuot.jpg', N'Quán bánh ướt thịt nướng than hoa và bún thịt nướng gia truyền nức tiếng làng cổ Kim Long suốt hơn 30 năm.', N'["Bánh ướt thịt nướng","Nem lụi than hoa","Giao tận phòng 15p"]', 1),
    ('FOOD-BANH-BEO', N'Quán Bánh Bèo - Nậm - Lọc Bà Đỏ', N'Quán ăn và ẩm thực', N'Quán ăn và ẩm thực', 16.4564, 107.5518, N'71 Nguyễn Bỉnh Khiêm / Liên kết Kim Long, TP. Huế', 0.65, '0234 3527 890', N'08:00 - 20:30 hàng ngày', 'images/mon_banhbeonamloc.jpg', N'Đối tác cung cấp mẹt bánh bèo chén tôm chấy, bánh nậm lá dong, bánh bột lọc và bánh khoái nóng giòn.', N'["Mẹt bánh Huế","Bánh khoái","Bánh ép Huế"]', 1),
    ('PARTNER-DAC-SAN-KIM-LONG', N'Cơ Sở Đặc Sản & Mứt Gừng Kim Long O Lành', N'Đặc sản và sản phẩm địa phương', N'Đặc sản và sản phẩm địa phương', 16.4578, 107.5512, N'Đường Kim Long, Phường Kim Long, TP. Huế', 0.28, '0905 456 789', N'07:00 - 20:30 hàng ngày', 'images/qua_mutgung.jpg', N'Lò chế biến mứt gừng sẻ Kim Long, mè xửng giòn, tôm chua và nem chua tré gia truyền đạt chuẩn OCOP.', N'["Mứt gừng Kim Long","Mè xửng giòn","Tôm chua & Mắm ruốc"]', 1),
    ('PARTNER-TRA-CUNG-DINH', N'Trà Thất & Đặc Sản Trà Cung Đình Kim Long', N'Đặc sản và sản phẩm địa phương', N'Đặc sản và sản phẩm địa phương', 16.4552, 107.5525, N'Đường Kim Long, Phường Kim Long, TP. Huế', 0.40, '0983 567 890', N'07:30 - 21:30 hàng ngày', 'images/tra_cung_dinh.jpg', N'Không gian thưởng trà và cung cấp trà Cung Đình Bát Bảo 16 vị thảo mộc, trà sen búp tươi và bộ ấm trà gốm.', N'["Trà Cung Đình 16 vị","Trà sen búp tươi","Bộ ấm trà quà tặng"]', 1),
    ('PARTNER-HTX-NONG-SAN', N'HTX Nông Sản Sạch & Thanh Trà Kim Long', N'Đặc sản và sản phẩm địa phương', N'Đặc sản và sản phẩm địa phương', 16.4595, 107.5450, N'Khu nông nghiệp bãi bồi ven sông, Kim Long, TP. Huế', 0.50, '0912 678 901', N'06:00 - 18:00 hàng ngày', 'images/nongsan_thanhtra.jpg', N'Hợp tác xã quy tụ các hộ nhà vườn trồng rau hữu cơ, củ quả sạch, nấm bào ngư và trái cây theo mùa tại Kim Long.', N'["Rau xanh hữu cơ","Giỏ quà nông sản","Trái cây theo mùa"]', 1),
    ('EXP-ECO-BIKE-KIM-LONG', N'Trạm Xe Đạp Sinh Thái Eco-Bike & Bến Thuyền Kim Long', N'Điểm trải nghiệm', N'Điểm trải nghiệm', 16.4555, 107.5505, N'Bến thuyền Kim Long ven sông Hương, TP. Huế', 0.15, '0905 123 456', N'06:00 - 21:00 hàng ngày', 'images/hero_ecostay.jpg', N'Điểm xuất phát hành trình đạp xe trợ lực điện dọc bờ sông Hương đi Chùa Thiên Mụ kèm thuyết minh GPS Audio Guide.', N'["Xe đạp trợ lực điện","GPS Audio Guide","Ngắm hoàng hôn sông Hương"]', 1);
END
GO

-- ==========================================================================================
-- 5. BẢNG ĐƠN GIAO PHÒNG & ĐẶT MÓN TẬN PHÒNG (dbo.RoomDeliveries)
-- ==========================================================================================
IF OBJECT_ID(N'dbo.RoomDeliveries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoomDeliveries (
        Id INT IDENTITY(1,1) CONSTRAINT PK_RoomDeliveries PRIMARY KEY,
        OrderCode VARCHAR(50) NOT NULL CONSTRAINT UQ_RoomDeliveries_Code UNIQUE,
        DeliveryMethod VARCHAR(30) NOT NULL DEFAULT ('ROOM_DELIVERY'),
        RoomNumber VARCHAR(20) NOT NULL,
        RoomName NVARCHAR(150) NOT NULL,
        BookingCode VARCHAR(50) NULL,
        CustomerName NVARCHAR(100) NOT NULL,
        Phone VARCHAR(30) NOT NULL,
        Email VARCHAR(100) NULL,
        DeliveryTime NVARCHAR(100) NOT NULL,
        ServiceNotes NVARCHAR(500) NULL,
        PaymentMethod VARCHAR(30) NOT NULL DEFAULT ('VIETQR'),
        PaymentStatus VARCHAR(30) NOT NULL DEFAULT ('PAID'),
        ItemsJson NVARCHAR(MAX) NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL DEFAULT (0),
        ShippingFee DECIMAL(18,2) NOT NULL DEFAULT (0),
        Total DECIMAL(18,2) NOT NULL DEFAULT (0),
        Status VARCHAR(30) NOT NULL DEFAULT ('PENDING'), -- PENDING, CONFIRMED, PREPARING, DELIVERING, DELIVERED, CANCELLED
        StatusHistoryJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_RoomDeliveries_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_RoomDeliveries_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        SyncVersion INT NOT NULL CONSTRAINT DF_RoomDeliveries_SyncVersion DEFAULT (1)
    );
    CREATE INDEX IX_RoomDeliveries_Room ON dbo.RoomDeliveries (RoomNumber, Status);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.RoomDeliveries)
BEGIN
    INSERT INTO dbo.RoomDeliveries (OrderCode, DeliveryMethod, RoomNumber, RoomName, BookingCode, CustomerName, Phone, Email, DeliveryTime, ServiceNotes, PaymentMethod, PaymentStatus, ItemsJson, Subtotal, ShippingFee, Total, Status, StatusHistoryJson)
    VALUES
    ('KLE-DELIV-20261029-01', 'ROOM_DELIVERY', 'KLE-R101', N'Eco-Nest Ban Mai (Phòng KLE-R101 • Tầng 1)', 'KLE-20261018-01', N'Nguyễn Văn An', '0905123456', 'khachhang@example.com', N'Giao ngay (15 - 30 phút)', N'Giao 2 đôi đũa phụ, không ớt trong nước chấm, gõ cửa trước khi giao', 'VIETQR', 'PAID',
     N'[{"id":"SP021","name":"Bánh Ướt Thịt Nướng Kim Long Trứ Danh","price":65000,"qty":2,"unit":"suất giao nóng","img":"images/mon_banhuot.jpg","category":"Ẩm thực Huế"},{"id":"SP019","name":"Nước Ép Bưởi Thanh Trà Tươi Kim Long","price":45000,"qty":2,"unit":"ly 350ml","img":"images/nuoc_thanhtra.jpg","category":"Đồ uống và trà"}]',
     220000, 0, 220000, 'DELIVERING',
     N'[{"status":"PENDING","note":"Đơn hàng được tạo thành công bởi khách lưu trú."},{"status":"CONFIRMED","note":"Lễ tân đã tiếp nhận và chuyển bếp chế biến."},{"status":"PREPARING","note":"Bếp đã hoàn tất món nướng nóng hổi."},{"status":"DELIVERING","note":"Nhân viên phục vụ đang mang món lên cửa phòng R101."}]'),
    ('KLE-DELIV-20261029-02', 'ROOM_DELIVERY', 'KLE-R201', N'Garden Suite Cố Đô (Phòng KLE-R201 • Tầng 2)', 'KLE-20261014-42', N'Đặng Thanh Nga', '0988776655', 'nga.dang@example.com', N'11:30 Trưa (Bữa trưa)', N'Chuẩn bị mâm cơm trước 11h30, dọn thêm chén bát cho 2 người lớn, 1 bé', 'CASH', 'UNPAID',
     N'[{"id":"SP026","name":"Mâm Cơm Gia Đình Huế 5 Món Phục Vụ Tại Phòng","price":165000,"qty":1,"unit":"mâm (2 khách)","img":"images/mam_com_hue.jpg","category":"Ẩm thực Huế"},{"id":"SP016","name":"Trà Thảo Mộc Hoa Cúc Atiso Cố Đô","price":75000,"qty":1,"unit":"hộp 20 túi lọc","img":"images/tra_thaomoc.jpg","category":"Đồ uống và trà"}]',
     240000, 0, 240000, 'PREPARING',
     N'[{"status":"PENDING","note":"Đơn hàng được đặt thành công."},{"status":"CONFIRMED","note":"Lễ tân đã xác nhận lịch hẹn 11:30."},{"status":"PREPARING","note":"Bếp nhà vườn đang nấu mâm cơm 5 món."}]'),
    ('KLE-DELIV-20261029-03', 'ROOM_DELIVERY', 'KLE-R102', N'Eco-Nest Hoàng Hôn (Phòng KLE-R102 • Tầng 1)', 'KLE-20261015-88', N'Hoàng Minh Tuấn', '0912334455', 'tuan.hoang@example.com', N'07:30 Sáng (Điểm tâm tại phòng)', N'Bún bò nhiều hành lá giò heo mềm thơm, mang thêm chanh ớt tươi', 'VIETQR', 'PAID',
     N'[{"id":"SP022","name":"Bún Bò Giò Heo Cố Đô Kim Long","price":55000,"qty":2,"unit":"tô đặc biệt","img":"images/mon_bunbo.jpg","category":"Ẩm thực Huế"},{"id":"SP018","name":"Nước Thảo Mộc Lá Sen & Lá Vối Thanh Mát","price":25000,"qty":2,"unit":"chai 500ml","img":"images/nuoc_thaomoc.jpg","category":"Đồ uống và trà"}]',
     160000, 0, 160000, 'DELIVERED',
     N'[{"status":"PENDING","note":"Đơn hàng được tạo thành công."},{"status":"DELIVERED","note":"Khách hàng đã nhận món tại phòng, chúc ngon miệng."}]');
END
GO

-- ==========================================================================================
-- 6. BẢNG KHÓA THÔNG MINH SMARTLOCK IOT (dbo.SmartLockPins) & NHẬT KÝ AUDIT (dbo.SmartLockAuditLogs)
-- (Tuyệt đối không lộ mã PIN plaintext trên giao diện - chỉ lưu hash/masked và ghi Audit Log)
-- ==========================================================================================
IF OBJECT_ID(N'dbo.SmartLockPins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SmartLockPins (
        Id INT IDENTITY(1,1) CONSTRAINT PK_SmartLockPins PRIMARY KEY,
        RoomNumber VARCHAR(20) NOT NULL CONSTRAINT UQ_SmartLockPins_Room UNIQUE,
        RoomName NVARCHAR(150) NOT NULL,
        BookingCode VARCHAR(50) NULL,
        GuestName NVARCHAR(100) NULL,
        GuestPhone VARCHAR(30) NULL,
        PinHash VARCHAR(128) NOT NULL,        -- SHA-256 Hash của mã PIN
        MaskedDisplay VARCHAR(20) NOT NULL DEFAULT ('••••••'),
        PinStatus VARCHAR(30) NOT NULL DEFAULT ('ACTIVE'), -- ACTIVE, STANDBY, EXPIRED, REVOKED
        BatteryPercent INT NOT NULL DEFAULT (95),
        ValidFrom DATETIME2(0) NULL,
        ValidUntil DATETIME2(0) NULL,
        LastReissuedBy NVARCHAR(100) NULL,
        LastReissuedReason NVARCHAR(300) NULL,
        UpdatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_SmartLockPins_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        SyncVersion INT NOT NULL CONSTRAINT DF_SmartLockPins_SyncVersion DEFAULT (1)
    );
END
GO

IF OBJECT_ID(N'dbo.SmartLockAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SmartLockAuditLogs (
        Id INT IDENTITY(1,1) CONSTRAINT PK_SmartLockAuditLogs PRIMARY KEY,
        LogCode VARCHAR(50) NOT NULL,
        RoomNumber VARCHAR(20) NOT NULL,
        BookingCode VARCHAR(50) NULL,
        ActionType VARCHAR(50) NOT NULL,      -- REISSUE_PIN, AUTO_ISSUE_ON_DEPOSIT, REVOKE_ON_CHECKOUT
        PerformedBy NVARCHAR(100) NOT NULL,
        RoleName VARCHAR(50) NOT NULL,
        Reason NVARCHAR(500) NOT NULL,
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_SmartLockAuditLogs_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
    CREATE INDEX IX_SmartLockAuditLogs_Created ON dbo.SmartLockAuditLogs (CreatedAt DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SmartLockPins)
BEGIN
    INSERT INTO dbo.SmartLockPins (RoomNumber, RoomName, BookingCode, GuestName, GuestPhone, PinHash, MaskedDisplay, PinStatus, BatteryPercent, ValidFrom, ValidUntil, LastReissuedBy, LastReissuedReason)
    VALUES
    ('KLE-R101', N'Eco-Nest Ban Mai (KLE-R101)', 'KLE-20261018-01', N'Nguyễn Văn An', '0905123456', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(NEWID() AS VARCHAR(64))), 2), '••••••', 'STANDBY', 98, '2026-10-18 14:00:00', '2026-10-20 12:00:00', N'Hệ Thống Tự Động', N'[DEMO-SIMULATION] Đơn chờ đối soát cọc 50%'),
    ('KLE-R102', N'Eco-Nest Hoàng Hôn (KLE-R102)', 'KLE-20261015-88', N'Hoàng Minh Tuấn', '0912334455', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(NEWID() AS VARCHAR(64))), 2), '••••••', 'ACTIVE', 94, '2026-10-16 14:00:00', '2026-10-18 12:00:00', N'Hệ Thống VietQR', N'[DEMO-SIMULATION] Xác nhận đặt cọc 50% thành công'),
    ('KLE-R201', N'Garden Suite Cố Đô Thanh Trà (KLE-R201)', 'KLE-20261014-42', N'Đặng Thanh Nga', '0988776655', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(NEWID() AS VARCHAR(64))), 2), '••••••', 'ACTIVE', 91, '2026-10-14 14:00:00', '2026-10-16 12:00:00', N'Đặng Thu Thảo (Lễ Tân)', N'[DEMO-SIMULATION] Khách đã Check-in nhận phòng'),
    ('KLE-R202', N'Family Eco-Villa Gắn Kết (KLE-R202)', NULL, NULL, NULL, CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(NEWID() AS VARCHAR(64))), 2), '••••••', 'STANDBY', 96, NULL, NULL, N'Hệ Thống IoT', N'[DEMO-SIMULATION] Chế độ chờ khách đặt phòng'),
    ('KLE-R203', N'Heritage Studio Trầm Mặc (KLE-R203)', 'KLE-20261010-19', N'Lê Hoàng Long', '0933221100', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(NEWID() AS VARCHAR(64))), 2), '••••••', 'EXPIRED', 89, '2026-10-10 14:00:00', '2026-10-12 12:00:00', N'Hệ Thống IoT', N'[DEMO-SIMULATION] Đã thu hồi tự động sau khi khách Check-out');

    INSERT INTO dbo.SmartLockAuditLogs (LogCode, RoomNumber, BookingCode, ActionType, PerformedBy, RoleName, Reason)
    VALUES
    ('SL-LOG-001', 'KLE-R101', 'KLE-20261018-01', 'AUTO_ISSUE_ON_DEPOSIT', N'Hệ Thống VietQR Cloud', 'SYSTEM', N'Tự động mã hóa và gửi mã mở cửa SmartLock qua Email khách hàng'),
    ('SL-LOG-002', 'KLE-R201', 'KLE-20261014-42', 'REISSUE_PIN', N'Đặng Thu Thảo', 'RECEPTIONIST', N'Cấp lại mã khóa mới theo yêu cầu của khách Đặng Thanh Nga tại quầy lễ tân');
END
GO

-- ==========================================================================================
-- 7. MỞ RỘNG BẢNG ĐẶT PHÒNG (dbo.Bookings) ĐỂ ĐỒNG BỘ HAI CHIỀU ĐẦY ĐỦ VỚI WEBSITE
-- ==========================================================================================
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Bookings_Status')
BEGIN
    ALTER TABLE dbo.Bookings DROP CONSTRAINT CK_Bookings_Status;
END
GO

ALTER TABLE dbo.Bookings ADD CONSTRAINT CK_Bookings_Status
    CHECK (Status IN ('PENDING_DEPOSIT', 'WAITING_RECONCILIATION', 'CONFIRMED', 'CHECKED_IN', 'CHECKED_OUT', 'COMPLETED', 'CANCELLED'));
GO

IF COL_LENGTH('dbo.Bookings', 'GuestName') IS NULL
    ALTER TABLE dbo.Bookings ADD GuestName NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.Bookings', 'GuestPhone') IS NULL
    ALTER TABLE dbo.Bookings ADD GuestPhone VARCHAR(30) NULL;
IF COL_LENGTH('dbo.Bookings', 'GuestEmail') IS NULL
    ALTER TABLE dbo.Bookings ADD GuestEmail VARCHAR(100) NULL;
IF COL_LENGTH('dbo.Bookings', 'RoomName') IS NULL
    ALTER TABLE dbo.Bookings ADD RoomName NVARCHAR(150) NULL;
IF COL_LENGTH('dbo.Bookings', 'PaymentStatus') IS NULL
    ALTER TABLE dbo.Bookings ADD PaymentStatus VARCHAR(40) NOT NULL CONSTRAINT DF_Bookings_PaymentStatus DEFAULT ('WAITING_RECONCILIATION');
IF COL_LENGTH('dbo.Bookings', 'PaymentMethod') IS NULL
    ALTER TABLE dbo.Bookings ADD PaymentMethod VARCHAR(40) NOT NULL CONSTRAINT DF_Bookings_PaymentMethod DEFAULT ('VietQR');
IF COL_LENGTH('dbo.Bookings', 'TransferContent') IS NULL
    ALTER TABLE dbo.Bookings ADD TransferContent VARCHAR(100) NULL;
IF COL_LENGTH('dbo.Bookings', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.Bookings ADD UpdatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Bookings_UpdatedAt DEFAULT (SYSUTCDATETIME());
IF COL_LENGTH('dbo.Bookings', 'SyncVersion') IS NULL
    ALTER TABLE dbo.Bookings ADD SyncVersion INT NOT NULL CONSTRAINT DF_Bookings_SyncVersion DEFAULT (1);
GO

-- Cập nhật thông tin đầy đủ cho các đơn đặt phòng mẫu trong dbo.Bookings
UPDATE b
SET b.GuestName = ISNULL(b.GuestName, g.FullName),
    b.GuestPhone = ISNULL(b.GuestPhone, g.PhoneNumber),
    b.GuestEmail = ISNULL(b.GuestEmail, g.Email),
    b.RoomName = ISNULL(b.RoomName, N'Garden Suite Cố Đô Thanh Trà (KLE-R201)'),
    b.PaymentStatus = CASE WHEN b.Status IN ('CONFIRMED', 'CHECKED_IN', 'COMPLETED') THEN 'CONFIRMED' ELSE 'WAITING_RECONCILIATION' END,
    b.TransferContent = ISNULL(b.TransferContent, 'KLE ' + REPLACE(b.BookingCode, '-', '') + ' COC50')
FROM dbo.Bookings b
LEFT JOIN dbo.Guests g ON b.GuestId = g.Id;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Bookings WHERE BookingCode = 'KLE-20261018-01')
BEGIN
    INSERT INTO dbo.Bookings (BookingCode, GuestId, GuestName, GuestPhone, GuestEmail, RoomName, CheckInDate, CheckOutDate, TotalGuests, TotalAmount, DepositAmount, Status, PaymentStatus, PaymentMethod, TransferContent, Notes)
    VALUES
    ('KLE-20261018-01', 1, N'Nguyễn Văn An', '0905123456', 'khachhang@example.com', N'Eco-Nest Ban Mai (KLE-R101)', '2026-10-18', '2026-10-20', 2, 780000, 390000, 'PENDING_DEPOSIT', 'WAITING_RECONCILIATION', 'VietQR', 'KLE 2026101801 COC50', N'Khách đặt trực tuyến chờ đối soát cọc 50%'),
    ('KLE-20261015-88', 2, N'Hoàng Minh Tuấn', '0912334455', 'tuan.hoang@example.com', N'Eco-Nest Hoàng Hôn (KLE-R102)', '2026-10-16', '2026-10-18', 2, 840000, 420000, 'CONFIRMED', 'CONFIRMED', 'VietQR', 'KLE 2026101588 COC50', N'Đã đối soát cọc VietQR 50%'),
    ('KLE-20261014-42', 3, N'Đặng Thanh Nga', '0988776655', 'nga.dang@example.com', N'Garden Suite Cố Đô (KLE-R201)', '2026-10-14', '2026-10-16', 3, 1100000, 550000, 'CHECKED_IN', 'CHECKED_IN', 'VietQR', 'KLE 2026101442 COC50', N'Khách đang lưu trú tại phòng R201'),
    ('KLE-20261010-19', 1, N'Lê Hoàng Long', '0933221100', 'long.le@example.com', N'Heritage Studio Trầm Mặc (KLE-R203)', '2026-10-10', '2026-10-12', 2, 980000, 490000, 'CHECKED_OUT', 'COMPLETED', 'VietQR', 'KLE 2026101019 COC50', N'Đã tất toán 100% và trả phòng');
END
GO

-- Đảm bảo cột UpdatedAt & SyncVersion có trên Employees, Payrolls, FinancialTransactions
IF COL_LENGTH('dbo.Employees', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.Employees ADD UpdatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Employees_UpdatedAt DEFAULT (SYSUTCDATETIME());
IF COL_LENGTH('dbo.Employees', 'SyncVersion') IS NULL
    ALTER TABLE dbo.Employees ADD SyncVersion INT NOT NULL CONSTRAINT DF_Employees_SyncVersion DEFAULT (1);

IF COL_LENGTH('dbo.Payrolls', 'SyncVersion') IS NULL
    ALTER TABLE dbo.Payrolls ADD SyncVersion INT NOT NULL CONSTRAINT DF_Payrolls_SyncVersion DEFAULT (1);

IF COL_LENGTH('dbo.FinancialTransactions', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.FinancialTransactions ADD UpdatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_FinancialTransactions_UpdatedAt DEFAULT (SYSUTCDATETIME());
IF COL_LENGTH('dbo.FinancialTransactions', 'SyncVersion') IS NULL
    ALTER TABLE dbo.FinancialTransactions ADD SyncVersion INT NOT NULL CONSTRAINT DF_FinancialTransactions_SyncVersion DEFAULT (1);
GO

-- ==========================================================================================
-- 8. BẬT SQL SERVER CHANGE TRACKING TRÊN CÁC BẢNG NGHIỆP VỤ CHÍNH
-- ==========================================================================================
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(N'dbo.SpecialtyProducts'))
    ALTER TABLE dbo.SpecialtyProducts ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(N'dbo.Rooms'))
    ALTER TABLE dbo.Rooms ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(N'dbo.Bookings'))
    ALTER TABLE dbo.Bookings ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(N'dbo.RoomDeliveries'))
    ALTER TABLE dbo.RoomDeliveries ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(N'dbo.SmartLockPins'))
    ALTER TABLE dbo.SmartLockPins ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(N'dbo.LocalPartners'))
    ALTER TABLE dbo.LocalPartners ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(N'dbo.Employees'))
    ALTER TABLE dbo.Employees ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(N'dbo.Payrolls'))
    ALTER TABLE dbo.Payrolls ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);
IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(N'dbo.FinancialTransactions'))
    ALTER TABLE dbo.FinancialTransactions ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);
GO

-- ==========================================================================================
-- 9. TRIGGERS ĐỒNG BỘ TỰ ĐỘNG KHI THAY ĐỔI TRỰC TIẾP TRONG SSMS HOẶC STORED PROCEDURE
-- ==========================================================================================

-- 9.1. Trigger trên bảng SpecialtyProducts
CREATE OR ALTER TRIGGER dbo.trg_Sync_SpecialtyProducts
ON dbo.SpecialtyProducts
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    DECLARE @Action VARCHAR(20) = CASE
        WHEN EXISTS(SELECT 1 FROM inserted) AND EXISTS(SELECT 1 FROM deleted) THEN 'UPDATE'
        WHEN EXISTS(SELECT 1 FROM inserted) THEN 'INSERT'
        ELSE 'DELETE'
    END;

    IF @Action IN ('INSERT', 'UPDATE')
    BEGIN
        UPDATE p
        SET p.UpdatedAt = SYSUTCDATETIME(),
            p.SyncVersion = p.SyncVersion + 1
        FROM dbo.SpecialtyProducts p
        INNER JOIN inserted i ON p.Id = i.Id;

        INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
        SELECT 'products', i.ProductCode, @Action, 'SQL_ENGINE', SUSER_SNAME(),
               N'Đồng bộ sản phẩm ' + i.ProductCode + N' (' + i.Name + N') - Giá: ' + CAST(CAST(i.Price AS INT) AS NVARCHAR(30)) + N'đ, Tồn: ' + CAST(i.StockQuantity AS NVARCHAR(20)),
               ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0)
        FROM inserted i;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
        SELECT 'products', d.ProductCode, 'DELETE', 'SQL_ENGINE', SUSER_SNAME(),
               N'Xóa sản phẩm ' + d.ProductCode + N' (' + d.Name + N') khỏi CSDL SQL',
               ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0)
        FROM deleted d;
    END
END;
GO

-- 9.2. Trigger trên bảng Rooms
CREATE OR ALTER TRIGGER dbo.trg_Sync_Rooms
ON dbo.Rooms
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    IF EXISTS(SELECT 1 FROM inserted)
    BEGIN
        UPDATE r
        SET r.UpdatedAt = SYSUTCDATETIME(),
            r.SyncVersion = r.SyncVersion + 1
        FROM dbo.Rooms r
        INNER JOIN inserted i ON r.Id = i.Id;

        INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
        SELECT 'rooms', i.RoomNumber, 'UPDATE', 'SQL_ENGINE', SUSER_SNAME(),
               N'Cập nhật trạng thái phòng ' + i.RoomNumber + N' → ' + i.Status,
               ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0)
        FROM inserted i;
    END
END;
GO

-- 9.3. Trigger trên bảng Bookings
CREATE OR ALTER TRIGGER dbo.trg_Sync_Bookings
ON dbo.Bookings
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    DECLARE @Action VARCHAR(20) = CASE
        WHEN EXISTS(SELECT 1 FROM inserted) AND EXISTS(SELECT 1 FROM deleted) THEN 'UPDATE'
        WHEN EXISTS(SELECT 1 FROM inserted) THEN 'INSERT'
        ELSE 'DELETE'
    END;

    IF EXISTS(SELECT 1 FROM inserted)
    BEGIN
        UPDATE b
        SET b.UpdatedAt = SYSUTCDATETIME(),
            b.SyncVersion = b.SyncVersion + 1
        FROM dbo.Bookings b
        INNER JOIN inserted i ON b.Id = i.Id;

        INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
        SELECT 'bookings', i.BookingCode, @Action, 'SQL_ENGINE', SUSER_SNAME(),
               N'Đơn đặt phòng ' + i.BookingCode + N' (' + ISNULL(i.GuestName, N'Khách') + N') → ' + i.Status,
               ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0)
        FROM inserted i;
    END
END;
GO

-- 9.4. Trigger trên bảng RoomDeliveries
CREATE OR ALTER TRIGGER dbo.trg_Sync_RoomDeliveries
ON dbo.RoomDeliveries
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    IF EXISTS(SELECT 1 FROM inserted)
    BEGIN
        UPDATE d
        SET d.UpdatedAt = SYSUTCDATETIME(),
            d.SyncVersion = d.SyncVersion + 1
        FROM dbo.RoomDeliveries d
        INNER JOIN inserted i ON d.Id = i.Id;

        INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
        SELECT 'deliveries', i.OrderCode, 'UPDATE', 'SQL_ENGINE', SUSER_SNAME(),
               N'Đơn giao tận phòng ' + i.OrderCode + N' (' + i.RoomNumber + N') → ' + i.Status,
               ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0)
        FROM inserted i;
    END
END;
GO

-- 9.5. Trigger trên bảng LocalPartners
CREATE OR ALTER TRIGGER dbo.trg_Sync_LocalPartners
ON dbo.LocalPartners
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    IF EXISTS(SELECT 1 FROM inserted)
    BEGIN
        UPDATE lp
        SET lp.UpdatedAt = SYSUTCDATETIME(),
            lp.SyncVersion = lp.SyncVersion + 1
        FROM dbo.LocalPartners lp
        INNER JOIN inserted i ON lp.PartnerId = i.PartnerId;

        INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
        SELECT 'partners', i.PartnerId, 'UPDATE', 'SQL_ENGINE', SUSER_SNAME(),
               N'Đồng bộ đối tác bản đồ ' + i.PartnerId + N' (' + i.Name + N')',
               ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0)
        FROM inserted i;
    END
END;
GO

-- 9.6. Trigger trên bảng Employees & Payrolls & FinancialTransactions
CREATE OR ALTER TRIGGER dbo.trg_Sync_Employees
ON dbo.Employees
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL() > 1 RETURN;
    IF EXISTS(SELECT 1 FROM inserted)
    BEGIN
        UPDATE e SET e.UpdatedAt = SYSUTCDATETIME(), e.SyncVersion = e.SyncVersion + 1
        FROM dbo.Employees e INNER JOIN inserted i ON e.Id = i.Id;

        INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
        SELECT 'employees', i.EmployeeCode, 'UPDATE', 'SQL_ENGINE', SUSER_SNAME(),
               N'Đồng bộ hồ sơ nhân sự ' + i.EmployeeCode + N' (' + i.FullName + N')',
               ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0)
        FROM inserted i;
    END
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_Sync_Payrolls
ON dbo.Payrolls
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL() > 1 RETURN;
    IF EXISTS(SELECT 1 FROM inserted)
    BEGIN
        UPDATE p SET p.UpdatedAt = SYSUTCDATETIME(), p.SyncVersion = p.SyncVersion + 1
        FROM dbo.Payrolls p INNER JOIN inserted i ON p.Id = i.Id;

        INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
        SELECT 'payrolls', i.PayrollCode, 'UPDATE', 'SQL_ENGINE', SUSER_SNAME(),
               N'Đồng bộ bảng lương ' + i.PayrollCode + N' → ' + i.Status,
               ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0)
        FROM inserted i;
    END
END;
GO

CREATE OR ALTER TRIGGER dbo.trg_Sync_FinancialTransactions
ON dbo.FinancialTransactions
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL() > 1 RETURN;
    IF EXISTS(SELECT 1 FROM inserted)
    BEGIN
        UPDATE f SET f.UpdatedAt = SYSUTCDATETIME(), f.SyncVersion = f.SyncVersion + 1
        FROM dbo.FinancialTransactions f INNER JOIN inserted i ON f.Id = i.Id;

        INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
        SELECT 'finance', i.TransactionCode, 'INSERT', 'SQL_ENGINE', SUSER_SNAME(),
               N'Giao dịch tài chính ' + i.TransactionCode + N' (' + i.Type + N'): ' + CAST(CAST(i.Amount AS INT) AS NVARCHAR(30)) + N'đ',
               ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0)
        FROM inserted i;
    END
END;
GO

PRINT N'Hoàn tất khởi tạo Hệ thống Đồng bộ SQL Hai Chiều (Two-Way SQL Sync + Change Tracking + Triggers) cho Kim Long EcoStay!';
GO

-- ==========================================================================================
-- 10. BẢNG DỊCH VỤ TRÀ CUNG ĐÌNH (dbo.TeaServices), ĐẶT BÀN TRÀ (dbo.TeaBookings) & EMAIL LOGS
-- ==========================================================================================
IF OBJECT_ID(N'dbo.TeaServices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeaServices (
        Id INT IDENTITY(1,1) CONSTRAINT PK_TeaServices PRIMARY KEY,
        ServiceCode VARCHAR(50) NOT NULL CONSTRAINT UQ_TeaServices_Code UNIQUE,
        Name NVARCHAR(200) NOT NULL,
        PackageType NVARCHAR(50) NOT NULL DEFAULT (N'Cá nhân'),
        Price DECIMAL(18,2) NOT NULL CONSTRAINT CK_TeaServices_Price CHECK (Price >= 0),
        PriceUnit NVARCHAR(50) NOT NULL DEFAULT (N'người'),
        DurationMinutes INT NOT NULL DEFAULT (60),
        MaxGuests INT NOT NULL DEFAULT (8),
        AvailableSlots INT NOT NULL DEFAULT (6),
        ServingSchedule NVARCHAR(200) NOT NULL DEFAULT (N'08:30 - 21:00'),
        ImageUrl NVARCHAR(500) NULL,
        Description NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL DEFAULT (1),
        UpdatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_TeaServices_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        SyncVersion INT NOT NULL CONSTRAINT DF_TeaServices_SyncVersion DEFAULT (1)
    );

    INSERT INTO dbo.TeaServices (ServiceCode, Name, PackageType, Price, PriceUnit, DurationMinutes, MaxGuests, AvailableSlots, ServingSchedule, ImageUrl, Description, IsActive)
    VALUES
    ('TEA-HOMESTAY', N'Thưởng Trà Cung Đình Tại Homestay', N'Cá nhân', 85000, N'người', 60, 8, 6, N'08:30 - 21:00 (Mỗi tiếng 1 lượt)', 'images/tra_cung_dinh.jpg', N'Thưởng thức ấm trà sen Cung Đình 16 vị thảo mộc tại hiên nhà rường lộng gió sông Hương.', 1),
    ('TEA-CAKES', N'Trải Nghiệm Thưởng Trà Kết Hợp Bánh Huế', N'Cá nhân', 135000, N'người', 75, 10, 8, N'09:00, 15:30, 19:30', 'images/mon_banhbeonamloc.jpg', N'Bộ ấm trà sen thượng hạng kết hợp mẹt 4 loại bánh Huế cổ truyền và kẹo cau, mè xửng Kim Long.', 1),
    ('TEA-COUPLE', N'Gói Thưởng Trà Dành Cho Cặp Đôi & Nhóm Khách', N'Gói 2-4 khách', 250000, N'gói 2-4 khách', 90, 4, 2, N'16:00 - 18:00 (Hoàng hôn) hoặc 20:00 (Trăng thanh)', 'images/qua_trasen.jpg', N'Bàn trà riêng tư trang trí nến thơm và hoa sen bên hồ cá koi nhà rường, kèm mứt gừng và âm nhạc nhã nhạc Huế.', 1),
    ('TEA-CULTURE', N'Gói Trải Nghiệm Văn Hóa Huế Kết Hợp Giới Thiệu Trà', N'Gói trải nghiệm', 390000, N'gói trải nghiệm', 120, 12, 4, N'15:00 - 17:00 Hàng ngày', 'images/bo_amtra_quatang.jpg', N'Nghệ nhân trà Huế trực tiếp hướng dẫn nghi thức pha trà Cung Đình, giải thích ý nghĩa 16 vị thảo mộc và thưởng trà đàm đạo.', 1);
END
GO

IF OBJECT_ID(N'dbo.EmailLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmailLogs (
        Id INT IDENTITY(1,1) CONSTRAINT PK_EmailLogs PRIMARY KEY,
        EmailId VARCHAR(50) NOT NULL CONSTRAINT UQ_EmailLogs_EmailId UNIQUE,
        TemplateType VARCHAR(50) NOT NULL,
        RecipientEmail VARCHAR(150) NOT NULL,
        Subject NVARCHAR(300) NOT NULL,
        Status VARCHAR(30) NOT NULL DEFAULT ('SUCCESS'),
        StatusMessage NVARCHAR(500) NULL,
        AssociatedBookingCode VARCHAR(50) NULL,
        SentAt DATETIME2(3) NOT NULL CONSTRAINT DF_EmailLogs_SentAt DEFAULT (SYSUTCDATETIME()),
        RetryCount INT NOT NULL DEFAULT (0),
        ErrorDetails NVARCHAR(MAX) NULL
    );

    INSERT INTO dbo.EmailLogs (EmailId, TemplateType, RecipientEmail, Subject, Status, StatusMessage, AssociatedBookingCode)
    VALUES
    ('EM-20261018-01', 'BOOKING_REQUEST_RECEIVED', 'khachhang@example.com', N'[Kim Long EcoStay] Tiếp nhận yêu cầu đặt phòng #KLE-20261018-01', 'SUCCESS', N'Đã gửi hướng dẫn chuyển khoản VietQR cọc 50%', 'KLE-20261018-01'),
    ('EM-20261015-88', 'DEPOSIT_CONFIRMED', 'tuan.hoang@example.com', N'[Kim Long EcoStay] Xác nhận đặt cọc 50% & Cấp mã khóa SmartLock #KLE-20261015-88', 'SUCCESS', N'Đã gửi mã khóa SmartLock bảo mật tới email khách hàng', 'KLE-20261015-88'),
    ('EM-20261014-42', 'REMAINING_BALANCE_REQUEST', 'nga.dang@example.com', N'[Kim Long EcoStay] Chào mừng Check-in & Thông báo số dư 50% còn lại #KLE-20261014-42', 'SUCCESS', N'Kích hoạt tự động khi khách Check-in phòng KLE-R201', 'KLE-20261014-42'),
    ('EM-20261010-19', 'FULL_PAYMENT_CONFIRMED', 'long.le@example.com', N'[Kim Long EcoStay] Xác nhận tất toán 100% & Cảm ơn quý khách #KLE-20261010-19', 'SUCCESS', N'Hoàn tất lưu trú và gửi biên lai điện tử', 'KLE-20261010-19');
END
GO

-- ==========================================================================================
-- 11. STORED PROCEDURES GIAO DỊCH SQL TRANSACTION & OPTIMISTIC CONCURRENCY
-- ==========================================================================================

-- 11.1. Thêm mới hoặc Cập nhật Sản Phẩm Đặc Sản (Có kiểm tra xung đột phiên bản SyncVersion)
CREATE OR ALTER PROCEDURE dbo.sp_UpsertSpecialtyProduct
    @ProductCode VARCHAR(50),
    @Name NVARCHAR(200),
    @Category NVARCHAR(100),
    @Price DECIMAL(18,2),
    @StockQuantity INT,
    @Unit NVARCHAR(50),
    @ImageLink NVARCHAR(500),
    @Origin NVARCHAR(250),
    @PartnerId VARCHAR(50) = NULL,
    @PartnerName NVARCHAR(200) = NULL,
    @Description NVARCHAR(MAX) = NULL,
    @IsActive BIT = 1,
    @ExpectedSyncVersion INT = NULL,
    @ChangedBy NVARCHAR(100) = N'Admin',
    @NewSyncVersion INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Price < 0
        THROW 50001, N'Giá bán sản phẩm không được nhỏ hơn 0 VNĐ.', 1;
    IF @StockQuantity < 0
        THROW 50002, N'Số lượng tồn kho không được nhỏ hơn 0.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @CurrentVersion INT;
        SELECT @CurrentVersion = SyncVersion FROM dbo.SpecialtyProducts WITH (UPDLOCK) WHERE ProductCode = @ProductCode;

        IF @CurrentVersion IS NOT NULL
        BEGIN
            IF @ExpectedSyncVersion IS NOT NULL AND @ExpectedSyncVersion > 0 AND @ExpectedSyncVersion <> @CurrentVersion
            BEGIN
                THROW 50009, N'CONCURRENCY_CONFLICT: Sản phẩm này vừa được cập nhật bởi một người dùng khác hoặc từ SQL Server. Vui lòng tải lại dữ liệu mới nhất trước khi lưu.', 1;
            END

            SET @NewSyncVersion = @CurrentVersion + 1;

            UPDATE dbo.SpecialtyProducts
            SET Name = @Name,
                Category = @Category,
                Price = @Price,
                StockQuantity = @StockQuantity,
                Unit = @Unit,
                ImageLink = @ImageLink,
                Origin = @Origin,
                PartnerId = @PartnerId,
                PartnerName = @PartnerName,
                Description = @Description,
                IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME(),
                SyncVersion = @NewSyncVersion
            WHERE ProductCode = @ProductCode;
        END
        ELSE
        BEGIN
            SET @NewSyncVersion = 1;
            INSERT INTO dbo.SpecialtyProducts (
                ProductCode, Name, Category, Price, StockQuantity, Unit, ImageLink,
                Origin, PartnerId, PartnerName, Description, IsActive, IsFeatured, IsSample,
                CreatedAt, UpdatedAt, SyncVersion
            )
            VALUES (
                @ProductCode, @Name, @Category, @Price, @StockQuantity, @Unit, @ImageLink,
                @Origin, @PartnerId, @PartnerName, @Description, @IsActive, 0, 0,
                SYSUTCDATETIME(), SYSUTCDATETIME(), 1
            );
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 11.2. Cấp lại mã khóa SmartLock trong 1 SQL Transaction (Cập nhật Hash + Ghi Audit Log)
CREATE OR ALTER PROCEDURE dbo.sp_ReissueSmartLockPinWithAudit
    @RoomNumber VARCHAR(20),
    @BookingCode VARCHAR(50) = NULL,
    @NewPinHash VARCHAR(128),
    @PerformedBy NVARCHAR(100),
    @RoleName VARCHAR(50),
    @Reason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF LEN(ISNULL(TRIM(@Reason), '')) < 3
        THROW 50003, N'Bắt buộc nhập lý do cấp lại mã khóa SmartLock để lưu nhật ký bảo mật.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.SmartLockPins
        SET PinHash = @NewPinHash,
            MaskedDisplay = '••••••',
            PinStatus = 'ACTIVE',
            LastReissuedBy = @PerformedBy,
            LastReissuedReason = @Reason,
            UpdatedAt = SYSUTCDATETIME(),
            SyncVersion = SyncVersion + 1
        WHERE RoomNumber = @RoomNumber;

        DECLARE @LogCode VARCHAR(50) = 'SL-' + FORMAT(SYSUTCDATETIME(), 'yyyyMMddHHmmss');

        INSERT INTO dbo.SmartLockAuditLogs (LogCode, RoomNumber, BookingCode, ActionType, PerformedBy, RoleName, Reason, CreatedAt)
        VALUES (@LogCode, @RoomNumber, @BookingCode, 'REISSUE_PIN', @PerformedBy, @RoleName, @Reason, SYSUTCDATETIME());

        INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
        VALUES ('smartlock', @RoomNumber, 'REISSUE_PIN', 'WEB_API', @PerformedBy,
                N'Cấp lại mã PIN bảo mật cho phòng ' + @RoomNumber + N' (Lý do: ' + @Reason + N')',
                ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0));

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
