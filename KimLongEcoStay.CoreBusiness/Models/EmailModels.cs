using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public enum EmailTemplateType
    {
        BOOKING_REQUEST_RECEIVED,   // Email 1: Nhận yêu cầu đặt phòng & hướng dẫn cọc 50%
        DEPOSIT_CONFIRMED,          // Email 2: Xác nhận thanh toán cọc 50% & cấp mã PIN SmartLock
        REMAINING_BALANCE_REQUEST,  // Email 3: Yêu cầu thanh toán phần tiền còn lại khi Check-in
        FULL_PAYMENT_CONFIRMED,     // Email 4: Xác nhận thanh toán phần còn lại thành công (Tất toán 100%)
        SPECIALTY_ORDER_CONFIRMED,  // Đơn hàng ẩm thực & đặc sản Kim Long
        TEA_SERVICE_CONFIRMED       // Xác nhận đặt lịch thưởng trà cung đình
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = "booking@kimlongecostay.vn";
        public string SenderName { get; set; } = "Kim Long EcoStay — Nhà Vườn Cố Đô";
        public string SenderPassword { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public bool IsSimulationMode { get; set; } = false;
        public int MaxRetryAttempts { get; set; } = 3;
    }

    public class EmailSendResult
    {
        public string EmailId { get; set; } = Guid.NewGuid().ToString("N");
        public bool IsSuccess { get; set; }
        public EmailTemplateType TemplateType { get; set; } = EmailTemplateType.DEPOSIT_CONFIRMED;
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsSimulated { get; set; }
        public string HtmlContent { get; set; } = string.Empty;
        public string AssociatedBookingCode { get; set; } = string.Empty;
        public int RetryCount { get; set; } = 0;
        public string ErrorDetails { get; set; } = string.Empty;
    }

    public class EmailLogEntry : EmailSendResult
    {
    }
}

