using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.CoreBusiness.Services.Interfaces
{
    public interface IEmailService
    {
        // 1. Email cũ tương thích ngược
        Task<EmailSendResult> SendBookingConfirmationEmailAsync(Order order, string smartLockPin, string roomName);

        // 2. Email 1: Nhận yêu cầu đặt phòng (Pending confirmation/deposit 50%)
        Task<EmailSendResult> SendBookingRequestReceivedEmailAsync(Order order, string roomName, decimal depositRequired, string vietQrUrl);

        // 3. Email 2: Xác nhận thanh toán cọc 50% thành công & cấp PIN SmartLock
        Task<EmailSendResult> SendDepositConfirmedEmailAsync(Order order, string smartLockPin, string roomName, decimal depositPaid, decimal remainingBalance);

        // 4. Email 3: Yêu cầu thanh toán phần tiền còn lại khi Check-in (Idempotent)
        Task<EmailSendResult> SendRemainingBalanceRequestEmailAsync(Order order, string roomName, decimal remainingBalance, string vietQrUrl);

        // 5. Email 4: Xác nhận thanh toán phần còn lại & hoàn tất 100%
        Task<EmailSendResult> SendFullPaymentConfirmedEmailAsync(Order order, string roomName, decimal finalPaidAmount);

        // 6. Email Đặc sản & Ẩm thực Kim Long
        Task<EmailSendResult> SendSpecialtyOrderEmailAsync(Order order, decimal shippingFee);

        // 7. Email Đặt dịch vụ Trà Cung Đình Huế (cho khách và thông báo Admin)
        Task<EmailSendResult> SendTeaBookingEmailAsync(TeaBookingRequest request);
        Task<EmailSendResult> SendAdminTeaBookingAlertEmailAsync(TeaBookingRequest request);

        // 8. Email Mã PIN SmartLock chuyên biệt
        Task<EmailSendResult> SendSmartLockPinEmailAsync(string customerName, string customerEmail, string bookingCode, string roomName, string smartLockPin, DateTime checkIn, DateTime checkOut);

        // 9. Email thông báo cho Quản trị viên khi có đặt phòng mới
        Task<EmailSendResult> SendAdminNewBookingAlertEmailAsync(Order order, string roomName, decimal depositRequired);

        // 10. Email liên hệ khách hàng & thông báo Admin
        Task<EmailSendResult> SendCustomerContactConfirmedEmailAsync(string customerName, string customerEmail, string subject, string message);
        Task<EmailSendResult> SendAdminNewContactAlertEmailAsync(string customerName, string customerEmail, string customerPhone, string subject, string message);

        // 11. Gửi email tổng quát & quản lý nhật ký / thử lại
        Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string htmlBody, EmailTemplateType templateType = EmailTemplateType.DEPOSIT_CONFIRMED, string bookingCode = "", string? plainText = null);
        Task<EmailSendResult> RetryEmailAsync(string emailId);
        string GenerateBookingConfirmationHtml(Order order, string smartLockPin, string roomName);
        IReadOnlyList<EmailSendResult> GetSentEmailHistory();
    }
}
