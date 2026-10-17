using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class EmployeeContract
    {
        public int Id { get; set; }
        public string ContractCode { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? PositionTitle { get; set; }

        public string ContractType { get; set; } = "Chính thức";
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime? EndDate { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal Allowance { get; set; }

        // "Còn hiệu lực", "Sắp hết hạn", "Đã hết hạn", "Đã chấm dứt"
        public string Status { get; set; } = "Còn hiệu lực";
        public DateTime SignedDate { get; set; } = DateTime.Today;
        public string? Notes { get; set; }

        public int DaysRemaining =>
            EndDate.HasValue ? (int)(EndDate.Value.Date - DateTime.Today.Date).TotalDays : 9999;
    }
}
