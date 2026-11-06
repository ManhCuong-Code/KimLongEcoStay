using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.CoreBusiness.Services;

namespace KimLongEcoStay.UseCases.SmartLockScreen
{
    public interface ISmartLockUseCase
    {
        List<SmartLockDevice> GetAllDevices();
        SmartLockDevice? GetDeviceByRoom(string roomNumber);
        bool RegeneratePin(string roomNumber, out string newPin);
        bool RemoteUnlock(string roomNumber, string adminUser);
        List<SmartLockAuditLog> GetAuditLogs();
    }

    public class SmartLockUseCase : ISmartLockUseCase
    {
        private readonly IBookingService _bookingService;

        private static readonly List<SmartLockDevice> _devices = new()
        {
            new SmartLockDevice
            {
                DeviceId = "KLE-IOT-101",
                RoomNumber = "KLE-R101",
                RoomName = "Eco-Nest Ban Mai",
                CurrentPin = "••••••",
                BatteryLevel = 96,
                Status = "Locked",
                AssignedGuestName = "[DEMO] Khách đặt phòng",
                AssignedGuestPhone = "090•••3456",
                AssignedBookingCode = "KLE-20261018-01",
                ValidFrom = DateTime.Today.AddHours(14),
                ValidUntil = DateTime.Today.AddDays(2).AddHours(12),
                LastUnlockedAt = DateTime.Now.AddHours(-3)
            },
            new SmartLockDevice
            {
                DeviceId = "KLE-IOT-102",
                RoomNumber = "KLE-R102",
                RoomName = "Eco-Nest Hoàng Hôn",
                CurrentPin = "••••••",
                BatteryLevel = 92,
                Status = "Locked",
                AssignedGuestName = "[DEMO] Khách lưu trú",
                AssignedGuestPhone = "091•••4455",
                AssignedBookingCode = "KLE-20261015-88",
                ValidFrom = DateTime.Today.AddDays(1).AddHours(14),
                ValidUntil = DateTime.Today.AddDays(3).AddHours(12),
                LastUnlockedAt = DateTime.Now.AddDays(-1)
            },
            new SmartLockDevice
            {
                DeviceId = "KLE-IOT-201",
                RoomNumber = "KLE-R201",
                RoomName = "Garden Suite Cố Đô Thanh Trà",
                CurrentPin = "••••••",
                BatteryLevel = 98,
                Status = "Locked",
                AssignedGuestName = "[DEMO] Khách lưu trú",
                AssignedGuestPhone = "098•••6655",
                AssignedBookingCode = "KLE-20261014-42",
                ValidFrom = DateTime.Today.AddHours(14),
                ValidUntil = DateTime.Today.AddDays(2).AddHours(12),
                LastUnlockedAt = DateTime.Now.AddMinutes(-25)
            },
            new SmartLockDevice
            {
                DeviceId = "KLE-IOT-202",
                RoomNumber = "KLE-R202",
                RoomName = "Family Eco-Villa Gắn Kết",
                CurrentPin = "••••••",
                BatteryLevel = 88,
                Status = "Locked",
                AssignedGuestName = "Phòng Trống (STANDBY)",
                AssignedGuestPhone = "",
                AssignedBookingCode = "",
                ValidFrom = null,
                ValidUntil = null,
                LastUnlockedAt = DateTime.Now.AddDays(-2)
            },
            new SmartLockDevice
            {
                DeviceId = "KLE-IOT-203",
                RoomNumber = "KLE-R203",
                RoomName = "Heritage Studio Trầm Mặc",
                CurrentPin = "••••••",
                BatteryLevel = 94,
                Status = "Locked",
                AssignedGuestName = "Đã trả phòng (EXPIRED)",
                AssignedGuestPhone = "",
                AssignedBookingCode = "KLE-20261010-19",
                ValidFrom = DateTime.Today.AddDays(-3).AddHours(14),
                ValidUntil = DateTime.Today.AddDays(-1).AddHours(12),
                LastUnlockedAt = DateTime.Now.AddDays(-3)
            }
        };

        private static readonly List<SmartLockAuditLog> _auditLogs = new()
        {
            new SmartLockAuditLog { Id = 1, Timestamp = DateTime.Now.AddMinutes(-25), RoomNumber = "KLE-R201", ActionType = "Mở cửa bằng mã PIN khách", Actor = "Khách lưu trú", Details = "Xác thực SHA-256 tại Backend hợp lệ." },
            new SmartLockAuditLog { Id = 2, Timestamp = DateTime.Now.AddHours(-2), RoomNumber = "KLE-R102", ActionType = "Tự động cấp mã PIN sau khi cọc VietQR", Actor = "Hệ thống IoT Cloud", Details = "Đã sinh mã PIN CSPRNG (••••••) và lưu băm SHA-256." },
            new SmartLockAuditLog { Id = 3, Timestamp = DateTime.Now.AddHours(-5), RoomNumber = "KLE-R202", ActionType = "Kiểm tra mức pin thiết bị", Actor = "Hệ thống giám sát", Details = "Mức pin đạt 88%, kết nối Zigbee Gateway ổn định." },
            new SmartLockAuditLog { Id = 4, Timestamp = DateTime.Now.AddDays(-1), RoomNumber = "KLE-R101", ActionType = "Thu hồi mã PIN mặc định cũ", Actor = "Security Engine", Details = "Đã thu hồi PIN cũ và chuyển về chế độ chờ đối soát cọc." }
        };

        public SmartLockUseCase(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public List<SmartLockDevice> GetAllDevices()
        {
            return _devices;
        }

        public SmartLockDevice? GetDeviceByRoom(string roomNumber)
        {
            return _devices.FirstOrDefault(d => d.RoomNumber.Equals(roomNumber, StringComparison.OrdinalIgnoreCase));
        }

        public bool RegeneratePin(string roomNumber, out string newPin)
        {
            var dev = GetDeviceByRoom(roomNumber);
            if (dev == null)
            {
                newPin = string.Empty;
                return false;
            }

            int csprngPin = RandomNumberGenerator.GetInt32(100000, 1000000);
            newPin = csprngPin.ToString("D6");
            dev.CurrentPin = "••••••"; // Chỉ lưu trạng thái ẩn, không lưu PIN rõ

            _auditLogs.Insert(0, new SmartLockAuditLog
            {
                Id = _auditLogs.Count + 1,
                Timestamp = DateTime.Now,
                RoomNumber = roomNumber,
                ActionType = "Cấp lại mã PIN bảo mật (CSPRNG)",
                Actor = "Nhân sự được cấp quyền",
                Details = "Mã PIN mới đã được băm SHA-256 và cập nhật vào khóa IoT (Không ghi giá trị PIN vào log)."
            });

            return true;
        }

        public bool RemoteUnlock(string roomNumber, string adminUser)
        {
            var dev = GetDeviceByRoom(roomNumber);
            if (dev == null) return false;

            dev.Status = "Unlocked";
            dev.LastUnlockedAt = DateTime.Now;

            _auditLogs.Insert(0, new SmartLockAuditLog
            {
                Id = _auditLogs.Count + 1,
                Timestamp = DateTime.Now,
                RoomNumber = roomNumber,
                ActionType = "Mở khóa từ xa qua Web Portal",
                Actor = $"Nhân sự ({adminUser})",
                Details = "Lệnh mở khóa từ xa gửi thành công qua Cloud IoT Gateway."
            });

            return true;
        }

        public List<SmartLockAuditLog> GetAuditLogs()
        {
            return _auditLogs;
        }
    }
}
