using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class Payroll
    {
        public int Id { get; set; }
        public string PayrollCode { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? PositionTitle { get; set; }

        public int Month { get; set; } = DateTime.Today.Month;
        public int Year { get; set; } = DateTime.Today.Year;

        public decimal BaseSalary { get; set; }
        public decimal Allowance { get; set; }
        public decimal Bonus { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal Deductions { get; set; }
        public decimal AdvancePayment { get; set; }

        // Tổng thu nhập = Lương cơ bản + Phụ cấp + Thưởng + Tiền tăng ca
        public decimal GrossIncome { get; set; }

        // Thực lĩnh = Tổng thu nhập - Các khoản khấu trừ - Tạm ứng
        public decimal NetIncome { get; set; }

        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Chuyển khoản VietQR";

        // Trạng thái: "DRAFT" (Nháp), "PENDING_APPROVAL" (Chờ duyệt), "APPROVED" (Đã duyệt), "PAID" (Đã thanh toán)
        public string Status { get; set; } = "DRAFT";

        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public void CalculateTotals()
        {
            GrossIncome = BaseSalary + Allowance + Bonus + OvertimePay;
            NetIncome = Math.Max(0, GrossIncome - Deductions - AdvancePayment);
        }

        public string StatusDisplay => Status switch
        {
            "DRAFT" => "Bản nháp",
            "PENDING_APPROVAL" => "Chờ duyệt",
            "APPROVED" => "Đã duyệt (Chờ chi)",
            "PAID" => "Đã thanh toán",
            _ => Status
        };

        public string StatusBadgeClass => Status switch
        {
            "DRAFT" => "bg-secondary",
            "PENDING_APPROVAL" => "bg-warning text-dark",
            "APPROVED" => "bg-info text-dark",
            "PAID" => "bg-success",
            _ => "bg-secondary"
        };
    }
}
