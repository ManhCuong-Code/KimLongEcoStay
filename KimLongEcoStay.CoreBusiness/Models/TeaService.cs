using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class TeaService
    {
        public int Id { get; set; }
        public string ServiceCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PackageType { get; set; } = "Cá nhân"; // Cá nhân, Cặp đôi, Nhóm bạn, Gói văn hóa
        public decimal Price { get; set; }
        public string PriceUnit { get; set; } = "khách"; // khách hoặc gói
        public int DurationMinutes { get; set; } = 60;
        public int MaxGuests { get; set; } = 8;
        public int AvailableSlots { get; set; } = 8;
        public string ServingSchedule { get; set; } = "08:30 - 10:30, 15:30 - 17:30, 19:30 - 21:00";
        public string ImageUrl { get; set; } = "/images/nha_ruong_2.jpg";
        public string ShortDescription { get; set; } = string.Empty;
        public string FullDescription { get; set; } = string.Empty;
        public string IncludedItems { get; set; } = string.Empty;
        public string TermsAndCancellation { get; set; } = "Đặt trước tối thiểu 02 tiếng. Miễn phí hủy trước 04 tiếng.";
        public bool IsActive { get; set; } = true;
        public bool RequiresManualConfirmation { get; set; } = false; // Nếu chưa có lịch thực tế hiển thị 'Cần xác nhận'
    }

    public class TeaBookingRequest
    {
        public int Id { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public int TeaServiceId { get; set; }
        public string TeaServiceName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; } = DateTime.Today;
        public string TimeSlot { get; set; } = "15:30 - 16:30";
        public int NumberOfGuests { get; set; } = 2;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "PENDING_CONFIRMATION"; // PENDING_CONFIRMATION, CONFIRMED, COMPLETED, CANCELLED
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
