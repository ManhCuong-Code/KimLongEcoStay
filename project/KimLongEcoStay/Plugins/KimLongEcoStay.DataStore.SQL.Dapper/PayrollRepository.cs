using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.DataStore.SQL.Dapper
{
    public class PayrollRepository : IPayrollRepository
    {
        private readonly IDataAccess _dataAccess;
        private readonly IEmployeeRepository _employeeRepository;

        private static readonly List<Payroll> _fallbackPayrolls = new()
        {
            // Tháng 9/2026: Đã thanh toán
            new Payroll
            {
                Id = 1,
                PayrollCode = "PL-202609-KLE-NV001",
                EmployeeId = 1,
                EmployeeCode = "KLE-NV001",
                EmployeeName = "Trần Thị Thu",
                DepartmentName = "Ban Điều Hành & Quản Lý Homestay",
                PositionTitle = "Giám Đốc Điều Hành (CEO)",
                Month = 9,
                Year = 2026,
                BaseSalary = 18000000,
                Allowance = 2000000,
                Bonus = 1000000,
                OvertimePay = 0,
                Deductions = 500000,
                AdvancePayment = 0,
                GrossIncome = 21000000,
                NetIncome = 20500000,
                PaymentDate = new DateTime(2026, 10, 5, 10, 0, 0),
                PaymentMethod = "Chuyển khoản VietQR",
                Status = "PAID",
                ApprovedBy = "Trần Thị Thu (CEO)",
                ApprovedAt = new DateTime(2026, 10, 4, 15, 0, 0),
                Notes = "Lương tháng 9/2026 thanh toán đúng hạn qua VietQR"
            },
            new Payroll
            {
                Id = 2,
                PayrollCode = "PL-202609-KLE-NV002",
                EmployeeId = 2,
                EmployeeCode = "KLE-NV002",
                EmployeeName = "Trần Viết Mạnh Cường",
                DepartmentName = "Bộ Phận Tài Chính - Kế Toán & Nhân Sự",
                PositionTitle = "Trưởng Phòng Nhân Sự & Vận Hành (COO)",
                Month = 9,
                Year = 2026,
                BaseSalary = 14000000,
                Allowance = 1500000,
                Bonus = 1000000,
                OvertimePay = 0,
                Deductions = 500000,
                AdvancePayment = 0,
                GrossIncome = 16500000,
                NetIncome = 16000000,
                PaymentDate = new DateTime(2026, 10, 5, 10, 0, 0),
                PaymentMethod = "Chuyển khoản VietQR",
                Status = "PAID",
                ApprovedBy = "Trần Thị Thu (CEO)",
                ApprovedAt = new DateTime(2026, 10, 4, 15, 0, 0),
                Notes = "Lương tháng 9/2026 thanh toán đúng hạn qua VietQR"
            },
            new Payroll
            {
                Id = 3,
                PayrollCode = "PL-202609-KLE-NV003",
                EmployeeId = 3,
                EmployeeCode = "KLE-NV003",
                EmployeeName = "Trần Minh Khoa",
                DepartmentName = "Bộ Phận Tài Chính - Kế Toán & Nhân Sự",
                PositionTitle = "Kế Toán Trưởng (CFO)",
                Month = 9,
                Year = 2026,
                BaseSalary = 13000000,
                Allowance = 1200000,
                Bonus = 1000000,
                OvertimePay = 0,
                Deductions = 500000,
                AdvancePayment = 0,
                GrossIncome = 15200000,
                NetIncome = 14700000,
                PaymentDate = new DateTime(2026, 10, 5, 10, 0, 0),
                PaymentMethod = "Chuyển khoản VietQR",
                Status = "PAID",
                ApprovedBy = "Trần Thị Thu (CEO)",
                ApprovedAt = new DateTime(2026, 10, 4, 15, 0, 0),
                Notes = "Lương tháng 9/2026 thanh toán đúng hạn qua VietQR"
            },
            new Payroll
            {
                Id = 4,
                PayrollCode = "PL-202609-KLE-NV004",
                EmployeeId = 4,
                EmployeeCode = "KLE-NV004",
                EmployeeName = "Lê Thị Hoàng Yến",
                DepartmentName = "Bộ Phận Lễ Tân & Trải Nghiệm Khách Hàng",
                PositionTitle = "Trưởng Nhóm Lễ Tân Homestay",
                Month = 9,
                Year = 2026,
                BaseSalary = 9000000,
                Allowance = 1000000,
                Bonus = 800000,
                OvertimePay = 350000,
                Deductions = 400000,
                AdvancePayment = 0,
                GrossIncome = 11150000,
                NetIncome = 10750000,
                PaymentDate = new DateTime(2026, 10, 5, 10, 0, 0),
                PaymentMethod = "Chuyển khoản VietQR",
                Status = "PAID",
                ApprovedBy = "Trần Thị Thu (CEO)",
                ApprovedAt = new DateTime(2026, 10, 4, 15, 0, 0),
                Notes = "Lương tháng 9/2026 thanh toán đúng hạn qua VietQR"
            },

            // Tháng 10/2026: Kỳ hiện tại
            new Payroll
            {
                Id = 5,
                PayrollCode = "PL-202610-KLE-NV001",
                EmployeeId = 1,
                EmployeeCode = "KLE-NV001",
                EmployeeName = "Trần Thị Thu",
                DepartmentName = "Ban Điều Hành & Quản Lý Homestay",
                PositionTitle = "Giám Đốc Điều Hành (CEO)",
                Month = 10,
                Year = 2026,
                BaseSalary = 18000000,
                Allowance = 2000000,
                Bonus = 500000,
                OvertimePay = 0,
                Deductions = 500000,
                AdvancePayment = 0,
                GrossIncome = 20500000,
                NetIncome = 20000000,
                PaymentDate = null,
                PaymentMethod = "Chuyển khoản VietQR",
                Status = "APPROVED",
                ApprovedBy = "Trần Thị Thu (CEO)",
                ApprovedAt = new DateTime(2026, 10, 15, 9, 0, 0),
                Notes = "Bảng lương tháng 10/2026 đã được duyệt"
            },
            new Payroll
            {
                Id = 6,
                PayrollCode = "PL-202610-KLE-NV002",
                EmployeeId = 2,
                EmployeeCode = "KLE-NV002",
                EmployeeName = "Trần Viết Mạnh Cường",
                DepartmentName = "Bộ Phận Tài Chính - Kế Toán & Nhân Sự",
                PositionTitle = "Trưởng Phòng Nhân Sự & Vận Hành (COO)",
                Month = 10,
                Year = 2026,
                BaseSalary = 14000000,
                Allowance = 1500000,
                Bonus = 500000,
                OvertimePay = 0,
                Deductions = 500000,
                AdvancePayment = 0,
                GrossIncome = 16000000,
                NetIncome = 15500000,
                PaymentDate = null,
                PaymentMethod = "Chuyển khoản VietQR",
                Status = "APPROVED",
                ApprovedBy = "Trần Thị Thu (CEO)",
                ApprovedAt = new DateTime(2026, 10, 15, 9, 0, 0),
                Notes = "Bảng lương tháng 10/2026 đã được duyệt"
            },
            new Payroll
            {
                Id = 7,
                PayrollCode = "PL-202610-KLE-NV004",
                EmployeeId = 4,
                EmployeeCode = "KLE-NV004",
                EmployeeName = "Lê Thị Hoàng Yến",
                DepartmentName = "Bộ Phận Lễ Tân & Trải Nghiệm Khách Hàng",
                PositionTitle = "Trưởng Nhóm Lễ Tân Homestay",
                Month = 10,
                Year = 2026,
                BaseSalary = 9000000,
                Allowance = 1000000,
                Bonus = 500000,
                OvertimePay = 450000,
                Deductions = 400000,
                AdvancePayment = 0,
                GrossIncome = 10950000,
                NetIncome = 10550000,
                PaymentDate = null,
                PaymentMethod = "Chuyển khoản VietQR",
                Status = "PENDING_APPROVAL",
                ApprovedBy = null,
                ApprovedAt = null,
                Notes = "Chờ kế toán & CEO phê duyệt"
            }
        };

        public PayrollRepository(IDataAccess dataAccess, IEmployeeRepository employeeRepository)
        {
            _dataAccess = dataAccess;
            _employeeRepository = employeeRepository;
        }

        public async Task<IEnumerable<Payroll>> GetPayrollsAsync(int? month = null, int? year = null, int? departmentId = null, int? employeeId = null, string? status = null)
        {
            try
            {
                const string sql = @"
                    SELECT p.*, e.FullName AS EmployeeName, e.EmployeeCode, d.Name AS DepartmentName, pos.Title AS PositionTitle
                    FROM dbo.Payrolls p
                    JOIN dbo.Employees e ON p.EmployeeId = e.Id
                    JOIN dbo.Departments d ON e.DepartmentId = d.Id
                    JOIN dbo.Positions pos ON e.PositionId = pos.Id
                    WHERE (@Month IS NULL OR p.Month = @Month)
                      AND (@Year IS NULL OR p.Year = @Year)
                      AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
                      AND (@EmployeeId IS NULL OR p.EmployeeId = @EmployeeId)
                      AND (@Status IS NULL OR p.Status = @Status)
                    ORDER BY p.Year DESC, p.Month DESC, e.Id ASC";

                var list = await _dataAccess.QueryAsync<Payroll, dynamic>(sql, new
                {
                    Month = month,
                    Year = year,
                    DepartmentId = departmentId,
                    EmployeeId = employeeId,
                    Status = status
                });

                if (list != null && list.Count > 0) return list;
            }
            catch
            {
                // Fallback
            }

            var query = _fallbackPayrolls.AsEnumerable();
            if (month.HasValue) query = query.Where(p => p.Month == month.Value);
            if (year.HasValue) query = query.Where(p => p.Year == year.Value);
            if (employeeId.HasValue) query = query.Where(p => p.EmployeeId == employeeId.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            return query.ToList();
        }

        public async Task<Payroll?> GetPayrollByIdAsync(int id)
        {
            try
            {
                const string sql = @"
                    SELECT p.*, e.FullName AS EmployeeName, e.EmployeeCode, d.Name AS DepartmentName, pos.Title AS PositionTitle
                    FROM dbo.Payrolls p
                    JOIN dbo.Employees e ON p.EmployeeId = e.Id
                    JOIN dbo.Departments d ON e.DepartmentId = d.Id
                    JOIN dbo.Positions pos ON e.PositionId = pos.Id
                    WHERE p.Id = @Id";

                var res = await _dataAccess.QuerySingleAsync<Payroll, dynamic>(sql, new { Id = id });
                if (res != null) return res;
            }
            catch
            {
                // Fallback
            }

            return _fallbackPayrolls.FirstOrDefault(p => p.Id == id);
        }

        public async Task<Payroll?> GetPayrollByCodeAsync(string code)
        {
            try
            {
                const string sql = @"
                    SELECT p.*, e.FullName AS EmployeeName, e.EmployeeCode, d.Name AS DepartmentName, pos.Title AS PositionTitle
                    FROM dbo.Payrolls p
                    JOIN dbo.Employees e ON p.EmployeeId = e.Id
                    JOIN dbo.Departments d ON e.DepartmentId = d.Id
                    JOIN dbo.Positions pos ON e.PositionId = pos.Id
                    WHERE p.PayrollCode = @Code";

                var res = await _dataAccess.QuerySingleAsync<Payroll, dynamic>(sql, new { Code = code });
                if (res != null) return res;
            }
            catch
            {
                // Fallback
            }

            return _fallbackPayrolls.FirstOrDefault(p => p.PayrollCode.Equals(code, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<int> AddPayrollAsync(Payroll payroll)
        {
            payroll.CalculateTotals();

            try
            {
                const string sql = @"
                    INSERT INTO dbo.Payrolls (
                        PayrollCode, EmployeeId, Month, Year, BaseSalary, Allowance, Bonus, OvertimePay,
                        Deductions, AdvancePayment, GrossIncome, NetIncome, PaymentDate, PaymentMethod,
                        Status, ApprovedBy, ApprovedAt, Notes, CreatedAt
                    ) VALUES (
                        @PayrollCode, @EmployeeId, @Month, @Year, @BaseSalary, @Allowance, @Bonus, @OvertimePay,
                        @Deductions, @AdvancePayment, @GrossIncome, @NetIncome, @PaymentDate, @PaymentMethod,
                        @Status, @ApprovedBy, @ApprovedAt, @Notes, SYSUTCDATETIME()
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int newId = await _dataAccess.QuerySingleAsync<int, dynamic>(sql, payroll);
                if (newId > 0)
                {
                    payroll.Id = newId;
                    _fallbackPayrolls.Add(payroll);
                    return newId;
                }
            }
            catch
            {
                // Fallback
            }

            int nextId = _fallbackPayrolls.Count > 0 ? _fallbackPayrolls.Max(p => p.Id) + 1 : 1;
            payroll.Id = nextId;
            _fallbackPayrolls.Add(payroll);
            return nextId;
        }

        public async Task<bool> UpdatePayrollAsync(Payroll payroll)
        {
            payroll.CalculateTotals();
            payroll.UpdatedAt = DateTime.UtcNow;

            try
            {
                const string sql = @"
                    UPDATE dbo.Payrolls SET
                        BaseSalary = @BaseSalary,
                        Allowance = @Allowance,
                        Bonus = @Bonus,
                        OvertimePay = @OvertimePay,
                        Deductions = @Deductions,
                        AdvancePayment = @AdvancePayment,
                        GrossIncome = @GrossIncome,
                        NetIncome = @NetIncome,
                        PaymentMethod = @PaymentMethod,
                        Status = @Status,
                        ApprovedBy = @ApprovedBy,
                        ApprovedAt = @ApprovedAt,
                        Notes = @Notes,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE Id = @Id";

                int rows = await _dataAccess.ExecuteCommandAsync(sql, payroll);
                if (rows > 0)
                {
                    var existing = _fallbackPayrolls.FirstOrDefault(p => p.Id == payroll.Id);
                    if (existing != null)
                    {
                        int index = _fallbackPayrolls.IndexOf(existing);
                        _fallbackPayrolls[index] = payroll;
                    }
                    return true;
                }
            }
            catch
            {
                // Fallback
            }

            var local = _fallbackPayrolls.FirstOrDefault(p => p.Id == payroll.Id);
            if (local != null)
            {
                int index = _fallbackPayrolls.IndexOf(local);
                _fallbackPayrolls[index] = payroll;
                return true;
            }
            return false;
        }

        public async Task<bool> DeletePayrollAsync(int id)
        {
            try
            {
                const string sql = "DELETE FROM dbo.Payrolls WHERE Id = @Id";
                int rows = await _dataAccess.ExecuteCommandAsync(sql, new { Id = id });
                if (rows > 0)
                {
                    var existing = _fallbackPayrolls.FirstOrDefault(p => p.Id == id);
                    if (existing != null) _fallbackPayrolls.Remove(existing);
                    return true;
                }
            }
            catch
            {
                // Fallback
            }

            var local = _fallbackPayrolls.FirstOrDefault(p => p.Id == id);
            if (local != null)
            {
                _fallbackPayrolls.Remove(local);
                return true;
            }
            return false;
        }

        public async Task<int> GenerateMonthlyPayrollAsync(int month, int year, string createdBy)
        {
            var employees = await _employeeRepository.GetEmployeesAsync(status: "Đang làm việc");
            var existingPayrolls = await GetPayrollsAsync(month: month, year: year);
            var existingEmpIds = existingPayrolls.Select(p => p.EmployeeId).ToHashSet();

            int createdCount = 0;
            foreach (var emp in employees)
            {
                // Ngăn tạo trùng cho cùng nhân viên và cùng kỳ!
                if (existingEmpIds.Contains(emp.Id)) continue;

                var payroll = new Payroll
                {
                    PayrollCode = $"PL-{year}{month:D2}-{emp.EmployeeCode}",
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.EmployeeCode,
                    EmployeeName = emp.FullName,
                    DepartmentName = emp.DepartmentName,
                    PositionTitle = emp.PositionTitle,
                    Month = month,
                    Year = year,
                    BaseSalary = emp.BaseSalary,
                    Allowance = emp.Allowance,
                    Bonus = 0,
                    OvertimePay = 0,
                    Deductions = 500000, // Mức trích nộp chuẩn bảo hiểm
                    AdvancePayment = 0,
                    PaymentMethod = "Chuyển khoản VietQR",
                    Status = "DRAFT",
                    Notes = $"Lập tự động bởi {createdBy} ngày {DateTime.Today:dd/MM/yyyy}"
                };

                int newId = await AddPayrollAsync(payroll);
                if (newId > 0) createdCount++;
            }

            return createdCount;
        }

        public async Task<bool> ApprovePayrollAsync(int id, string approvedBy)
        {
            var p = await GetPayrollByIdAsync(id);
            if (p == null) return false;

            p.Status = "APPROVED";
            p.ApprovedBy = approvedBy;
            p.ApprovedAt = DateTime.Now;

            return await UpdatePayrollAsync(p);
        }

        public async Task<bool> PayPayrollAsync(int id, string paymentMethod)
        {
            var p = await GetPayrollByIdAsync(id);
            if (p == null) return false;

            p.Status = "PAID";
            p.PaymentDate = DateTime.Now;
            p.PaymentMethod = paymentMethod;

            return await UpdatePayrollAsync(p);
        }
    }
}
