using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime DateOfBirth { get; set; } = new DateTime(1995, 1, 1);
        public string Gender { get; set; } = "Nam";
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Address { get; set; }

        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }

        public int PositionId { get; set; }
        public string? PositionTitle { get; set; }

        public DateTime HireDate { get; set; } = DateTime.Today;
        public string ContractType { get; set; } = "Chính thức";
        public DateTime ContractStartDate { get; set; } = DateTime.Today;
        public DateTime? ContractEndDate { get; set; }

        // Trạng thái: "Đang làm việc", "Nghỉ phép", "Tạm nghỉ", "Đã nghỉ việc"
        public string WorkStatus { get; set; } = "Đang làm việc";

        public decimal BaseSalary { get; set; }
        public decimal Allowance { get; set; }
        public string? Notes { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Thuộc tính tính toán hỗ trợ hiển thị
        public bool IsContractExpiringSoon =>
            ContractEndDate.HasValue &&
            ContractEndDate.Value >= DateTime.Today &&
            ContractEndDate.Value <= DateTime.Today.AddDays(45);

        public int DaysUntilContractExpires =>
            ContractEndDate.HasValue ? (int)(ContractEndDate.Value.Date - DateTime.Today.Date).TotalDays : 9999;
    }
}
