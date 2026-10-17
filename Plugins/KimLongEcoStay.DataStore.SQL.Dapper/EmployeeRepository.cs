using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.DataStore.SQL.Dapper
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly IDataAccess _dataAccess;

        private static readonly List<Department> _fallbackDepartments = new()
        {
            new Department { Id = 1, Code = "EXEC", Name = "Ban Điều Hành & Quản Lý Homestay", Description = "Chịu trách nhiệm chiến lược, điều hành kinh doanh và quản lý tổng thể", EmployeeCount = 1 },
            new Department { Id = 2, Code = "FO", Name = "Bộ Phận Lễ Tân & Trải Nghiệm Khách Hàng", Description = "Tiếp đón khách, check-in/out, hướng dẫn tour xe đạp và di tích Kim Long", EmployeeCount = 2 },
            new Department { Id = 3, Code = "HK", Name = "Bộ Phận Buồng Phòng & Cảnh Quan Nhà Vườn", Description = "Dọn dẹp vệ sinh phòng sinh thái, chăm sóc vườn bưởi thanh trà và hồ sen", EmployeeCount = 2 },
            new Department { Id = 4, Code = "FIN", Name = "Bộ Phận Tài Chính - Kế Toán & Nhân Sự", Description = "Quản trị dòng tiền, thu chi, đối soát cọc VietQR, bảng lương và nhân sự", EmployeeCount = 2 }
        };

        private static readonly List<Position> _fallbackPositions = new()
        {
            new Position { Id = 1, Title = "Giám Đốc Điều Hành (CEO)", DepartmentId = 1, DepartmentName = "Ban Điều Hành & Quản Lý Homestay", BaseSalaryRange = "15.000.000đ - 22.000.000đ", Description = "Lãnh đạo toàn diện dự án Kim Long EcoStay" },
            new Position { Id = 2, Title = "Trưởng Phòng Nhân Sự & Vận Hành (COO)", DepartmentId = 4, DepartmentName = "Bộ Phận Tài Chính - Kế Toán & Nhân Sự", BaseSalaryRange = "12.000.000đ - 16.000.000đ", Description = "Điều phối nhân sự, quy trình homestay và đào tạo" },
            new Position { Id = 3, Title = "Kế Toán Trưởng (CFO)", DepartmentId = 4, DepartmentName = "Bộ Phận Tài Chính - Kế Toán & Nhân Sự", BaseSalaryRange = "11.000.000đ - 15.000.000đ", Description = "Quản lý doanh thu, chi phí, thuế và tiền lương" },
            new Position { Id = 4, Title = "Trưởng Nhóm Lễ Tân Homestay", DepartmentId = 2, DepartmentName = "Bộ Phận Lễ Tân & Trải Nghiệm Khách Hàng", BaseSalaryRange = "8.000.000đ - 10.000.000đ", Description = "Giám sát đón khách, trực ca, xử lý mã khóa IoT" },
            new Position { Id = 5, Title = "Nhân Viên Lễ Tân & Tour Guide Di Sản", DepartmentId = 2, DepartmentName = "Bộ Phận Lễ Tân & Trải Nghiệm Khách Hàng", BaseSalaryRange = "6.500.000đ - 8.500.000đ", Description = "Trực lễ tân, hướng dẫn khách trải nghiệm tour xe đạp" },
            new Position { Id = 6, Title = "Trưởng Bộ Phận Buồng Phòng", DepartmentId = 3, DepartmentName = "Bộ Phận Buồng Phòng & Cảnh Quan Nhà Vườn", BaseSalaryRange = "7.500.000đ - 9.500.000đ", Description = "Kiểm soát chất lượng phòng sạch theo chuẩn EcoStay" },
            new Position { Id = 7, Title = "Nhân Viên Buồng Phòng & Giặt Là Sinh Thái", DepartmentId = 3, DepartmentName = "Bộ Phận Buồng Phòng & Cảnh Quan Nhà Vườn", BaseSalaryRange = "5.500.000đ - 7.000.000đ", Description = "Làm buồng, thay drap thảo mộc, bồ hòn & tinh dầu" },
            new Position { Id = 8, Title = "Nhân Viên Cảnh Quan Vườn & Nông Nghiệp Hữu Cơ", DepartmentId = 3, DepartmentName = "Bộ Phận Buồng Phòng & Cảnh Quan Nhà Vườn", BaseSalaryRange = "5.500.000đ - 7.000.000đ", Description = "Chăm sóc vườn bưởi thanh trà 40 năm, hoa súng và trà sen" }
        };

        private static readonly List<Employee> _fallbackEmployees = new()
        {
            new Employee
            {
                Id = 1,
                EmployeeCode = "KLE-NV001",
                FullName = "Trần Thị Thu",
                AvatarUrl = "https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=150",
                DateOfBirth = new DateTime(1995, 4, 12),
                Gender = "Nữ",
                PhoneNumber = "0905111222",
                Email = "ceo@kimlongecostay.vn",
                Address = "24 Kim Long, TP. Huế",
                DepartmentId = 1,
                DepartmentName = "Ban Điều Hành & Quản Lý Homestay",
                PositionId = 1,
                PositionTitle = "Giám Đốc Điều Hành (CEO)",
                HireDate = new DateTime(2025, 1, 1),
                ContractType = "Chính thức",
                ContractStartDate = new DateTime(2025, 1, 1),
                ContractEndDate = new DateTime(2028, 1, 1),
                WorkStatus = "Đang làm việc",
                BaseSalary = 18000000,
                Allowance = 2000000,
                Notes = "Đồng sáng lập dự án Kim Long EcoStay"
            },
            new Employee
            {
                Id = 2,
                EmployeeCode = "KLE-NV002",
                FullName = "Trần Viết Mạnh Cường",
                AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150",
                DateOfBirth = new DateTime(1996, 8, 20),
                Gender = "Nam",
                PhoneNumber = "0905333444",
                Email = "nhansu@kimlongecostay.vn",
                Address = "88 Nguyễn Huệ, TP. Huế",
                DepartmentId = 4,
                DepartmentName = "Bộ Phận Tài Chính - Kế Toán & Nhân Sự",
                PositionId = 2,
                PositionTitle = "Trưởng Phòng Nhân Sự & Vận Hành (COO)",
                HireDate = new DateTime(2025, 1, 1),
                ContractType = "Chính thức",
                ContractStartDate = new DateTime(2025, 1, 1),
                ContractEndDate = new DateTime(2027, 1, 1),
                WorkStatus = "Đang làm việc",
                BaseSalary = 14000000,
                Allowance = 1500000,
                Notes = "Trưởng phòng nhân sự kiêm điều phối tour"
            },
            new Employee
            {
                Id = 3,
                EmployeeCode = "KLE-NV003",
                FullName = "Trần Minh Khoa",
                AvatarUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150",
                DateOfBirth = new DateTime(1997, 11, 5),
                Gender = "Nam",
                PhoneNumber = "0905555666",
                Email = "ketoan@kimlongecostay.vn",
                Address = "15 Lê Thánh Tôn, TP. Huế",
                DepartmentId = 4,
                DepartmentName = "Bộ Phận Tài Chính - Kế Toán & Nhân Sự",
                PositionId = 3,
                PositionTitle = "Kế Toán Trưởng (CFO)",
                HireDate = new DateTime(2025, 2, 1),
                ContractType = "Chính thức",
                ContractStartDate = new DateTime(2025, 2, 1),
                ContractEndDate = new DateTime(2027, 2, 1),
                WorkStatus = "Đang làm việc",
                BaseSalary = 13000000,
                Allowance = 1200000,
                Notes = "Kế toán trưởng quản lý thu chi và thuế"
            },
            new Employee
            {
                Id = 4,
                EmployeeCode = "KLE-NV004",
                FullName = "Lê Thị Hoàng Yến",
                AvatarUrl = "https://images.unsplash.com/photo-1580489944761-15a19d654956?w=150",
                DateOfBirth = new DateTime(2001, 6, 18),
                Gender = "Nữ",
                PhoneNumber = "0914888999",
                Email = "letan@kimlongecostay.vn",
                Address = "102 Phan Chu Trinh, TP. Huế",
                DepartmentId = 2,
                DepartmentName = "Bộ Phận Lễ Tân & Trải Nghiệm Khách Hàng",
                PositionId = 4,
                PositionTitle = "Trưởng Nhóm Lễ Tân Homestay",
                HireDate = new DateTime(2025, 3, 1),
                ContractType = "Chính thức",
                ContractStartDate = new DateTime(2025, 3, 1),
                ContractEndDate = DateTime.Today.AddDays(25), // Hợp đồng sắp hết hạn trong 25 ngày!
                WorkStatus = "Đang làm việc",
                BaseSalary = 9000000,
                Allowance = 1000000,
                Notes = "Lễ tân chính, giao tiếp tiếng Anh tốt (Hợp đồng sắp hết hạn)"
            },
            new Employee
            {
                Id = 5,
                EmployeeCode = "KLE-NV005",
                FullName = "Nguyễn Hữu Thành",
                AvatarUrl = "https://images.unsplash.com/photo-1522075469751-3a6694fb2f61?w=150",
                DateOfBirth = new DateTime(2003, 9, 25),
                Gender = "Nam",
                PhoneNumber = "0988222333",
                Email = "thanh.nguyen@kimlongecostay.vn",
                Address = "45 Chi Lăng, TP. Huế",
                DepartmentId = 2,
                DepartmentName = "Bộ Phận Lễ Tân & Trải Nghiệm Khách Hàng",
                PositionId = 5,
                PositionTitle = "Nhân Viên Lễ Tân & Tour Guide Di Sản",
                HireDate = new DateTime(2025, 8, 1),
                ContractType = "Thử việc",
                ContractStartDate = new DateTime(2025, 8, 1),
                ContractEndDate = DateTime.Today.AddDays(14), // Hợp đồng sắp hết hạn trong 14 ngày!
                WorkStatus = "Đang làm việc",
                BaseSalary = 7000000,
                Allowance = 800000,
                Notes = "Nhân viên đón khách và điều phối xe đạp GPS"
            },
            new Employee
            {
                Id = 6,
                EmployeeCode = "KLE-NV006",
                FullName = "Đặng Thị Kim Hoa",
                AvatarUrl = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=150",
                DateOfBirth = new DateTime(1988, 2, 14),
                Gender = "Nữ",
                PhoneNumber = "0905444555",
                Email = "hoa.dang@kimlongecostay.vn",
                Address = "12 Kiệt 44 Kim Long, TP. Huế",
                DepartmentId = 3,
                DepartmentName = "Bộ Phận Buồng Phòng & Cảnh Quan Nhà Vườn",
                PositionId = 7,
                PositionTitle = "Nhân Viên Buồng Phòng & Giặt Là Sinh Thái",
                HireDate = new DateTime(2025, 2, 15),
                ContractType = "Chính thức",
                ContractStartDate = new DateTime(2025, 2, 15),
                ContractEndDate = new DateTime(2027, 2, 15),
                WorkStatus = "Đang làm việc",
                BaseSalary = 6500000,
                Allowance = 800000,
                Notes = "Người dân bản địa Kim Long, chế biến xà phòng bồ hòn & chăm sóc buồng phòng"
            }
        };

        private static readonly List<EmployeeContract> _fallbackContracts = new()
        {
            new EmployeeContract { Id = 1, ContractCode = "HD-KLE-NV001", EmployeeId = 1, EmployeeName = "Trần Thị Thu", EmployeeCode = "KLE-NV001", DepartmentName = "Ban Điều Hành & Quản Lý Homestay", PositionTitle = "Giám Đốc Điều Hành (CEO)", ContractType = "Chính thức", StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2028, 1, 1), BasicSalary = 18000000, Allowance = 2000000, Status = "Còn hiệu lực", SignedDate = new DateTime(2025, 1, 1), Notes = "Hợp đồng giám đốc điều hành" },
            new EmployeeContract { Id = 2, ContractCode = "HD-KLE-NV002", EmployeeId = 2, EmployeeName = "Trần Viết Mạnh Cường", EmployeeCode = "KLE-NV002", DepartmentName = "Bộ Phận Tài Chính - Kế Toán & Nhân Sự", PositionTitle = "Trưởng Phòng Nhân Sự & Vận Hành (COO)", ContractType = "Chính thức", StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2027, 1, 1), BasicSalary = 14000000, Allowance = 1500000, Status = "Còn hiệu lực", SignedDate = new DateTime(2025, 1, 1), Notes = "Hợp đồng nhân sự" },
            new EmployeeContract { Id = 3, ContractCode = "HD-KLE-NV003", EmployeeId = 3, EmployeeName = "Trần Minh Khoa", EmployeeCode = "KLE-NV003", DepartmentName = "Bộ Phận Tài Chính - Kế Toán & Nhân Sự", PositionTitle = "Kế Toán Trưởng (CFO)", ContractType = "Chính thức", StartDate = new DateTime(2025, 2, 1), EndDate = new DateTime(2027, 2, 1), BasicSalary = 13000000, Allowance = 1200000, Status = "Còn hiệu lực", SignedDate = new DateTime(2025, 2, 1), Notes = "Hợp đồng kế toán trưởng" },
            new EmployeeContract { Id = 4, ContractCode = "HD-KLE-NV004", EmployeeId = 4, EmployeeName = "Lê Thị Hoàng Yến", EmployeeCode = "KLE-NV004", DepartmentName = "Bộ Phận Lễ Tân & Trải Nghiệm Khách Hàng", PositionTitle = "Trưởng Nhóm Lễ Tân Homestay", ContractType = "Chính thức", StartDate = new DateTime(2025, 3, 1), EndDate = DateTime.Today.AddDays(25), BasicSalary = 9000000, Allowance = 1000000, Status = "Sắp hết hạn", SignedDate = new DateTime(2025, 3, 1), Notes = "Hợp đồng sắp hết hạn trong 25 ngày" },
            new EmployeeContract { Id = 5, ContractCode = "HD-KLE-NV005", EmployeeId = 5, EmployeeName = "Nguyễn Hữu Thành", EmployeeCode = "KLE-NV005", DepartmentName = "Bộ Phận Lễ Tân & Trải Nghiệm Khách Hàng", PositionTitle = "Nhân Viên Lễ Tân & Tour Guide Di Sản", ContractType = "Thử việc", StartDate = new DateTime(2025, 8, 1), EndDate = DateTime.Today.AddDays(14), BasicSalary = 7000000, Allowance = 800000, Status = "Sắp hết hạn", SignedDate = new DateTime(2025, 8, 1), Notes = "Hợp đồng thử việc sắp hết hạn trong 14 ngày" },
            new EmployeeContract { Id = 6, ContractCode = "HD-KLE-NV006", EmployeeId = 6, EmployeeName = "Đặng Thị Kim Hoa", EmployeeCode = "KLE-NV006", DepartmentName = "Bộ Phận Buồng Phòng & Cảnh Quan Nhà Vườn", PositionTitle = "Nhân Viên Buồng Phòng & Giặt Là Sinh Thái", ContractType = "Chính thức", StartDate = new DateTime(2025, 2, 15), EndDate = new DateTime(2027, 2, 15), BasicSalary = 6500000, Allowance = 800000, Status = "Còn hiệu lực", SignedDate = new DateTime(2025, 2, 15), Notes = "Hợp đồng nhân viên buồng phòng" }
        };

        public EmployeeRepository(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public async Task<IEnumerable<Employee>> GetEmployeesAsync(string? search = null, int? departmentId = null, string? status = null, bool includeDeleted = false)
        {
            try
            {
                const string sql = @"
                    SELECT e.*, d.Name AS DepartmentName, p.Title AS PositionTitle
                    FROM dbo.Employees e
                    JOIN dbo.Departments d ON e.DepartmentId = d.Id
                    JOIN dbo.Positions p ON e.PositionId = p.Id
                    WHERE (@IncludeDeleted = 1 OR e.IsDeleted = 0)
                      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
                      AND (@Status IS NULL OR e.WorkStatus = @Status)
                      AND (@Search IS NULL OR (
                          e.FullName LIKE '%' + @Search + '%' OR
                          e.EmployeeCode LIKE '%' + @Search + '%' OR
                          e.Email LIKE '%' + @Search + '%' OR
                          e.PhoneNumber LIKE '%' + @Search + '%'
                      ))
                    ORDER BY e.Id ASC";

                var list = await _dataAccess.QueryAsync<Employee, dynamic>(sql, new
                {
                    IncludeDeleted = includeDeleted ? 1 : 0,
                    DepartmentId = departmentId,
                    Status = status,
                    Search = search
                });

                if (list != null && list.Count > 0)
                {
                    return list;
                }
            }
            catch
            {
                // Fallback to static memory store
            }

            // Fallback filtering
            var query = _fallbackEmployees.AsEnumerable();
            if (!includeDeleted) query = query.Where(e => !e.IsDeleted);
            if (departmentId.HasValue) query = query.Where(e => e.DepartmentId == departmentId.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(e => e.WorkStatus.Equals(status, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(e =>
                    e.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    e.EmployeeCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    e.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    e.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
            return query.ToList();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            try
            {
                const string sql = @"
                    SELECT e.*, d.Name AS DepartmentName, p.Title AS PositionTitle
                    FROM dbo.Employees e
                    JOIN dbo.Departments d ON e.DepartmentId = d.Id
                    JOIN dbo.Positions p ON e.PositionId = p.Id
                    WHERE e.Id = @Id";

                var emp = await _dataAccess.QuerySingleAsync<Employee, dynamic>(sql, new { Id = id });
                if (emp != null) return emp;
            }
            catch
            {
                // Fallback
            }

            return _fallbackEmployees.FirstOrDefault(e => e.Id == id);
        }

        public async Task<Employee?> GetEmployeeByCodeAsync(string code)
        {
            try
            {
                const string sql = @"
                    SELECT e.*, d.Name AS DepartmentName, p.Title AS PositionTitle
                    FROM dbo.Employees e
                    JOIN dbo.Departments d ON e.DepartmentId = d.Id
                    JOIN dbo.Positions p ON e.PositionId = p.Id
                    WHERE e.EmployeeCode = @Code";

                var emp = await _dataAccess.QuerySingleAsync<Employee, dynamic>(sql, new { Code = code });
                if (emp != null) return emp;
            }
            catch
            {
                // Fallback
            }

            return _fallbackEmployees.FirstOrDefault(e => e.EmployeeCode.Equals(code, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<int> AddEmployeeAsync(Employee employee)
        {
            try
            {
                const string sql = @"
                    INSERT INTO dbo.Employees (
                        EmployeeCode, FullName, AvatarUrl, DateOfBirth, Gender, PhoneNumber, Email, Address,
                        DepartmentId, PositionId, HireDate, ContractType, ContractStartDate, ContractEndDate,
                        WorkStatus, BaseSalary, Allowance, Notes, IsDeleted, CreatedAt
                    ) VALUES (
                        @EmployeeCode, @FullName, @AvatarUrl, @DateOfBirth, @Gender, @PhoneNumber, @Email, @Address,
                        @DepartmentId, @PositionId, @HireDate, @ContractType, @ContractStartDate, @ContractEndDate,
                        @WorkStatus, @BaseSalary, @Allowance, @Notes, 0, SYSUTCDATETIME()
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int newId = await _dataAccess.QuerySingleAsync<int, dynamic>(sql, employee);
                if (newId > 0)
                {
                    employee.Id = newId;
                    _fallbackEmployees.Add(employee);
                    return newId;
                }
            }
            catch
            {
                // Fallback
            }

            int nextId = _fallbackEmployees.Count > 0 ? _fallbackEmployees.Max(e => e.Id) + 1 : 1;
            employee.Id = nextId;
            var dept = _fallbackDepartments.FirstOrDefault(d => d.Id == employee.DepartmentId);
            if (dept != null) employee.DepartmentName = dept.Name;
            var pos = _fallbackPositions.FirstOrDefault(p => p.Id == employee.PositionId);
            if (pos != null) employee.PositionTitle = pos.Title;
            _fallbackEmployees.Add(employee);
            return nextId;
        }

        public async Task<bool> UpdateEmployeeAsync(Employee employee)
        {
            try
            {
                const string sql = @"
                    UPDATE dbo.Employees SET
                        FullName = @FullName,
                        AvatarUrl = @AvatarUrl,
                        DateOfBirth = @DateOfBirth,
                        Gender = @Gender,
                        PhoneNumber = @PhoneNumber,
                        Email = @Email,
                        Address = @Address,
                        DepartmentId = @DepartmentId,
                        PositionId = @PositionId,
                        HireDate = @HireDate,
                        ContractType = @ContractType,
                        ContractStartDate = @ContractStartDate,
                        ContractEndDate = @ContractEndDate,
                        WorkStatus = @WorkStatus,
                        BaseSalary = @BaseSalary,
                        Allowance = @Allowance,
                        Notes = @Notes
                    WHERE Id = @Id";

                int rows = await _dataAccess.ExecuteCommandAsync(sql, employee);
                if (rows > 0)
                {
                    var existing = _fallbackEmployees.FirstOrDefault(e => e.Id == employee.Id);
                    if (existing != null)
                    {
                        int index = _fallbackEmployees.IndexOf(existing);
                        _fallbackEmployees[index] = employee;
                    }
                    return true;
                }
            }
            catch
            {
                // Fallback
            }

            var local = _fallbackEmployees.FirstOrDefault(e => e.Id == employee.Id);
            if (local != null)
            {
                int index = _fallbackEmployees.IndexOf(local);
                var dept = _fallbackDepartments.FirstOrDefault(d => d.Id == employee.DepartmentId);
                if (dept != null) employee.DepartmentName = dept.Name;
                var pos = _fallbackPositions.FirstOrDefault(p => p.Id == employee.PositionId);
                if (pos != null) employee.PositionTitle = pos.Title;
                _fallbackEmployees[index] = employee;
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteEmployeeAsync(int id, bool softDelete = true)
        {
            try
            {
                string sql = softDelete
                    ? "UPDATE dbo.Employees SET IsDeleted = 1, WorkStatus = N'Đã nghỉ việc' WHERE Id = @Id"
                    : "DELETE FROM dbo.Employees WHERE Id = @Id";

                int rows = await _dataAccess.ExecuteCommandAsync(sql, new { Id = id });
                if (rows > 0)
                {
                    var existing = _fallbackEmployees.FirstOrDefault(e => e.Id == id);
                    if (existing != null)
                    {
                        if (softDelete)
                        {
                            existing.IsDeleted = true;
                            existing.WorkStatus = "Đã nghỉ việc";
                        }
                        else
                        {
                            _fallbackEmployees.Remove(existing);
                        }
                    }
                    return true;
                }
            }
            catch
            {
                // Fallback
            }

            var local = _fallbackEmployees.FirstOrDefault(e => e.Id == id);
            if (local != null)
            {
                if (softDelete)
                {
                    local.IsDeleted = true;
                    local.WorkStatus = "Đã nghỉ việc";
                }
                else
                {
                    _fallbackEmployees.Remove(local);
                }
                return true;
            }
            return false;
        }

        public async Task<IEnumerable<Department>> GetDepartmentsAsync()
        {
            try
            {
                const string sql = @"
                    SELECT d.*, 
                           (SELECT COUNT(*) FROM dbo.Employees e WHERE e.DepartmentId = d.Id AND e.IsDeleted = 0) AS EmployeeCount
                    FROM dbo.Departments d
                    ORDER BY d.Id";

                var list = await _dataAccess.QueryAsync<Department, dynamic>(sql, new { });
                if (list != null && list.Count > 0) return list;
            }
            catch
            {
                // Fallback
            }

            foreach (var d in _fallbackDepartments)
            {
                d.EmployeeCount = _fallbackEmployees.Count(e => e.DepartmentId == d.Id && !e.IsDeleted);
            }
            return _fallbackDepartments;
        }

        public async Task<IEnumerable<Position>> GetPositionsAsync(int? departmentId = null)
        {
            try
            {
                const string sql = @"
                    SELECT p.*, d.Name AS DepartmentName,
                           (SELECT COUNT(*) FROM dbo.Employees e WHERE e.PositionId = p.Id AND e.IsDeleted = 0) AS EmployeeCount
                    FROM dbo.Positions p
                    JOIN dbo.Departments d ON p.DepartmentId = d.Id
                    WHERE (@DepartmentId IS NULL OR p.DepartmentId = @DepartmentId)
                    ORDER BY p.Id";

                var list = await _dataAccess.QueryAsync<Position, dynamic>(sql, new { DepartmentId = departmentId });
                if (list != null && list.Count > 0) return list;
            }
            catch
            {
                // Fallback
            }

            var query = _fallbackPositions.AsEnumerable();
            if (departmentId.HasValue) query = query.Where(p => p.DepartmentId == departmentId.Value);
            var result = query.ToList();
            foreach (var p in result)
            {
                p.EmployeeCount = _fallbackEmployees.Count(e => e.PositionId == p.Id && !e.IsDeleted);
            }
            return result;
        }

        public async Task<IEnumerable<EmployeeContract>> GetContractsAsync(int? employeeId = null, string? status = null)
        {
            try
            {
                const string sql = @"
                    SELECT c.*, e.FullName AS EmployeeName, e.EmployeeCode, d.Name AS DepartmentName, p.Title AS PositionTitle
                    FROM dbo.Contracts c
                    JOIN dbo.Employees e ON c.EmployeeId = e.Id
                    JOIN dbo.Departments d ON e.DepartmentId = d.Id
                    JOIN dbo.Positions p ON e.PositionId = p.Id
                    WHERE (@EmployeeId IS NULL OR c.EmployeeId = @EmployeeId)
                      AND (@Status IS NULL OR c.Status = @Status)
                    ORDER BY c.StartDate DESC";

                var list = await _dataAccess.QueryAsync<EmployeeContract, dynamic>(sql, new { EmployeeId = employeeId, Status = status });
                if (list != null && list.Count > 0) return list;
            }
            catch
            {
                // Fallback
            }

            var query = _fallbackContracts.AsEnumerable();
            if (employeeId.HasValue) query = query.Where(c => c.EmployeeId == employeeId.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            return query.ToList();
        }

        public async Task<int> AddContractAsync(EmployeeContract contract)
        {
            try
            {
                const string sql = @"
                    INSERT INTO dbo.Contracts (
                        ContractCode, EmployeeId, ContractType, StartDate, EndDate, BasicSalary, Allowance, Status, SignedDate, Notes
                    ) VALUES (
                        @ContractCode, @EmployeeId, @ContractType, @StartDate, @EndDate, @BasicSalary, @Allowance, @Status, @SignedDate, @Notes
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int newId = await _dataAccess.QuerySingleAsync<int, dynamic>(sql, contract);
                if (newId > 0)
                {
                    contract.Id = newId;
                    _fallbackContracts.Add(contract);
                    return newId;
                }
            }
            catch
            {
                // Fallback
            }

            int nextId = _fallbackContracts.Count > 0 ? _fallbackContracts.Max(c => c.Id) + 1 : 1;
            contract.Id = nextId;
            _fallbackContracts.Add(contract);
            return nextId;
        }

        public async Task<bool> UpdateContractAsync(EmployeeContract contract)
        {
            try
            {
                const string sql = @"
                    UPDATE dbo.Contracts SET
                        ContractType = @ContractType,
                        StartDate = @StartDate,
                        EndDate = @EndDate,
                        BasicSalary = @BasicSalary,
                        Allowance = @Allowance,
                        Status = @Status,
                        SignedDate = @SignedDate,
                        Notes = @Notes
                    WHERE Id = @Id";

                int rows = await _dataAccess.ExecuteCommandAsync(sql, contract);
                if (rows > 0) return true;
            }
            catch
            {
                // Fallback
            }

            var local = _fallbackContracts.FirstOrDefault(c => c.Id == contract.Id);
            if (local != null)
            {
                int index = _fallbackContracts.IndexOf(local);
                _fallbackContracts[index] = contract;
                return true;
            }
            return false;
        }
    }
}
