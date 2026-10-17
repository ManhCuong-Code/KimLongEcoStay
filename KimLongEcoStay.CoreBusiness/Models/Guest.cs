using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class Guest
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? IdentityCard { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

