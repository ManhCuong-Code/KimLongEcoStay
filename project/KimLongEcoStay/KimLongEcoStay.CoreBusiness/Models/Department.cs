using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Thống kê phụ trợ
        public int EmployeeCount { get; set; }
    }
}
