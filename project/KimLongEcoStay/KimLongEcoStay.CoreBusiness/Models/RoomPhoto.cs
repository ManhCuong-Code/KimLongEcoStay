using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class RoomPhoto
    {
        public string Url { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
    }
}
