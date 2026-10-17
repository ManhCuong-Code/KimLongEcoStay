using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class SmartLockDevice
    {
        public string DeviceId { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string CurrentPin { get; set; } = string.Empty;
        public int BatteryLevel { get; set; } = 95; // %
        public string Status { get; set; } = "Locked"; // Locked, Unlocked
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string AssignedGuestName { get; set; } = string.Empty;
        public string AssignedGuestPhone { get; set; } = string.Empty;
        public string AssignedBookingCode { get; set; } = string.Empty;
        public DateTime? LastUnlockedAt { get; set; }
        public string ConnectionStatus { get; set; } = "Online (Zigbee IoT)";
    }

    public class SmartLockAuditLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string RoomNumber { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty; // e.g. "Mở cửa bằng PIN", "Tự động cấp PIN 10s", "Tạo lại PIN khẩn cấp"
        public string Actor { get; set; } = string.Empty; // "Khách hàng", "Hệ thống IoT", "Lễ tân"
        public string Details { get; set; } = string.Empty;
    }
}
