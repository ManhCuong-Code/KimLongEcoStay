USE KimLongEcoStayDB;
GO

-- 1. CẬP NHẬT TÀI KHOẢN HỆ THỐNG VỚI FONT TIẾNG VIỆT CHUẨN
UPDATE dbo.AppUsers SET FullName = N'Trần Minh Khoa (Kế Toán Trưởng - CFO)' WHERE Username = 'ketoan';
UPDATE dbo.AppUsers SET FullName = N'Trần Viết Mạnh Cường (Trưởng Phòng Nhân Sự - COO)' WHERE Username = 'nhansu';
UPDATE dbo.AppUsers SET FullName = N'Trần Thị Thu (Giám Đốc Quản Lý - CEO)' WHERE Username = 'quanly';
UPDATE dbo.AppUsers SET FullName = N'Lê Thị Hoàng Yến (Lễ Tân - Điều Hành)' WHERE Username = 'letan';
GO

-- 2. XÓA CÁC BẢNG THEO THỨ TỰ KHÓA NGOẠI ĐỂ NẠP LẠI DỮ LIỆU UNICODE CHUẨN UTF-8
DELETE FROM dbo.FinancialTransactions;
DELETE FROM dbo.Expenses;
DELETE FROM dbo.Payrolls;
DELETE FROM dbo.Contracts;
DELETE FROM dbo.Employees;
DELETE FROM dbo.Positions;
DELETE FROM dbo.Departments;
DELETE FROM dbo.ExpenseCategories;
GO

DBCC CHECKIDENT ('dbo.Departments', RESEED, 0);
DBCC CHECKIDENT ('dbo.Positions', RESEED, 0);
DBCC CHECKIDENT ('dbo.Employees', RESEED, 0);
DBCC CHECKIDENT ('dbo.Contracts', RESEED, 0);
DBCC CHECKIDENT ('dbo.Payrolls', RESEED, 0);
DBCC CHECKIDENT ('dbo.ExpenseCategories', RESEED, 0);
DBCC CHECKIDENT ('dbo.Expenses', RESEED, 0);
DBCC CHECKIDENT ('dbo.FinancialTransactions', RESEED, 0);
GO

-- 3. NẠP LẠI PHÒNG BAN
INSERT INTO dbo.Departments (Code, Name, Description) VALUES
('EXEC', N'Ban Điều Hành & Quản Trị Homestay', N'Chịu trách nhiệm chiến lược phát triển mô hình nhà vườn sinh thái Kim Long'),
('FO', N'Bộ Phận Lễ Tân & Trải Nghiệm Khách Hàng', N'Đón tiếp khách du lịch, check-in thông minh qua IoT và hướng dẫn tour di sản'),
('HK', N'Bộ Phận Buồng Phòng & Cảnh Quan Nhà Vườn', N'Dọn dẹp vệ sinh phòng sinh thái, chăm sóc vườn bưởi thanh trà và hồ sen'),
('FIN', N'Bộ Phận Tài Chính - Kế Toán & Nhân Sự', N'Quản trị dòng tiền, thu chi, đối soát cọc VietQR, bảng lương và nhân sự');
GO

-- 4. NẠP LẠI CHỨC VỤ
DECLARE @dExec INT = (SELECT Id FROM dbo.Departments WHERE Code = 'EXEC');
DECLARE @dFO INT = (SELECT Id FROM dbo.Departments WHERE Code = 'FO');
DECLARE @dHK INT = (SELECT Id FROM dbo.Departments WHERE Code = 'HK');
DECLARE @dFin INT = (SELECT Id FROM dbo.Departments WHERE Code = 'FIN');

INSERT INTO dbo.Positions (Title, DepartmentId, BaseSalaryRange, Description) VALUES
(N'Giám Đốc Điều Hành (CEO)', @dExec, N'15.000.000đ - 22.000.000đ', N'Lãnh đạo toàn diện dự án Kim Long EcoStay'),
(N'Trưởng Phòng Nhân Sự & Vận Hành (COO)', @dFin, N'12.000.000đ - 16.000.000đ', N'Điều phối nhân sự, quy trình homestay và đào tạo'),
(N'Kế Toán Trưởng (CFO)', @dFin, N'11.000.000đ - 15.000.000đ', N'Quản lý doanh thu, chi phí, thuế và tiền lương'),
(N'Trưởng Nhóm Lễ Tân Homestay', @dFO, N'8.000.000đ - 10.000.000đ', N'Giám sát đón khách, trực ca, xử lý mã khóa IoT'),
(N'Nhân Viên Lễ Tân & Tour Guide Di Sản', @dFO, N'6.500.000đ - 8.500.000đ', N'Trực lễ tân, hướng dẫn khách trải nghiệm tour xe đạp'),
(N'Trưởng Bộ Phận Buồng Phòng', @dHK, N'7.500.000đ - 9.500.000đ', N'Kiểm soát chất lượng phòng sạch theo chuẩn EcoStay'),
(N'Nhân Viên Buồng Phòng & Giặt Là Sinh Thái', @dHK, N'5.500.000đ - 7.000.000đ', N'Làm buồng, thay drap thảo mộc, bồ hòn & tinh dầu'),
(N'Nhân Viên Cảnh Quan Vườn & Nông Nghiệp Hữu Cơ', @dHK, N'5.500.000đ - 7.000.000đ', N'Chăm sóc vườn bưởi thanh trà 40 năm, hoa súng và trà sen');
GO

-- 5. NẠP LẠI HỒ SƠ NHÂN VIÊN
DECLARE @posCEO INT = (SELECT Id FROM dbo.Positions WHERE Title LIKE N'%Giám Đốc Điều Hành%');
DECLARE @posCOO INT = (SELECT Id FROM dbo.Positions WHERE Title LIKE N'%Trưởng Phòng Nhân Sự%');
DECLARE @posCFO INT = (SELECT Id FROM dbo.Positions WHERE Title LIKE N'%Kế Toán Trưởng%');
DECLARE @posFOLeader INT = (SELECT Id FROM dbo.Positions WHERE Title LIKE N'%Trưởng Nhóm Lễ Tân%');
DECLARE @posFOGuide INT = (SELECT Id FROM dbo.Positions WHERE Title LIKE N'%Nhân Viên Lễ Tân & Tour Guide%');
DECLARE @posHKStaff INT = (SELECT Id FROM dbo.Positions WHERE Title LIKE N'%Nhân Viên Buồng Phòng & Giặt Là%');
DECLARE @posGardenStaff INT = (SELECT Id FROM dbo.Positions WHERE Title LIKE N'%Nhân Viên Cảnh Quan Vườn%');

DECLARE @depExec INT = (SELECT Id FROM dbo.Departments WHERE Code = 'EXEC');
DECLARE @depFO INT = (SELECT Id FROM dbo.Departments WHERE Code = 'FO');
DECLARE @depHK INT = (SELECT Id FROM dbo.Departments WHERE Code = 'HK');
DECLARE @depFin INT = (SELECT Id FROM dbo.Departments WHERE Code = 'FIN');

INSERT INTO dbo.Employees 
(EmployeeCode, FullName, AvatarUrl, DateOfBirth, Gender, PhoneNumber, Email, Address, DepartmentId, PositionId, HireDate, ContractType, ContractStartDate, ContractEndDate, WorkStatus, BaseSalary, Allowance, Notes)
VALUES
('KLE-NV001', N'Trần Thị Thu', 'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=150', '1995-04-12', N'Nữ', '0905111222', 'ceo@kimlongecostay.vn', N'24 Kim Long, TP. Huế', @depExec, @posCEO, '2025-01-01', N'Chính thức', '2025-01-01', '2028-01-01', N'Đang làm việc', 18000000, 2000000, N'Đồng sáng lập dự án Kim Long EcoStay'),
('KLE-NV002', N'Trần Viết Mạnh Cường', 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150', '1996-08-20', N'Nam', '0905333444', 'nhansu@kimlongecostay.vn', N'88 Nguyễn Huệ, TP. Huế', @depFin, @posCOO, '2025-01-01', N'Chính thức', '2025-01-01', '2027-01-01', N'Đang làm việc', 14000000, 1500000, N'Trưởng phòng nhân sự kiêm điều phối tour'),
('KLE-NV003', N'Trần Minh Khoa', 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150', '1997-11-05', N'Nam', '0905555666', 'ketoan@kimlongecostay.vn', N'15 Lê Thánh Tôn, TP. Huế', @depFin, @posCFO, '2025-02-01', N'Chính thức', '2025-02-01', '2027-02-01', N'Đang làm việc', 13000000, 1200000, N'Kế toán trưởng quản lý thu chi và thuế'),
('KLE-NV004', N'Lê Thị Hoàng Yến', 'https://images.unsplash.com/photo-1580489944761-15a19d654956?w=150', '2001-06-18', N'Nữ', '0914888999', 'letan@kimlongecostay.vn', N'102 Phan Chu Trinh, TP. Huế', @depFO, @posFOLeader, '2025-03-01', N'Chính thức', '2025-03-01', '2026-11-30', N'Đang làm việc', 9000000, 1000000, N'Lễ tân chính, giao tiếp tiếng Anh tốt (Hợp đồng sắp hết hạn trong 45 ngày)'),
('KLE-NV005', N'Nguyễn Hữu Thành', 'https://images.unsplash.com/photo-1522075469751-3a6694fb2f61?w=150', '2003-09-25', N'Nam', '0988222333', 'thanh.nguyen@kimlongecostay.vn', N'45 Chi Lăng, TP. Huế', @depFO, @posFOGuide, '2025-08-01', N'Thử việc', '2025-08-01', '2026-10-31', N'Đang làm việc', 7000000, 800000, N'Nhân viên đón khách và điều phối xe đạp GPS (Hợp đồng thử việc sắp hết hạn trong 15 ngày)'),
('KLE-NV006', N'Đặng Thị Kim Hoa', 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150', '1988-02-14', N'Nữ', '0905444555', 'hoa.dang@kimlongecostay.vn', N'12 Kiệt 44 Kim Long, TP. Huế', @depHK, @posHKStaff, '2025-02-15', N'Chính thức', '2025-02-15', '2027-02-15', N'Đang làm việc', 6500000, 800000, N'Người dân bản địa Kim Long, am hiểu chế biến xà phòng bồ hòn & chăm sóc buồng phòng');
GO

-- 6. NẠP LẠI HỢP ĐỒNG LAO ĐỘNG
INSERT INTO dbo.Contracts (ContractCode, EmployeeId, ContractType, StartDate, EndDate, BasicSalary, Allowance, Status, SignedDate, Notes)
SELECT 
    'HD-' + e.EmployeeCode,
    e.Id,
    e.ContractType,
    e.ContractStartDate,
    e.ContractEndDate,
    e.BaseSalary,
    e.Allowance,
    CASE 
        WHEN e.ContractEndDate <= DATEADD(day, 30, GETDATE()) THEN N'Sắp hết hạn'
        ELSE N'Còn hiệu lực'
    END,
    e.ContractStartDate,
    N'Hợp đồng lao động tiêu chuẩn Kim Long EcoStay'
FROM dbo.Employees e;
GO

-- 7. NẠP LẠI DANH MỤC CHI PHÍ
INSERT INTO dbo.ExpenseCategories (Code, Name, ColorHex, IconClass, Description) VALUES
('LUONG', N'Tiền Lương & Phụ Cấp Nhân Viên', '#10B981', 'bi bi-people-fill', N'Chi trả lương cơ bản, phụ cấp trách nhiệm và thưởng KPI'),
('DIEN_NUOC', N'Điện, Nước & Viễn Thông Internet', '#3B82F6', 'bi bi-lightning-charge-fill', N'Hóa đơn điện lực Huế, nước sạch sông Hương và wifi mesh homestay'),
('VESINH', N'Vệ Sinh & Giặt Là Thảo Mộc', '#06B6D4', 'bi bi-droplet-half', N'Giặt sấy drap giường, tinh dầu thiên nhiên, xà phòng bồ hòn'),
('BAOTRI', N'Bảo Trì, Sửa Chữa & Thiết Bị IoT', '#F59E0B', 'bi bi-tools', N'Bảo trì khóa SmartLock, điều hòa inverter, máy nước nóng NLMT'),
('VATTU', N'Vật Tư, Nông Sản & Đồ Dùng Homestay', '#EC4899', 'bi bi-box-seam-fill', N'Bánh ướt thịt nướng đón khách, trà sen Cung Đình, bưởi thanh trà'),
('MARKETING', N'Marketing, Quảng Cáo & Sự Kiện', '#8B5CF6', 'bi bi-megaphone-fill', N'Chạy quảng cáo Facebook, TikTok, website và hợp tác travel blogger'),
('VANHANH_KHAC', N'Chi Phí Thuê Mặt Bằng & Vận Hành Khác', '#64748B', 'bi bi-gear-fill', N'Tiền thuê 02 khuôn viên nhà vườn Kim Long và chi phí hành chính');
GO

-- 8. NẠP LẠI BẢNG LƯƠNG
-- Tháng 9/2026 (Đã duyệt & Đã thanh toán)
INSERT INTO dbo.Payrolls 
(PayrollCode, EmployeeId, Month, Year, BaseSalary, Allowance, Bonus, OvertimePay, Deductions, AdvancePayment, GrossIncome, NetIncome, PaymentDate, PaymentMethod, Status, ApprovedBy, ApprovedAt, Notes)
SELECT 
    'PL-202609-' + e.EmployeeCode,
    e.Id,
    9,
    2026,
    e.BaseSalary,
    e.Allowance,
    1000000,
    0,
    500000,
    0,
    (e.BaseSalary + e.Allowance + 1000000),
    (e.BaseSalary + e.Allowance + 1000000 - 500000),
    '2026-10-05 10:00:00',
    N'Chuyển khoản VietQR',
    N'PAID',
    N'Trần Thị Thu (CEO)',
    '2026-10-04 15:00:00',
    N'Lương tháng 9/2026 thanh toán đúng hạn qua tài khoản ngân hàng'
FROM dbo.Employees e;

-- Tháng 10/2026 (Kỳ hiện tại)
INSERT INTO dbo.Payrolls 
(PayrollCode, EmployeeId, Month, Year, BaseSalary, Allowance, Bonus, OvertimePay, Deductions, AdvancePayment, GrossIncome, NetIncome, PaymentDate, PaymentMethod, Status, ApprovedBy, ApprovedAt, Notes)
SELECT 
    'PL-202610-' + e.EmployeeCode,
    e.Id,
    10,
    2026,
    e.BaseSalary,
    e.Allowance,
    500000,
    CASE WHEN e.DepartmentId = 2 THEN 350000 ELSE 0 END,
    500000,
    0,
    (e.BaseSalary + e.Allowance + 500000 + CASE WHEN e.DepartmentId = 2 THEN 350000 ELSE 0 END),
    (e.BaseSalary + e.Allowance + 500000 + CASE WHEN e.DepartmentId = 2 THEN 350000 ELSE 0 END - 500000),
    NULL,
    N'Chuyển khoản VietQR',
    N'APPROVED',
    N'Trần Thị Thu (CEO)',
    '2026-10-15 09:00:00',
    N'Đã lập bảng lương tháng 10/2026, đã được CEO phê duyệt chờ ngày thanh toán 05/11'
FROM dbo.Employees e;
GO

-- 9. NẠP LẠI PHIẾU CHI
DECLARE @catLuong INT = (SELECT Id FROM dbo.ExpenseCategories WHERE Code = 'LUONG');
DECLARE @catDien INT = (SELECT Id FROM dbo.ExpenseCategories WHERE Code = 'DIEN_NUOC');
DECLARE @catVeSinh INT = (SELECT Id FROM dbo.ExpenseCategories WHERE Code = 'VESINH');
DECLARE @catBaoTri INT = (SELECT Id FROM dbo.ExpenseCategories WHERE Code = 'BAOTRI');
DECLARE @catVatTu INT = (SELECT Id FROM dbo.ExpenseCategories WHERE Code = 'VATTU');
DECLARE @catMkt INT = (SELECT Id FROM dbo.ExpenseCategories WHERE Code = 'MARKETING');
DECLARE @catThue INT = (SELECT Id FROM dbo.ExpenseCategories WHERE Code = 'VANHANH_KHAC');

INSERT INTO dbo.Expenses 
(VoucherCode, ExpenseDate, CategoryId, Title, Amount, PayeeName, PaymentMethod, Status, CreatedBy, ApprovedBy, Notes)
VALUES
('PC-202610-001', '2026-10-05', @catLuong, N'Chi trả lương và phụ cấp toàn thể nhân sự tháng 09/2026', 70700000, N'Tập thể Cán bộ Nhân viên Kim Long EcoStay', N'Chuyển khoản VietQR', N'PAID', N'Trần Minh Khoa', N'Trần Thị Thu', N'Thanh toán qua danh sách VietQR theo bảng lương tháng 9 đã duyệt'),
('PC-202610-002', '2026-10-08', @catDien, N'Thanh toán tiền điện sinh hoạt 02 nhà vườn Kim Long tháng 9/2026', 3850000, N'Công ty Điện Lực Thừa Thiên Huế', N'Chuyển khoản', N'PAID', N'Trần Minh Khoa', N'Trần Thị Thu', N'Mã khách hàng PE02000123456 - Điện lực Nam Sông Hương'),
('PC-202610-003', '2026-10-09', @catDien, N'Thanh toán hóa đơn nước sạch và Internet cáp quang Viettel', 1250000, N'Viettel Huế & Cty Cấp Nước HueWACO', N'Chuyển khoản', N'PAID', N'Trần Minh Khoa', N'Trần Thị Thu', N'Gói mesh wifi 300Mbps cho khuôn viên vườn'),
('PC-202610-004', '2026-10-10', @catVeSinh, N'Nhập vật tư giặt là organic, tinh dầu sả chanh & thảo mộc xông phòng', 2400000, N'Xưởng Tinh Dầu Tràm & Thảo Mộc Kim Long', N'Tiền mặt', N'PAID', N'Đặng Thị Kim Hoa', N'Trần Viết Mạnh Cường', N'Cung cấp xà phòng bồ hòn hữu cơ cho 5 buồng phòng'),
('PC-202610-005', '2026-10-12', @catBaoTri, N'Bảo trì định kỳ hệ thống khóa thông minh SmartLock IoT và xe đạp Eco-Bike', 1600000, N'Cửa hàng Cơ điện & Khóa thông minh Huế', N'Chuyển khoản', N'PAID', N'Nguyễn Hữu Thành', N'Trần Viết Mạnh Cường', N'Thay pin dung lượng cao cho 5 bộ khóa cửa và tra dầu 6 xe đạp trợ lực'),
('PC-202610-006', '2026-10-14', @catVatTu, N'Mua trà sen Cung Đình, bưởi thanh trà và đặc sản đón tiếp khách lưu trú', 2150000, N'Nhà vườn cụ Tôn Thất Hảo (Kim Long)', N'Tiền mặt', N'PAID', N'Lê Thị Hoàng Yến', N'Trần Thị Thu', N'Bưởi thanh trà loại 1 thu hái tại chỗ và 30 gói trà sen Cung Đình'),
('PC-202610-007', '2026-10-15', @catMkt, N'Chi phí truyền thông quảng bá mùa du lịch Cố Đô và chạy chiến dịch fanpage', 4500000, N'Meta Platforms / Agency Digital Marketing Huế', N'Thẻ tín dụng', N'PAID', N'Trần Viết Mạnh Cường', N'Trần Thị Thu', N'Chiến dịch quảng bá "Nghỉ dưỡng di sản Kim Long - Đón bình minh sông Hương"'),
('PC-202610-008', '2026-10-16', @catThue, N'Thanh toán tiền thuê mặt bằng nhà vườn tháng 10/2026 (Căn 1 & Căn 2)', 15000000, N'Chủ hộ di sản Nguyễn Phước & Lê Văn (Kim Long)', N'Chuyển khoản', N'PAID', N'Trần Minh Khoa', N'Trần Thị Thu', N'Thanh toán theo hợp đồng liên kết win-win bảo tồn nhà vườn Huế');
GO

-- 10. NẠP LẠI SỔ GIAO DỊCH THU
INSERT INTO dbo.FinancialTransactions 
(TransactionCode, TransactionDate, Type, Category, SourceType, SourceRefId, Amount, PaymentMethod, Status, CreatedBy, Description)
VALUES
('TX-20261015-01', '2026-10-15 08:30:00', 'INCOME', N'Tiền đặt cọc phòng nghỉ', 'BOOKING', 'BK20261015-01', 765000, N'VietQR', 'COMPLETED', N'Hệ Thống Tự Động', N'Thanh toán cọc 50% đơn phòng #BK20261015-01 của khách Nguyễn Văn An'),
('TX-20261015-02', '2026-10-15 15:45:00', 'INCOME', N'Trải nghiệm Tour xe đạp GPS Audio Guide', 'TOUR_BIKE', 'TB-20261015', 300000, N'VietQR', 'COMPLETED', N'Lê Thị Hoàng Yến', N'Khách thuê 2 xe đạp trợ lực điện khám phá chùa Thiên Mụ & văn thánh'),
('TX-20261016-01', '2026-10-16 09:15:00', 'INCOME', N'Ẩm thực & Thưởng trà sen Cung Đình', 'CULINARY', 'CU-20261016', 450000, N'Tiền mặt', 'COMPLETED', N'Lê Thị Hoàng Yến', N'Set trà sen cổ truyền và bánh ướt thịt nướng Kim Long cho 3 khách'),
('TX-20261016-02', '2026-10-16 11:20:00', 'INCOME', N'Bán nông sản & đặc sản bưởi thanh trà', 'SOUVENIR', 'SO-20261016', 380000, N'VietQR', 'COMPLETED', N'Đặng Thị Kim Hoa', N'Khách mua 4 quả bưởi thanh trà vườn Kim Long làm quà Cố Đô'),
('TX-20261016-03', '2026-10-16 14:00:00', 'INCOME', N'Tiền thanh toán còn lại khi nhận phòng', 'BOOKING', 'BK20261015-01', 765000, N'VietQR', 'COMPLETED', N'Lê Thị Hoàng Yến', N'Thanh toán 50% tiền phòng còn lại khi khách check-in thành công');
GO

PRINT N'Hoàn tất cập nhật Unicode Tiếng Việt chuẩn UTF-8 cho CSDL Kim Long EcoStay!';
GO
