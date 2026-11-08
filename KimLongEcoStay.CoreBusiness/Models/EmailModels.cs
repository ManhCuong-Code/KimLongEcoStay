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
        TEA_SERVICE_CONFIRMED,      // Xác nhận đặt lịch thưởng trà cung đình cho khách
        ADMIN_NEW_BOOKING_ALERT,    // Thông báo cho Admin khi có đặt phòng mới
        SMARTLOCK_PIN_DELIVERY,     // Gửi mã PIN truy cập SmartLock bảo mật cho khách
        CUSTOMER_CONTACT_CONFIRMED, // Xác nhận đã tiếp nhận liên hệ/hỏi đáp của khách
        ADMIN_NEW_CONTACT_ALERT,    // Thông báo cho Admin khi có khách gửi liên hệ mới
        ADMIN_TEA_BOOKING_ALERT     // Thông báo cho Admin khi có khách đặt trải nghiệm trà
    }

    public class EmailSettings
    {
        /// <summary>
        /// Nhà cung cấp gửi email: "Gmail" (Google Gmail API v1), "Resend", "SendGrid", "Brevo", "Smtp", "Auto"
        /// </summary>
        public string Provider { get; set; } = "Gmail";

        /// <summary>
        /// API Key của dịch vụ email (ví dụ: Resend re_..., SendGrid SG....)
        /// Tuyệt đối không hardcode trong mã nguồn, nạp từ biến môi trường EMAIL_API_KEY hoặc RESEND_API_KEY.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Địa chỉ email quản trị viên để nhận các thông báo đơn mới, liên hệ và đối soát
        /// Nạp từ biến môi trường ADMIN_EMAIL.
        /// </summary>
        public string AdminEmail { get; set; } = "titanio741@gmail.com";

        // Google Gmail API v1 & OAuth 2.0 Web Server Flow
        public string GmailClientId { get; set; } = string.Empty;
        public string GmailClientSecret { get; set; } = string.Empty;
        public string GmailRedirectUri { get; set; } = "http://localhost:5204/api/admin/gmail/oauth2callback";
        public string GmailSender { get; set; } = "titanio741@gmail.com";
        public string GmailRefreshToken { get; set; } = string.Empty;
        public string GmailAccessToken { get; set; } = string.Empty;
        public DateTime? GmailTokenExpiresAt { get; set; }

        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = "titanio741@gmail.com";
        public string SenderName { get; set; } = "Kim Long EcoStay";
        public string SenderPassword { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public bool IsSimulationMode { get; set; } = false;
        public int MaxRetryAttempts { get; set; } = 3;
    }

    public class GmailTokenData
    {
        public string RefreshToken { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string AccountEmail { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class GmailConnectionStatus
    {
        public bool IsConfigured { get; set; }
        public bool IsConnected { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public DateTime? TokenExpiresAt { get; set; }
        public DateTime? LastSentAt { get; set; }
        public string LastStatusMessage { get; set; } = string.Empty;
        public string LastError { get; set; } = string.Empty;
        public int TotalSentCount { get; set; }
        public string RedirectUri { get; set; } = string.Empty;
        public string Scope { get; set; } = "https://www.googleapis.com/auth/gmail.send";
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
        public string PlainTextContent { get; set; } = string.Empty;
        public string AssociatedBookingCode { get; set; } = string.Empty;
        public int RetryCount { get; set; } = 0;
        public string ErrorDetails { get; set; } = string.Empty;
        public string ProviderUsed { get; set; } = string.Empty;
    }

    public class EmailLogEntry : EmailSendResult
    {
    }

    public class ContactInquiry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
