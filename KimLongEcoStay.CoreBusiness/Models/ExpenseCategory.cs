namespace KimLongEcoStay.CoreBusiness.Models
{
    public class ExpenseCategory
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#10B981";
        public string IconClass { get; set; } = "bi bi-cash-stack";
        public string? Description { get; set; }
        public decimal TotalAmount { get; set; }
        public double Percentage { get; set; }
    }
}
