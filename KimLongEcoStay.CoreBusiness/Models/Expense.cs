using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; } = DateTime.Today;
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryCode { get; set; }
        public string? ColorHex { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PayeeName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "Chuyển khoản";

        // "PAID" (Đã thanh toán), "PENDING" (Chờ thanh toán), "CANCELLED" (Đã hủy)
        public string Status { get; set; } = "PAID";

        public string CreatedBy { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; }
        public int? PayrollId { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string StatusDisplay => Status switch
        {
            "PAID" => "Đã thanh toán",
            "PENDING" => "Chờ chi trả",
            "CANCELLED" => "Đã hủy",
            _ => Status
        };

        public string StatusBadgeClass => Status switch
        {
            "PAID" => "bg-success",
            "PENDING" => "bg-warning text-dark",
            "CANCELLED" => "bg-danger",
            _ => "bg-secondary"
        };
    }
}
