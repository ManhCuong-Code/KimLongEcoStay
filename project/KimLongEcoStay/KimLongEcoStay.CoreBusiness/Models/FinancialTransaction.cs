using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class FinancialTransaction
    {
        public int Id { get; set; }
        public string TransactionCode { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        // "INCOME" (Thu) hoặc "EXPENSE" (Chi)
        public string Type { get; set; } = "INCOME";

        public string Category { get; set; } = string.Empty;
        public string SourceType { get; set; } = "BOOKING"; // "BOOKING", "TOUR_BIKE", "CULINARY", "SOUVENIR", "EXPENSE_VOUCHER", "OTHER"
        public string? SourceRefId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "VietQR";
        public string Status { get; set; } = "COMPLETED"; // "COMPLETED", "PENDING"
        public string CreatedBy { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string TypeDisplay => Type == "INCOME" ? "Thu tiền" : "Chi tiền";
        public string TypeBadgeClass => Type == "INCOME" ? "bg-success" : "bg-danger";
    }
}
