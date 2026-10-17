using System;
using System.Collections.Generic;
using System.Linq;
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
                CurrentPin = "839201",
                BatteryLevel = 96,
                Status = "Locked",
                AssignedGuestName = "Trần Hải Đăng",
                AssignedGuestPhone = "0912.345.678",
                AssignedBookingCode = "KLE-20261015-01",
                ValidFrom = DateTime.Today.AddHours(14),
                ValidUntil = DateTime.Today.AddDays(2).AddHours(12),
                LastUnlockedAt = DateTime.Now.AddHours(-3)
            },
            new SmartLockDevice
            {
                DeviceId = "KLE-IOT-102",
                RoomNumber = "KLE-R102",
                RoomName = "Eco-Nest Hoàng Hôn",
                CurrentPin = "472910",
                BatteryLevel = 92,
                Status = "Locked",
                AssignedGuestName = "Lê Thị Mai Anh",
                AssignedGuestPhone = "0988.765.432",
                AssignedBookingCode = "KLE-20261016-02",
                ValidFrom = DateTime.Today.AddDays(1).AddHours(14),
                ValidUntil = DateTime.Today.AddDays(3).AddHours(12),
                LastUnlockedAt = DateTime.Now.AddDays(-1)
            },
            new SmartLockDevice
            {
                DeviceId = "KLE-IOT-201",
                RoomNumber = "KLE-R201",
                RoomName = "Garden Suite Cố Đô Thanh Trà",
                CurrentPin = "629184",
                BatteryLevel = 98,
                Status = "Unlocked",
                AssignedGuestName = "Nguyễn Văn Hoàng",
                AssignedGuestPhone = "0905.123.456",
                AssignedBookingCode = "KLE-20261014-03",
                ValidFrom = DateTime.Today.AddHours(14),
                ValidUntil = DateTime.Today.AddDays(2).AddHours(12),
                LastUnlockedAt = DateTime.Now.AddMinutes(-25)
            },
            new SmartLockDevice
            {
                DeviceId = "KLE-IOT-202",
                RoomNumber = "KLE-R202",
                RoomName = "Family Eco-Villa Gắn Kết",
                CurrentPin = "519283",
                BatteryLevel = 88,
                Status = "Locked",
                AssignedGuestName = "Gia đình chị Thảo (Hà Nội)",
                AssignedGuestPhone = "0934.567.890",
                AssignedBookingCode = "KLE-20261018-04",
                ValidFrom = DateTime.Today.AddDays(2).AddHours(14),
                ValidUntil = DateTime.Today.AddDays(5).AddHours(12),
                LastUnlockedAt = DateTime.Now.AddDays(-2)
            },
            new SmartLockDevice
            {
                DeviceId = "KLE-IOT-203",
                RoomNumber = "KLE-R203",
                RoomName = "Heritage Studio Trầm Mặc",
                CurrentPin = "739102",
                BatteryLevel = 94,
                Status = "Locked",
                AssignedGuestName = "Phạm Quang Minh",
                AssignedGuestPhone = "0918.234.567",
                AssignedBookingCode = "KLE-20261019-05",
                ValidFrom = DateTime.Today.AddDays(3).AddHours(14),
                ValidUntil = DateTime.Today.AddDays(4).AddHours(12),
                LastUnlockedAt = DateTime.Now.AddDays(-3)
            }
        };

        private static readonly List<SmartLockAuditLog> _auditLogs = new()
        {
            new SmartLockAuditLog { Id = 1, Timestamp = DateTime.Now.AddMinutes(-25), RoomNumber = "KLE-R201", ActionType = "Mở cửa bằng mã PIN khách", Actor = "Khách hàng (Nguyễn Văn Hoàng)", Details = "Bàn phím cảm ứng IoT nhận PIN hợp lệ." },
            new SmartLockAuditLog { Id = 2, Timestamp = DateTime.Now.AddHours(-2), RoomNumber = "KLE-R101", ActionType = "Tự động cấp mã PIN sau khi cọc VietQR", Actor = "Hệ thống IoT Cloud 10s", Details = "Sinh mã PIN 839201 gửi qua tin nhắn Zalo." },
            new SmartLockAuditLog { Id = 3, Timestamp = DateTime.Now.AddHours(-5), RoomNumber = "KLE-R202", ActionType = "Kiểm tra mức pin thiết bị", Actor = "Hệ thống giám sát", Details = "Mức pin đạt 88%, kết nối Zigbee Gateway ổn định." },
            new SmartLockAuditLog { Id = 4, Timestamp = DateTime.Now.AddDays(-1), RoomNumber = "KLE-R102", ActionType = "Mở khóa từ xa khẩn cấp", Actor = "Lễ tân (admin)", Details = "Hỗ trợ khách mở cửa nhận phòng sớm." }
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

            int seed = new Random().Next(100000, 999999);
            newPin = seed.ToString();
            dev.CurrentPin = newPin;

            _auditLogs.Insert(0, new SmartLockAuditLog
            {
                Id = _auditLogs.Count + 1,
                Timestamp = DateTime.Now,
                RoomNumber = roomNumber,
                ActionType = "Tạo lại mã PIN khẩn cấp",
                Actor = "Lễ tân quản trị",
                Details = $"Mã PIN mới: {newPin} được cập nhật vào khóa IoT."
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
                Actor = $"Lễ tân ({adminUser})",
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
