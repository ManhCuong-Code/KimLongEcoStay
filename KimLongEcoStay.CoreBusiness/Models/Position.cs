namespace KimLongEcoStay.CoreBusiness.Models
{
    public class Position
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? BaseSalaryRange { get; set; }
        public string? Description { get; set; }
        public int EmployeeCount { get; set; }
    }
}
