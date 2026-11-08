using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.CoreBusiness.Services.Interfaces;

namespace KimLongEcoStay.CoreBusiness.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly IGmailService? _gmailService;
        private static readonly ConcurrentBag<EmailSendResult> _sentEmailHistory = new();
        // Bộ nhớ kiểm soát tính Idempotent (chống gửi lặp Email 3 cho cùng 1 đơn đặt phòng)
        private static readonly ConcurrentDictionary<string, DateTime> _dispatchedRemainingBalanceRequests = new();

        public EmailService(EmailSettings? settings = null, HttpClient? httpClient = null, IGmailService? gmailService = null)
        {
            _settings = settings ?? new EmailSettings();
            _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            _gmailService = gmailService;
        }

        public IReadOnlyList<EmailSendResult> GetSentEmailHistory()
        {
            return _sentEmailHistory.OrderByDescending(x => x.SentAt).ToList();
        }

        // =========================================================================
        // 1. TƯƠNG THÍCH NGƯỢC
        // =========================================================================
        public async Task<EmailSendResult> SendBookingConfirmationEmailAsync(Order order, string smartLockPin, string roomName)
        {
            var deposit = (decimal)(order.TotalPrice * 0.5);
            var balance = (decimal)order.TotalPrice - deposit;
            return await SendDepositConfirmedEmailAsync(order, smartLockPin, roomName, deposit, balance);
        }

        // =========================================================================
        // 2. EMAIL 1: XÁC NHẬN NHẬN YÊU CẦU ĐẶT PHÒNG (PENDING DEPOSIT)
        // =========================================================================
        public async Task<EmailSendResult> SendBookingRequestReceivedEmailAsync(Order order, string roomName, decimal depositRequired, string vietQrUrl)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            string recipient = !string.IsNullOrWhiteSpace(order.CustomerEmail)
                ? order.CustomerEmail.Trim()
                : "khachhang@kimlongecostay.vn";

            string subject = $"[Kim Long EcoStay] Tiếp Nhận Đơn Đặt Phòng #{order.UniqueId} — Hướng Dẫn Đặt Cọc 50%";
            string html = GenerateBookingRequestReceivedHtml(order, roomName, depositRequired, vietQrUrl);
            string plainText = $"Kim Long EcoStay - Tiếp Nhận Đặt Phòng #{order.UniqueId}\n" +
                               $"Kính gửi {order.CustomerName},\n" +
                               $"Đơn đặt phòng {roomName} của quý khách đã được tiếp nhận.\n" +
                               $"Số tiền cọc 50%: {depositRequired:N0} VNĐ. Nội dung chuyển khoản: KLE {order.UniqueId} COC50\n" +
                               $"STK: 0905123456 (MB Bank) - KIM LONG ECOSTAY.\nHotline: 0905.123.456";

            return await SendEmailAsync(recipient, subject, html, EmailTemplateType.BOOKING_REQUEST_RECEIVED, order.UniqueId ?? "", plainText);
        }

        // =========================================================================
        // 3. EMAIL 2: XÁC NHẬN THANH TOÁN CỌC 50% THÀNH CÔNG & CẤP PIN SMARTLOCK
        // =========================================================================
        public async Task<EmailSendResult> SendDepositConfirmedEmailAsync(Order order, string smartLockPin, string roomName, decimal depositPaid, decimal remainingBalance)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            string recipient = !string.IsNullOrWhiteSpace(order.CustomerEmail)
                ? order.CustomerEmail.Trim()
                : "khachhang@kimlongecostay.vn";

            string subject = $"[Kim Long EcoStay] Xác Nhận Đã Nhận Cọc 50% & Mã Khóa SmartLock (#{order.UniqueId})";
            string html = GenerateDepositConfirmedHtml(order, smartLockPin, roomName, depositPaid, remainingBalance);
            string plainText = $"Kim Long EcoStay - Xác Nhận Cọc 50% Đơn #{order.UniqueId}\n" +
                               $"Kính gửi {order.CustomerName},\n" +
                               $"Thanh toán tiền cọc 50% thành công! Đặt phòng của bạn đã được xác nhận.\n" +
                               $"Phòng: {roomName}\n" +
                               $"Mã PIN SmartLock: {smartLockPin}\n" +
                               $"Số tiền còn lại thanh toán khi nhận phòng: {remainingBalance:N0} VNĐ.\n" +
                               $"Cảnh báo: Tuyệt đối không chia sẻ mã PIN cho người lạ.\nHotline: 0905.123.456";

            return await SendEmailAsync(recipient, subject, html, EmailTemplateType.DEPOSIT_CONFIRMED, order.UniqueId ?? "", plainText);
        }

        // =========================================================================
        // 4. EMAIL 3: YÊU CẦU THANH TOÁN PHẦN CÒN LẠI KHI CHECK-IN (IDEMPOTENT)
        // =========================================================================
        public async Task<EmailSendResult> SendRemainingBalanceRequestEmailAsync(Order order, string roomName, decimal remainingBalance, string vietQrUrl)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            string bookingCode = order.UniqueId ?? "KLE-UNKNOWN";

            if (remainingBalance <= 0)
            {
                return new EmailSendResult
                {
                    IsSuccess = true,
                    Subject = "Bỏ qua yêu cầu thanh toán (Đã thanh toán đủ)",
                    StatusMessage = $"Khách hàng #{bookingCode} đã thanh toán đủ 100%, không cần gửi email yêu cầu thanh toán phần còn lại.",
                    AssociatedBookingCode = bookingCode,
                    TemplateType = EmailTemplateType.REMAINING_BALANCE_REQUEST
                };
            }

            if (_dispatchedRemainingBalanceRequests.ContainsKey(bookingCode))
            {
                var sentTime = _dispatchedRemainingBalanceRequests[bookingCode];
                return new EmailSendResult
                {
                    IsSuccess = true,
                    Subject = "Bỏ qua (Idempotent: Đã gửi trước đó)",
                    StatusMessage = $"Yêu cầu thanh toán phần còn lại cho #{bookingCode} đã được gửi vào lúc {sentTime:HH:mm dd/MM/yyyy}. Bỏ qua thao tác gửi lặp.",
                    AssociatedBookingCode = bookingCode,
                    TemplateType = EmailTemplateType.REMAINING_BALANCE_REQUEST
                };
            }

            string recipient = !string.IsNullOrWhiteSpace(order.CustomerEmail)
                ? order.CustomerEmail.Trim()
                : "khachhang@kimlongecostay.vn";

            string subject = $"[Kim Long EcoStay] Chào Mừng Nhận Phòng & Yêu Cầu Thanh Toán Số Dư Còn Lại (#{bookingCode})";
            string html = GenerateRemainingBalanceRequestHtml(order, roomName, remainingBalance, vietQrUrl);
            string plainText = $"Kim Long EcoStay - Yêu Cầu Thanh Toán Khoản Tiền Còn Lại #{bookingCode}\n" +
                               $"Kính gửi {order.CustomerName},\n" +
                               $"Số tiền còn lại cần thanh toán: {remainingBalance:N0} VNĐ.\n" +
                               $"Quý khách có thể chuyển khoản hoặc thanh toán tại quầy lễ tân.\nHotline: 0905.123.456";

            var result = await SendEmailAsync(recipient, subject, html, EmailTemplateType.REMAINING_BALANCE_REQUEST, bookingCode, plainText);
            if (result.IsSuccess)
            {
                _dispatchedRemainingBalanceRequests[bookingCode] = DateTime.Now;
            }
            return result;
        }

        // =========================================================================
        // 5. EMAIL 4: XÁC NHẬN THANH TOÁN PHẦN CÒN LẠI THÀNH CÔNG (TẤT TOÁN 100%)
        // =========================================================================
        public async Task<EmailSendResult> SendFullPaymentConfirmedEmailAsync(Order order, string roomName, decimal finalPaidAmount)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            string recipient = !string.IsNullOrWhiteSpace(order.CustomerEmail)
                ? order.CustomerEmail.Trim()
                : "khachhang@kimlongecostay.vn";

            string subject = $"[Kim Long EcoStay] Biên Nhận Tất Toán 100% Lưu Trú (#{order.UniqueId}) — Cảm Ơn Quý Khách";
            string html = GenerateFullPaymentConfirmedHtml(order, roomName, finalPaidAmount);
            string plainText = $"Kim Long EcoStay - Biên Nhận Tất Toán 100% #{order.UniqueId}\n" +
                               $"Kính gửi {order.CustomerName},\n" +
                               $"Đơn đặt phòng {roomName} đã được tất toán 100%. Số dư còn lại: 0 VNĐ.\n" +
                               $"Cảm ơn quý khách đã tin tưởng Kim Long EcoStay! Hotline: 0905.123.456";

            return await SendEmailAsync(recipient, subject, html, EmailTemplateType.FULL_PAYMENT_CONFIRMED, order.UniqueId ?? "", plainText);
        }

        // =========================================================================
        // 6. EMAIL ĐẶC SẢN & ẨM THỰC KIM LONG
        // =========================================================================
        public async Task<EmailSendResult> SendSpecialtyOrderEmailAsync(Order order, decimal shippingFee)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            string recipient = !string.IsNullOrWhiteSpace(order.CustomerEmail)
                ? order.CustomerEmail.Trim()
                : "khachhang@kimlongecostay.vn";

            string subject = $"[Kim Long EcoStay] Xác Nhận Đơn Hàng Đặc Sản & Ẩm Thực Kim Long (#{order.UniqueId})";
            string html = GenerateSpecialtyOrderHtml(order, shippingFee);
            string plainText = $"Kim Long EcoStay - Tiếp Nhận Đơn Đặc Sản #{order.UniqueId}\n" +
                               $"Kính gửi {order.CustomerName},\n" +
                               $"Đơn ẩm thực & đặc sản của quý khách đã được tiếp nhận. Tổng thanh toán: {order.TotalPrice:N0} VNĐ.\n" +
                               $"Bếp đang chuẩn bị giao lên phòng. Hotline: 0905.123.456";

            return await SendEmailAsync(recipient, subject, html, EmailTemplateType.SPECIALTY_ORDER_CONFIRMED, order.UniqueId ?? "", plainText);
        }

        // =========================================================================
        // 7. EMAIL ĐẶT TRẢI NGHIỆM TRÀ CUNG ĐÌNH
        // =========================================================================
        public async Task<EmailSendResult> SendTeaBookingEmailAsync(TeaBookingRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            string recipient = !string.IsNullOrWhiteSpace(request.CustomerEmail)
                ? request.CustomerEmail.Trim()
                : "khachhang@kimlongecostay.vn";

            string subject = $"[Kim Long EcoStay] Tiếp Nhận Đặt Lịch Thưởng Trà Cung Đình Huế (#{request.BookingCode})";
            string html = GenerateTeaBookingHtml(request);
            string plainText = $"Kim Long EcoStay - Đặt Lịch Thưởng Trà #{request.BookingCode}\n" +
                               $"Kính gửi {request.CustomerName},\n" +
                               $"Gói trà: {request.TeaServiceName}\n" +
                               $"Ngày: {request.BookingDate:dd/MM/yyyy} ({request.TimeSlot}) - Số khách: {request.NumberOfGuests}\n" +
                               $"Tổng chi phí: {request.TotalAmount:N0} VNĐ.\n" +
                               $"Homestay sẽ liên hệ xác nhận bàn trà đẹp nhất cho quý khách. Hotline: 0905.123.456";

            return await SendEmailAsync(recipient, subject, html, EmailTemplateType.TEA_SERVICE_CONFIRMED, request.BookingCode, plainText);
        }

        public async Task<EmailSendResult> SendAdminTeaBookingAlertEmailAsync(TeaBookingRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            string adminRecipient = !string.IsNullOrWhiteSpace(_settings.AdminEmail) ? _settings.AdminEmail : "titanio741@gmail.com";

            string subject = $"[KLE ADMIN ALERT] Khách Đặt Trà Cung Đình Mới #{request.BookingCode} ({request.CustomerName})";
            string html = $@"
<div style=""font-family:sans-serif; max-width:600px; margin:0 auto; padding:20px; border:1px solid #D4AF37; border-radius:12px; background:#FAFAF5;"">
    <div style=""background:#14452F; color:#fff; padding:16px; border-radius:8px; text-align:center;"">
        <h3 style=""margin:0; color:#D4AF37;"">🍵 THÔNG BÁO ĐẶT TRÀ CUNG ĐÌNH MỚI</h3>
        <p style=""margin:4px 0 0 0; font-size:12px; opacity:0.9;"">Kim Long EcoStay • Bộ Phận Lễ Tân & Trà Quán</p>
    </div>
    <div style=""padding:16px 8px; color:#2D3748;"">
        <p>Hệ thống vừa tiếp nhận yêu cầu đặt lịch trải nghiệm trà từ website:</p>
        <ul>
            <li><strong>Mã đặt lịch:</strong> {request.BookingCode}</li>
            <li><strong>Gói trải nghiệm:</strong> {request.TeaServiceName}</li>
            <li><strong>Khách hàng:</strong> {request.CustomerName}</li>
            <li><strong>Số điện thoại:</strong> <a href=""tel:{request.CustomerPhone}"">{request.CustomerPhone}</a></li>
            <li><strong>Email:</strong> {request.CustomerEmail}</li>
            <li><strong>Ngày phục vụ:</strong> {request.BookingDate:dd/MM/yyyy} ({request.TimeSlot})</li>
            <li><strong>Số lượng khách:</strong> {request.NumberOfGuests} người</li>
            <li><strong>Tổng tiền:</strong> <strong>{request.TotalAmount:N0} VNĐ</strong></li>
            <li><strong>Ghi chú:</strong> {request.Notes}</li>
        </ul>
        <p style=""font-size:13px; color:#6B7280;"">Vui lòng liên hệ khách để xác nhận bàn trà và thời gian chuẩn bị.</p>
    </div>
</div>";
            string plainText = $"ADMIN ALERT: Khách đặt trà mới #{request.BookingCode} - {request.CustomerName} ({request.CustomerPhone}) - Gói: {request.TeaServiceName} - Ngày: {request.BookingDate:dd/MM/yyyy} {request.TimeSlot} - Tổng: {request.TotalAmount:N0}đ";

            return await SendEmailAsync(adminRecipient, subject, html, EmailTemplateType.ADMIN_TEA_BOOKING_ALERT, request.BookingCode, plainText);
        }

        // =========================================================================
        // 8. EMAIL MÃ PIN SMARTLOCK CHUYÊN BIỆT
        // =========================================================================
        public async Task<EmailSendResult> SendSmartLockPinEmailAsync(string customerName, string customerEmail, string bookingCode, string roomName, string smartLockPin, DateTime checkIn, DateTime checkOut)
        {
            string recipient = !string.IsNullOrWhiteSpace(customerEmail) ? customerEmail.Trim() : "khachhang@kimlongecostay.vn";
            string subject = $"Mã PIN truy cập – Kim Long EcoStay (Đơn #{bookingCode})";
            string html = GenerateSmartLockPinHtml(customerName, bookingCode, roomName, smartLockPin, checkIn, checkOut);
            string plainText = $"Mã PIN truy cập – Kim Long EcoStay\n\n" +
                               $"Kính gửi {customerName},\n" +
                               $"Kim Long EcoStay xin gửi quý khách mã PIN mở cửa SmartLock cho đơn đặt phòng #{bookingCode}:\n" +
                               $"- Phòng: {roomName}\n" +
                               $"- MÃ PIN MỞ CỬA: {smartLockPin}\n" +
                               $"- Thời hạn hiệu lực: Từ 14:00 ngày {checkIn:dd/MM/yyyy} đến 12:00 ngày {checkOut:dd/MM/yyyy}\n\n" +
                               $"CẢNH BÁO BẢO MẬT: Tuyệt đối không chia sẻ mã PIN cho người khác. Khóa sẽ tự động khóa sau 5 lần nhập sai liên tiếp.\n" +
                               $"Hotline hỗ trợ 24/7: 0905.123.456 - Làng Cổ Kim Long, TP. Huế";

            return await SendEmailAsync(recipient, subject, html, EmailTemplateType.SMARTLOCK_PIN_DELIVERY, bookingCode, plainText);
        }

        // =========================================================================
        // 9. EMAIL THÔNG BÁO ADMIN KHI CÓ ĐẶT PHÒNG MỚI
        // =========================================================================
        public async Task<EmailSendResult> SendAdminNewBookingAlertEmailAsync(Order order, string roomName, decimal depositRequired)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            string adminRecipient = !string.IsNullOrWhiteSpace(_settings.AdminEmail) ? _settings.AdminEmail : "titanio741@gmail.com";

            string subject = $"[KLE ADMIN ALERT] Đơn Đặt Phòng Mới #{order.UniqueId} — {order.CustomerName}";
            string html = $@"
<div style=""font-family:sans-serif; max-width:600px; margin:0 auto; padding:20px; border:1px solid #14452F; border-radius:12px; background:#F8FAF9;"">
    <div style=""background:#14452F; color:#fff; padding:18px; border-radius:8px; text-align:center;"">
        <h3 style=""margin:0; color:#D4AF37;"">🔔 THÔNG BÁO ĐƠN ĐẶT PHÒNG MỚI</h3>
        <p style=""margin:4px 0 0 0; font-size:12px; opacity:0.9;"">Kim Long EcoStay System Notification</p>
    </div>
    <div style=""padding:18px 8px; color:#2D3748;"">
        <p>Hệ thống vừa nhận được một đơn đặt phòng mới cần đối soát:</p>
        <table style=""width:100%; border-collapse:collapse; font-size:14px;"">
            <tr><td style=""padding:6px 0; color:#6B7280;"">Mã đơn:</td><td><strong>#{order.UniqueId}</strong></td></tr>
            <tr><td style=""padding:6px 0; color:#6B7280;"">Khách hàng:</td><td><strong>{order.CustomerName}</strong></td></tr>
            <tr><td style=""padding:6px 0; color:#6B7280;"">Điện thoại:</td><td><a href=""tel:{order.CustomerAddress}"">{order.CustomerAddress}</a></td></tr>
            <tr><td style=""padding:6px 0; color:#6B7280;"">Email:</td><td>{order.CustomerEmail}</td></tr>
            <tr><td style=""padding:6px 0; color:#6B7280;"">Buồng phòng:</td><td><strong>{roomName}</strong></td></tr>
            <tr><td style=""padding:6px 0; color:#6B7280;"">Tổng giá trị:</td><td><strong>{order.TotalPrice:N0} VNĐ</strong></td></tr>
            <tr><td style=""padding:6px 0; color:#6B7280;"">Tiền cọc cần đối soát (50%):</td><td><strong style=""color:#C53030;"">{depositRequired:N0} VNĐ</strong></td></tr>
            <tr><td style=""padding:6px 0; color:#6B7280;"">Trạng thái:</td><td><span style=""background:#FEF3C7; color:#92400E; padding:3px 8px; border-radius:12px; font-weight:700;"">CHỜ ĐỐI SOÁT CỌC</span></td></tr>
        </table>
        <div style=""margin-top:16px; padding:12px; background:#FFFBEB; border:1px solid #FCD34D; border-radius:8px; font-size:13px;"">
            Nội dung chuyển khoản khách cần ghi: <strong>KLE {order.UniqueId} COC50</strong>. Sau khi tài khoản nhận được tiền, vui lòng bấm Duyệt Cọc tại Admin Portal để cấp mã PIN mở cửa tự động.
        </div>
    </div>
</div>";
            string plainText = $"ADMIN ALERT: Đơn đặt phòng mới #{order.UniqueId} - Khách: {order.CustomerName} ({order.CustomerAddress}) - Phòng: {roomName} - Cọc 50%: {depositRequired:N0} VNĐ";

            return await SendEmailAsync(adminRecipient, subject, html, EmailTemplateType.ADMIN_NEW_BOOKING_ALERT, order.UniqueId ?? "", plainText);
        }

        // =========================================================================
        // 10. EMAIL LIÊN HỆ & THÔNG BÁO HỎI ĐÁP
        // =========================================================================
        public async Task<EmailSendResult> SendCustomerContactConfirmedEmailAsync(string customerName, string customerEmail, string subject, string message)
        {
            string recipient = !string.IsNullOrWhiteSpace(customerEmail) ? customerEmail.Trim() : "khachhang@kimlongecostay.vn";
            string mailSubject = $"[Kim Long EcoStay] Tiếp Nhận Thư Liên Hệ Của Quý Khách";
            string html = $@"
<div style=""font-family:sans-serif; max-width:600px; margin:0 auto; padding:20px; border:1px solid #E2E8F0; border-radius:12px; background:#fff;"">
    <div style=""background:#14452F; color:#fff; padding:20px; border-radius:8px; text-align:center;"">
        <h3 style=""margin:0; color:#D4AF37;"">Kim Long EcoStay</h3>
        <p style=""margin:4px 0 0 0; font-size:12px; opacity:0.9;"">Cảm Ơn Quý Khách Đã Liên Hệ</p>
    </div>
    <div style=""padding:20px 8px; color:#2D3748;"">
        <p>Kính chào <strong>{customerName}</strong>,</p>
        <p>Chúng tôi đã nhận được thông tin liên hệ của quý khách về: <em>""{subject}""</em>.</p>
        <div style=""background:#F8FAF9; border-left:4px solid #14452F; padding:12px; margin:16px 0; font-style:italic;"">
            {message}
        </div>
        <p>Đội ngũ lễ tân Kim Long EcoStay sẽ phản hồi qua email hoặc số điện thoại của quý khách trong thời gian sớm nhất (tối đa 2 giờ làm việc).</p>
        <p>Hotline hỗ trợ khẩn cấp: <strong>0905.123.456</strong>.</p>
    </div>
</div>";
            string plainText = $"Kim Long EcoStay - Cảm ơn {customerName} đã gửi thư liên hệ về: {subject}. Đội ngũ lễ tân sẽ phản hồi sớm nhất. Hotline: 0905.123.456";

            return await SendEmailAsync(recipient, mailSubject, html, EmailTemplateType.CUSTOMER_CONTACT_CONFIRMED, "", plainText);
        }

        public async Task<EmailSendResult> SendAdminNewContactAlertEmailAsync(string customerName, string customerEmail, string customerPhone, string subject, string message)
        {
            string adminRecipient = !string.IsNullOrWhiteSpace(_settings.AdminEmail) ? _settings.AdminEmail : "titanio741@gmail.com";
            string mailSubject = $"[KLE CONTACT] Thư Liên Hệ Mới Từ {customerName} ({customerPhone})";
            string html = $@"
<div style=""font-family:sans-serif; max-width:600px; margin:0 auto; padding:20px; border:1px solid #14452F; border-radius:12px; background:#fff;"">
    <div style=""background:#14452F; color:#fff; padding:16px; border-radius:8px; text-align:center;"">
        <h3 style=""margin:0; color:#D4AF37;"">✉️ THƯ LIÊN HỆ TỪ KHÁCH HÀNG</h3>
    </div>
    <div style=""padding:16px 8px; color:#2D3748;"">
        <p><strong>Khách hàng:</strong> {customerName}</p>
        <p><strong>Số điện thoại:</strong> <a href=""tel:{customerPhone}"">{customerPhone}</a></p>
        <p><strong>Email:</strong> <a href=""mailto:{customerEmail}"">{customerEmail}</a></p>
        <p><strong>Tiêu đề:</strong> {subject}</p>
        <div style=""background:#F8FAF9; padding:12px; border-radius:6px; border:1px solid #E2E8F0;"">
            <strong>Nội dung:</strong><br>{message}
        </div>
    </div>
</div>";
            string plainText = $"ADMIN ALERT: Khách {customerName} ({customerPhone} / {customerEmail}) gửi liên hệ: {subject} - {message}";

            return await SendEmailAsync(adminRecipient, mailSubject, html, EmailTemplateType.ADMIN_NEW_CONTACT_ALERT, "", plainText);
        }

        // =========================================================================
        // 11. PHƯƠNG THỨC GỬI EMAIL CHÍNH (HTTPS REST API / SMTP / THỬ LẠI)
        // =========================================================================
        public async Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string htmlBody, EmailTemplateType templateType = EmailTemplateType.DEPOSIT_CONFIRMED, string bookingCode = "", string? plainText = null)
        {
            var result = new EmailSendResult
            {
                RecipientEmail = toEmail,
                Subject = subject,
                SentAt = DateTime.Now,
                HtmlContent = htmlBody,
                PlainTextContent = plainText ?? ExtractPlainText(htmlBody),
                TemplateType = templateType,
                AssociatedBookingCode = bookingCode
            };

            // 0. Chế độ kiểm thử / mô phỏng (Test Suite)
            if (_settings.IsSimulationMode)
            {
                result.IsSuccess = true;
                result.IsSimulated = true;
                result.ProviderUsed = "Simulation Mode (Test Suite)";
                result.StatusMessage = $"Email [{templateType}] đã gửi thành công (Mô phỏng) tới {toEmail} lúc {result.SentAt:HH:mm:ss dd/MM/yyyy}.";
                _sentEmailHistory.Add(CreateRedactedHistoryEntry(result));
                return result;
            }

            // Kiểm tra cấu hình và chọn đường dẫn gửi
            string provider = (_settings.Provider ?? "Gmail").Trim();
            bool hasApiKey = !string.IsNullOrWhiteSpace(_settings.ApiKey);
            bool hasSmtpPass = !string.IsNullOrWhiteSpace(_settings.SenderPassword);

            // 0. Trường hợp gửi qua Google Gmail API v1 (Ưu tiên số 1 khi cấu hình Gmail hoặc đã kết nối OAuth)
            bool isGmailPreferred = provider.Equals("Gmail", StringComparison.OrdinalIgnoreCase);
            if ((isGmailPreferred || provider.Equals("Auto", StringComparison.OrdinalIgnoreCase)) && _gmailService != null && _gmailService.IsConnected)
            {
                return await SendViaGmailApiAsync(result, toEmail, subject, htmlBody, result.PlainTextContent);
            }
            if (isGmailPreferred && _gmailService != null && !_gmailService.IsConnected)
            {
                // Nếu chưa xác thực OAuth Google nhưng có API Key Resend/SendGrid/SMTP thì fallback
                if (!hasApiKey && !hasSmtpPass)
                {
                    result.IsSuccess = false;
                    result.IsSimulated = false;
                    result.ProviderUsed = "Google Gmail API (Chưa ủy quyền OAuth 2.0)";
                    result.ErrorDetails = "Quản trị viên chưa hoàn tất ủy quyền tài khoản Google Gmail. Vui lòng vào Cổng Quản Trị > Phân hệ 'Nhật ký Email' > Nhấn 'Kết nối Google Gmail' để cấp quyền.";
                    result.StatusMessage = "Chưa kết nối Gmail OAuth 2.0.";
                    _sentEmailHistory.Add(CreateRedactedHistoryEntry(result));
                    return result;
                }
            }

            // 1. Trường hợp gửi qua Resend HTTPS REST API (Ưu tiên số 1, vượt qua mọi chặn port của Cloud)
            if ((provider.Equals("Resend", StringComparison.OrdinalIgnoreCase) || provider.Equals("Auto", StringComparison.OrdinalIgnoreCase)) && hasApiKey)
            {
                return await SendViaResendRestApiAsync(result, toEmail, subject, htmlBody, result.PlainTextContent);
            }

            // 2. Trường hợp gửi qua SendGrid HTTPS REST API
            if (provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase) && hasApiKey)
            {
                return await SendViaSendGridRestApiAsync(result, toEmail, subject, htmlBody, result.PlainTextContent);
            }

            // 3. Trường hợp cấu hình gửi qua SMTP truyền thống
            if (provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase) && hasSmtpPass)
            {
                return await SendViaSmtpAsync(result, toEmail, subject, htmlBody);
            }

            // 4. Nếu không có API Key và không có mật khẩu SMTP
            if (!hasApiKey && !hasSmtpPass)
            {
                result.IsSuccess = false;
                result.IsSimulated = false;
                result.ProviderUsed = "None (Missing Configuration)";
                result.ErrorDetails = "Chưa cấu hình tài khoản gửi email thật. Quản trị viên vui lòng kết nối Google Gmail qua OAuth 2.0 hoặc khai báo EMAIL_API_KEY trong .env.";
                result.StatusMessage = "Không thể gửi email thật do thiếu cấu hình Gmail OAuth hoặc EMAIL_API_KEY.";
                _sentEmailHistory.Add(CreateRedactedHistoryEntry(result));
                return result;
            }

            // Fallback sang Resend nếu có API key
            if (hasApiKey)
            {
                return await SendViaResendRestApiAsync(result, toEmail, subject, htmlBody, result.PlainTextContent);
            }

            return await SendViaSmtpAsync(result, toEmail, subject, htmlBody);
        }

        private async Task<EmailSendResult> SendViaGmailApiAsync(EmailSendResult result, string toEmail, string subject, string htmlBody, string plainText)
        {
            result.ProviderUsed = "Google Gmail API v1 (OAuth 2.0)";
            try
            {
                var sendRes = await _gmailService!.SendEmailRawAsync(toEmail, subject, htmlBody, plainText);
                if (sendRes.Success)
                {
                    result.IsSuccess = true;
                    result.StatusMessage = $"Email [{result.TemplateType}] đã gửi thành công qua Gmail API tới {toEmail} lúc {result.SentAt:HH:mm:ss dd/MM/yyyy} (MsgID: {sendRes.MessageId}).";
                    result.ErrorDetails = string.Empty;
                }
                else
                {
                    result.IsSuccess = false;
                    result.StatusMessage = $"Gửi qua Gmail API thất bại tới {toEmail}.";
                    result.ErrorDetails = sendRes.Error ?? "Lỗi không xác định từ Google Gmail API.";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.StatusMessage = $"Ngoại lệ khi gọi Gmail API tới {toEmail}.";
                result.ErrorDetails = ex.Message;
            }

            _sentEmailHistory.Add(CreateRedactedHistoryEntry(result));
            return result;
        }

        private async Task<EmailSendResult> SendViaResendRestApiAsync(EmailSendResult result, string toEmail, string subject, string htmlBody, string plainText)
        {
            result.ProviderUsed = "Resend (HTTPS REST API)";
            int attempts = 0;

            while (attempts < Math.Max(1, _settings.MaxRetryAttempts))
            {
                attempts++;
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey.Trim());

                    // Địa chỉ người gửi theo chuẩn Resend
                    // Nếu chưa xác minh tên miền riêng kimlongecostay.vn, dùng sender mặc định onboarding@resend.dev
                    string fromEmail = !string.IsNullOrWhiteSpace(_settings.SenderEmail) ? _settings.SenderEmail.Trim() : "onboarding@resend.dev";
                    string fromName = !string.IsNullOrWhiteSpace(_settings.SenderName) ? _settings.SenderName.Trim() : "Kim Long EcoStay";
                    string fromFormatted = $"{fromName} <{fromEmail}>";

                    var payload = new
                    {
                        from = fromFormatted,
                        to = new[] { toEmail.Trim() },
                        subject = subject,
                        html = htmlBody,
                        text = plainText
                    };

                    string jsonString = JsonSerializer.Serialize(payload);
                    request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(request);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        string id = "";
                        try
                        {
                            using var doc = JsonDocument.Parse(responseBody);
                            if (doc.RootElement.TryGetProperty("id", out var idP)) id = idP.GetString() ?? "";
                        }
                        catch { }

                        result.IsSuccess = true;
                        result.IsSimulated = false;
                        result.StatusMessage = $"Đã gửi email thật thành công qua Resend API (ID: {id}) tới {toEmail} lúc {result.SentAt:HH:mm:ss dd/MM/yyyy}.";
                        _sentEmailHistory.Add(CreateRedactedHistoryEntry(result));
                        return result;
                    }
                    else
                    {
                        result.RetryCount = attempts;
                        result.ErrorDetails = $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase}): {responseBody}";

                        // Nếu là lỗi xác thực (401/403) thì không thử lại vô ích
                        if ((int)response.StatusCode == 401 || (int)response.StatusCode == 403)
                        {
                            result.IsSuccess = false;
                            result.StatusMessage = $"Resend API từ chối xác thực (HTTP {(int)response.StatusCode}): {responseBody}";
                            _sentEmailHistory.Add(CreateRedactedHistoryEntry(result));
                            return result;
                        }

                        if (attempts >= _settings.MaxRetryAttempts)
                        {
                            result.IsSuccess = false;
                            result.StatusMessage = $"Gửi qua Resend API thất bại sau {attempts} lần thử: {responseBody}";
                            _sentEmailHistory.Add(CreateRedactedHistoryEntry(result));
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.RetryCount = attempts;
                    result.ErrorDetails = ex.Message;
                    if (attempts >= _settings.MaxRetryAttempts)
                    {
                        result.IsSuccess = false;
                        result.StatusMessage = $"Lỗi kết nối tới Resend API: {ex.Message}";
                        _sentEmailHistory.Add(CreateRedactedHistoryEntry(result));
                        return result;
                    }
                    await Task.Delay(500 * attempts);
                }
            }

            _sentEmailHistory.Add(CreateRedactedHistoryEntry(result));
            return result;
        }

        private async Task<EmailSendResult> SendViaSendGridRestApiAsync(EmailSendResult result, string toEmail, string subject, string htmlBody, string plainText)
        {
            result.ProviderUsed = "SendGrid (HTTPS REST API)";
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey.Trim());

                var payload = new
                {
                    personalizations = new[]
                    {
                        new { to = new[] { new { email = toEmail.Trim() } } }
                    },
                    from = new
                    {
                        email = !string.IsNullOrWhiteSpace(_settings.SenderEmail) ? _settings.SenderEmail.Trim() : "booking@kimlongecostay.vn",
                        name = _settings.SenderName
                    },
                    subject = subject,
                    content = new[]
                    {
                        new { type = "text/plain", value = plainText },
                        new { type = "text/html", value = htmlBody }
                    }
                };

                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    result.IsSuccess = true;
                    result.StatusMessage = $"Đã gửi email thật thành công qua SendGrid API tới {toEmail}.";
                }
                else
                {
                    string err = await response.Content.ReadAsStringAsync();
                    result.IsSuccess = false;
                    result.ErrorDetails = $"HTTP {(int)response.StatusCode}: {err}";
                    result.StatusMessage = $"SendGrid API trả về lỗi: {err}";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorDetails = ex.Message;
                result.StatusMessage = $"Lỗi kết nối SendGrid: {ex.Message}";
            }

            _sentEmailHistory.Add(CreateRedactedHistoryEntry(result));
            return result;
        }

        private async Task<EmailSendResult> SendViaSmtpAsync(EmailSendResult result, string toEmail, string subject, string htmlBody)
        {
            result.ProviderUsed = "SMTP";
            try
            {
                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword),
                    Timeout = 8000
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName, Encoding.UTF8),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);

                result.IsSuccess = true;
                result.IsSimulated = false;
                result.StatusMessage = $"Email đã gửi thành công qua SMTP tới {toEmail} lúc {result.SentAt:HH:mm:ss dd/MM/yyyy}.";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorDetails = ex.Message;
                result.StatusMessage = $"Lỗi SMTP: {ex.Message} (Lưu ý: Render Free chặn outbound SMTP port 25/465/587, vui lòng dùng EMAIL_API_KEY).";
            }

            _sentEmailHistory.Add(CreateRedactedHistoryEntry(result));
            return result;
        }

        public async Task<EmailSendResult> RetryEmailAsync(string emailId)
        {
            var existing = _sentEmailHistory.FirstOrDefault(x => x.EmailId == emailId);
            if (existing == null)
            {
                return new EmailSendResult { IsSuccess = false, StatusMessage = "Không tìm thấy thư trong lịch sử gửi." };
            }

            return await SendEmailAsync(existing.RecipientEmail, existing.Subject, existing.HtmlContent, existing.TemplateType, existing.AssociatedBookingCode, existing.PlainTextContent);
        }

        // =========================================================================
        // 12. CÁC HÀM GENERATE HTML TEMPLATES ĐẲNG CẤP & BẢO MẬT
        // =========================================================================

        public string GenerateSmartLockPinHtml(string customerName, string bookingCode, string roomName, string smartLockPin, DateTime checkIn, DateTime checkOut)
        {
            string pin = !string.IsNullOrEmpty(smartLockPin) ? smartLockPin : "••••••";
            return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8""><title>Mã PIN truy cập – Kim Long EcoStay</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, sans-serif; background: #F7F8F4; margin: 0; padding: 24px; color: #2D3748; }}
        .email-container {{ max-width: 650px; margin: 0 auto; background: #fff; border-radius: 16px; overflow: hidden; border: 1px solid #E2E8F0; box-shadow: 0 4px 20px rgba(0,0,0,0.06); }}
        .header {{ background: linear-gradient(135deg, #14452F 0%, #0A3D2C 100%); color: #fff; padding: 32px 24px; text-align: center; border-bottom: 3px solid #D4AF37; }}
        .header h1 {{ margin: 0 0 6px 0; font-size: 26px; font-family: Georgia, serif; letter-spacing: 0.5px; }}
        .tagline {{ color: #D4AF37; font-size: 11px; font-weight: 700; letter-spacing: 2px; text-transform: uppercase; margin-bottom: 8px; }}
        .content {{ padding: 32px 28px; }}
        .pin-box {{ background: linear-gradient(135deg, #14452F 0%, #19583D 100%); color: #fff; border-radius: 14px; padding: 26px; text-align: center; margin: 24px 0; border: 2px solid #D4AF37; box-shadow: 0 8px 24px rgba(20,69,47,0.25); }}
        .pin-title {{ font-size: 12px; color: #D4AF37; font-weight: 700; letter-spacing: 1.5px; text-transform: uppercase; margin-bottom: 8px; }}
        .pin-code {{ font-size: 42px; font-weight: 800; letter-spacing: 8px; color: #FFFFFF; background: rgba(0,0,0,0.35); padding: 10px 24px; border-radius: 10px; display: inline-block; font-family: 'Courier New', monospace; border: 1px solid rgba(212,175,55,0.4); }}
        .box {{ background: #F8FAF9; border-radius: 12px; border: 1px solid #E2E8F0; padding: 20px; margin: 20px 0; }}
        .security-alert {{ background: #FFFBEB; border: 1px solid #FCD34D; border-left: 4px solid #D97706; padding: 14px; border-radius: 8px; font-size: 13px; color: #92400E; margin-top: 20px; }}
        .footer {{ background: #1A202C; color: #CBD5E1; padding: 24px; text-align: center; font-size: 12px; line-height: 1.6; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <div class=""tagline"">✦ KHÓA THÔNG MINH SMARTLOCK IOT 24/7 ✦</div>
            <h1>Kim Long EcoStay</h1>
            <p style=""margin:0; opacity:0.85; font-size:14px;"">Nhà Vườn Sinh Thái Cố Đô • Kim Long, TP. Huế</p>
        </div>
        <div class=""content"">
            <p style=""font-size:16px;"">Kính gửi <strong>{customerName}</strong>,</p>
            <p>Hệ thống tự động Kim Long EcoStay xin trân trọng gửi tới quý khách mã PIN mở cửa phòng lưu trú cho đơn đặt phòng <strong>#{bookingCode}</strong>:</p>
            
            <div class=""pin-box"">
                <div class=""pin-title"">MÃ PIN TRUY CẬP PHÒNG CỦA BẠN</div>
                <div class=""pin-code"">{pin}</div>
                <div style=""margin-top:14px; font-size:15px; color:#E2E8F0;"">Buồng phòng: <strong>{roomName}</strong></div>
                <div style=""font-size:13px; color:#CBD5E1; margin-top:8px;"">
                    Hiệu lực: Từ <strong>14:00 ngày {checkIn:dd/MM/yyyy}</strong> đến <strong>12:00 ngày {checkOut:dd/MM/yyyy}</strong>
                </div>
            </div>

            <div class=""box"">
                <div style=""font-weight:700; color:#14452F; margin-bottom:12px; text-transform:uppercase; font-size:13px; letter-spacing:0.5px;"">HƯỚNG DẪN MỞ CỬA PHÒNG NHANH CHÓNG</div>
                <div style=""font-size:14px; line-height:1.7;"">
                    1. Chạm tay vào bề mặt khóa để đánh thức màn hình cảm ứng sáng lên.<br>
                    2. Nhập chính xác 6 chữ số: <strong style=""font-family:monospace; font-size:16px; color:#14452F;"">{pin}</strong><br>
                    3. Bấm phím <strong>#</strong> để xác nhận. Khóa sẽ kêu bíp nhẹ và mở chốt trong 10 giây.
                </div>
            </div>

            <div class=""security-alert"">
                <strong>⚠️ CẢNH BÁO BẢO MẬT & QUY TẮC AN TOÀN:</strong><br>
                • Mã PIN này được cấp riêng cho quý khách. Tuyệt đối <strong>không chia sẻ</strong> mã PIN cho bất kỳ người nào khác.<br>
                • Để phòng chống xâm nhập trái phép, khóa sẽ <strong>tự động khóa tạm thời 15 phút</strong> nếu phát hiện nhập sai quá 5 lần liên tiếp.<br>
                • Khi trả phòng hoặc hết hạn lưu trú, mã PIN sẽ tự động vô hiệu hóa hoàn toàn trên hệ thống.
            </div>

            <p style=""margin-top:24px; font-size:14px; color:#4A5568;"">
                Nếu cần hỗ trợ kỹ thuật hoặc cấp lại mã PIN, quý khách vui lòng liên hệ quầy lễ tân trực 24/7 qua Hotline: <strong>0905.123.456</strong>.
            </p>
        </div>
        <div class=""footer"">
            <strong>Kim Long EcoStay</strong> • Làng Cổ Kim Long, Phường Kim Long, TP. Huế<br>
            Hotline: 0905.123.456 • Email: booking@kimlongecostay.vn
        </div>
    </div>
</body>
</html>";
        }

        public string GenerateBookingConfirmationHtml(Order order, string smartLockPin, string roomName)
        {
            var deposit = (decimal)(order.TotalPrice * 0.5);
            var balance = (decimal)order.TotalPrice - deposit;
            return GenerateDepositConfirmedHtml(order, smartLockPin, roomName, deposit, balance);
        }

        public string GenerateBookingRequestReceivedHtml(Order order, string roomName, decimal depositRequired, string vietQrUrl)
        {
            string uniqueId = order.UniqueId ?? "KLE-2026";
            string customerName = order.CustomerName ?? "Quý Khách";
            string checkIn = order.DatePlaced?.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy");
            string checkOut = order.DateProcessed?.ToString("dd/MM/yyyy") ?? DateTime.Now.AddDays(1).ToString("dd/MM/yyyy");
            decimal total = (decimal)order.TotalPrice;

            return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8""><title>Tiếp Nhận Yêu Cầu Đặt Phòng — Kim Long EcoStay</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, sans-serif; background: #F4F6F4; margin: 0; padding: 20px; color: #2D3748; }}
        .email-container {{ max-width: 650px; margin: 0 auto; background: #fff; border-radius: 14px; overflow: hidden; border: 1px solid #E2E8F0; }}
        .header {{ background: #14452F; color: #fff; padding: 28px 24px; text-align: center; border-bottom: 2px solid #D4AF37; }}
        .header h1 {{ margin: 0 0 6px 0; font-size: 24px; }}
        .content {{ padding: 28px 24px; }}
        .badge-status {{ display: inline-block; background: #FEF3C7; color: #92400E; padding: 6px 14px; border-radius: 20px; font-weight: 700; font-size: 13px; margin-bottom: 16px; border: 1px solid #FCD34D; }}
        .box {{ background: #F8FAF9; border-radius: 10px; border: 1px solid #E2E8F0; padding: 18px; margin: 18px 0; }}
        .qr-section {{ text-align: center; background: #FFFBEB; border: 2px dashed #D4AF37; border-radius: 12px; padding: 20px; margin: 20px 0; }}
        .footer {{ background: #2D3748; color: #CBD5E1; padding: 20px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <div style=""color: #D4AF37; font-size: 11px; font-weight: 700; letter-spacing: 2px;"">✦ TIẾP NHẬN YÊU CẦU ĐẶT PHÒNG ✦</div>
            <h1>Kim Long EcoStay</h1>
            <p style=""margin:0; opacity:0.85;"">Nhà Vườn Sinh Thái Cố Đô • Cạnh Chùa Thiên Mụ Huế</p>
        </div>
        <div class=""content"">
            <span class=""badge-status"">⏳ TRẠNG THÁI: CHỜ THANH TOÁN CỌC 50%</span>
            <p>Kính gửi <strong>{customerName}</strong>,</p>
            <p>Hệ thống Kim Long EcoStay đã tiếp nhận yêu cầu đặt phòng của quý khách. Để bảo đảm giữ chỗ và kích hoạt mã khóa SmartLock tự động, quý khách vui lòng hoàn tất đặt cọc 50% theo thông tin:</p>
            
            <div class=""box"">
                <div style=""font-weight:700; color:#14452F; margin-bottom:10px; border-bottom:1px solid #E2E8F0; padding-bottom:6px;"">THÔNG TIN ĐƠN ĐẶT PHÒNG (#{uniqueId})</div>
                <div>• Buồng phòng: <strong>{roomName}</strong></div>
                <div>• Thời gian: <strong>{checkIn}</strong> (14:00) đến <strong>{checkOut}</strong> (12:00)</div>
                <div>• Tổng tiền phòng: <strong>{total:N0} VNĐ</strong></div>
                <div>• Tiền cọc cần thanh toán (50%): <strong style=""color:#C53030; font-size:16px;"">{depositRequired:N0} VNĐ</strong></div>
                <div>• Tiền còn lại thanh toán khi nhận phòng: <strong>{(total - depositRequired):N0} VNĐ</strong></div>
            </div>

            <div class=""qr-section"">
                <div style=""font-weight:700; color:#92400E; margin-bottom:8px;"">QUÉT MÃ VIETQR ĐỂ ĐẶT CỌC 50% TỰ ĐỘNG</div>
                <img src=""{vietQrUrl}"" alt=""VietQR Cọc 50%"" style=""max-width:240px; border-radius:8px; border:1px solid #E2E8F0;"">
                <div style=""font-size:13px; margin-top:8px; color:#4B5563;"">
                    Nội dung chuyển khoản chuẩn: <strong style=""color:#14452F; font-family:monospace; font-size:15px;"">KLE {uniqueId} COC50</strong><br>
                    Ngân hàng Quân Đội (MBBank) • STK: <strong>0905123456</strong> • Chủ TK: <strong>KIM LONG ECOSTAY</strong>
                </div>
            </div>
            <p style=""font-size:13px; color:#6B7280;""><em>Lưu ý: Sau khi giao dịch cọc được đối soát thành công, quý khách sẽ nhận được Email số 2 kèm Mã PIN mở cửa phòng SmartLock.</em></p>
        </div>
        <div class=""footer"">
            Hotline / Zalo: 0905.123.456 • Làng Cổ Kim Long, TP. Huế
        </div>
    </div>
</body>
</html>";
        }

        public string GenerateDepositConfirmedHtml(Order order, string smartLockPin, string roomName, decimal depositPaid, decimal remainingBalance)
        {
            string pin = !string.IsNullOrEmpty(smartLockPin) ? smartLockPin : "••••••";
            string room = !string.IsNullOrEmpty(roomName) ? roomName : "Phòng Nghỉ Dưỡng Sinh Thái Kim Long";
            string uniqueId = order.UniqueId ?? "KLE-2026";
            string customerName = order.CustomerName ?? "Quý Khách";
            string checkIn = order.DatePlaced?.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy");
            string checkOut = order.DateProcessed?.ToString("dd/MM/yyyy") ?? DateTime.Now.AddDays(1).ToString("dd/MM/yyyy");
            decimal total = (decimal)order.TotalPrice;

            return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8""><title>Xác Nhận Đặt Cọc 50% & Mã Khóa SmartLock — Kim Long EcoStay</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, sans-serif; background: #F4F6F4; margin: 0; padding: 20px; color: #2D3748; }}
        .email-container {{ max-width: 660px; margin: 0 auto; background: #fff; border-radius: 14px; overflow: hidden; border: 1px solid #E2E8F0; }}
        .header {{ background: linear-gradient(135deg, #14452F 0%, #0A3D2C 100%); color: #fff; padding: 32px 24px; text-align: center; border-bottom: 2px solid #D4AF37; }}
        .header h1 {{ margin: 0 0 6px 0; font-size: 26px; }}
        .content {{ padding: 28px 24px; }}
        .pin-box {{ background: linear-gradient(135deg, #14452F 0%, #19583D 100%); color: #fff; border-radius: 12px; padding: 22px; text-align: center; margin: 20px 0; border: 2px solid #D4AF37; }}
        .pin-code {{ font-size: 38px; font-weight: 800; letter-spacing: 6px; color: #fff; background: rgba(0,0,0,0.3); padding: 8px 18px; border-radius: 8px; display: inline-block; font-family: monospace; }}
        .box {{ background: #F8FAF9; border-radius: 10px; border: 1px solid #E2E8F0; padding: 18px; margin: 18px 0; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 6px 0; border-bottom: 1px dashed #E2E8F0; font-size: 14px; }}
        .footer {{ background: #2D3748; color: #CBD5E1; padding: 22px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <div style=""display:inline-block; background:rgba(212,175,55,0.25); border:1px solid #D4AF37; color:#D4AF37; font-size:11px; font-weight:700; padding:4px 14px; border-radius:30px; margin-bottom:10px;"">✦ XÁC NHẬN ĐÃ NHẬN CỌC 50% THÀNH CÔNG ✦</div>
            <h1>Kim Long EcoStay</h1>
            <p style=""margin:0; opacity:0.85;"">Nhà Vườn Sinh Thái Cố Đô • Cạnh Chùa Thiên Mụ Huế</p>
        </div>
        <div class=""content"">
            <p>Kính gửi <strong>{customerName}</strong>,</p>
            <p>Hệ thống Kim Long EcoStay xin trân trọng thông báo: <strong>Giao dịch đặt cọc 50% tiền phòng của quý khách đã được đối soát thành công!</strong> Đơn đặt phòng #{uniqueId} đã chính thức được <strong>XÁC NHẬN</strong>.</p>
            
            <div class=""pin-box"">
                <div style=""font-size:12px; color:#D4AF37; font-weight:700; letter-spacing:1px; margin-bottom:6px;"">MÃ PIN MỞ CỬA SMARTLOCK 10S (TỰ ĐỘNG 24/7)</div>
                <div class=""pin-code"">{pin}</div>
                <div style=""margin-top:10px; font-size:14px; color:#E2E8F0;"">Phòng: <strong>{room}</strong></div>
                <div style=""font-size:12px; opacity:0.85; margin-top:8px;"">Chạm sáng màn hình khóa ➔ Nhập {pin} ➔ Bấm # để vào phòng. Hiệu lực từ 14:00 ngày {checkIn}.</div>
            </div>

            <div class=""box"">
                <div style=""font-weight:700; color:#14452F; margin-bottom:10px; text-transform:uppercase;"">Chi Tiết Quyết Toán & Đặt Cọc</div>
                <div class=""detail-row""><span>Mã đặt phòng:</span><strong>{uniqueId}</strong></div>
                <div class=""detail-row""><span>Tổng tiền phòng:</span><strong>{total:N0} VNĐ</strong></div>
                <div class=""detail-row""><span>Tiền cọc đã nhận (50%):</span><strong style=""color:#15803D;"">✔ {depositPaid:N0} VNĐ</strong></div>
                <div class=""detail-row""><span>Số tiền còn lại thanh toán khi Check-in:</span><strong style=""color:#C53030;"">{remainingBalance:N0} VNĐ</strong></div>
                <div class=""detail-row""><span>Lịch trình lưu trú:</span><span>{checkIn} (14:00) ➔ {checkOut} (12:00)</span></div>
            </div>

            <div style=""background:#F0FDF4; border:1px solid #86EFAC; border-radius:10px; padding:14px; font-size:13px; color:#166534;"">
                🌿 <strong>ĐẶC QUYỀN MIỄN PHÍ DÀNH CHO BẠN:</strong> 02 xe đạp trợ lực điện Eco-Bike kèm GPS Audio Guide tự động thuyết minh di tích chùa Thiên Mụ & Làng cổ Kim Long.
            </div>
        </div>
        <div class=""footer"">
            Kim Long EcoStay • Hotline Tiếp Đón: <strong>0905.123.456</strong> (24/7)<br>
            Địa chỉ: Làng Cổ Kim Long, Phường Kim Long, TP. Huế
        </div>
    </div>
</body>
</html>";
        }

        public string GenerateRemainingBalanceRequestHtml(Order order, string roomName, decimal remainingBalance, string vietQrUrl)
        {
            string uniqueId = order.UniqueId ?? "KLE-2026";
            string customerName = order.CustomerName ?? "Quý Khách";
            decimal total = (decimal)order.TotalPrice;
            decimal depositPaid = total - remainingBalance;

            return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8""><title>Yêu Cầu Thanh Toán Số Dư Còn Lại Khi Nhận Phòng — Kim Long EcoStay</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, sans-serif; background: #F4F6F4; margin: 0; padding: 20px; color: #2D3748; }}
        .email-container {{ max-width: 650px; margin: 0 auto; background: #fff; border-radius: 14px; overflow: hidden; border: 1px solid #E2E8F0; }}
        .header {{ background: #14452F; color: #fff; padding: 28px 24px; text-align: center; border-bottom: 2px solid #D4AF37; }}
        .content {{ padding: 26px 24px; }}
        .badge-notice {{ display:inline-block; background:#E0F2FE; color:#0369A1; padding:6px 14px; border-radius:20px; font-weight:700; font-size:13px; margin-bottom:14px; }}
        .box {{ background:#F8FAF9; border-radius:10px; border:1px solid #E2E8F0; padding:18px; margin:16px 0; }}
        .qr-box {{ text-align:center; background:#FFFBEB; border:2px dashed #D4AF37; border-radius:12px; padding:18px; margin:18px 0; }}
        .footer {{ background:#2D3748; color:#CBD5E1; padding:20px; text-align:center; font-size:12px; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <div style=""color:#D4AF37; font-size:11px; font-weight:700; letter-spacing:1px;"">✦ CHÀO MỪNG QUÝ KHÁCH ĐÃ CHECK-IN ✦</div>
            <h1 style=""margin:4px 0;"">Kim Long EcoStay</h1>
            <p style=""margin:0; opacity:0.85;"">Thông Báo Thanh Toán Khoản Tiền Còn Lại</p>
        </div>
        <div class=""content"">
            <span class=""badge-notice"">🛎️ THỦ TỤC NHẬN PHÒNG HOÀN TẤT</span>
            <p>Kính gửi <strong>{customerName}</strong>,</p>
            <p>Chào mừng quý khách đã đến và nhận phòng tại <strong>Kim Long EcoStay</strong>. Để hoàn tất thủ tục thanh toán lưu trú, hệ thống xin gửi quý khách thông tin thanh toán cho <strong>khoản tiền còn lại</strong>:</p>
            
            <div class=""box"">
                <div style=""font-weight:700; color:#14452F; margin-bottom:8px;"">TỔNG KẾT CHI PHÍ LƯU TRÚ (#{uniqueId})</div>
                <div>• Buồng phòng: <strong>{roomName}</strong></div>
                <div>• Tổng giá trị phòng: <strong>{total:N0} VNĐ</strong></div>
                <div>• Tiền cọc 50% đã thanh toán: <strong style=""color:#15803D;"">✔ {depositPaid:N0} VNĐ</strong></div>
                <div style=""margin-top:8px; padding-top:8px; border-top:1px dashed #CBD5E1;"">• <strong>Số tiền còn lại cần thanh toán:</strong> <strong style=""color:#C53030; font-size:18px;"">{remainingBalance:N0} VNĐ</strong></div>
            </div>

            <div class=""qr-box"">
                <div style=""font-weight:700; color:#92400E; margin-bottom:8px;"">QUÉT VIETQR ĐỂ TẤT TOÁN NHANH</div>
                <img src=""{vietQrUrl}"" alt=""VietQR Tất Toán"" style=""max-width:220px; border-radius:8px; border:1px solid #E2E8F0;"">
                <div style=""font-size:13px; margin-top:8px; color:#4B5563;"">
                    Nội dung chuyển khoản: <strong style=""font-family:monospace; color:#14452F;"">KLE {uniqueId} DU50</strong><br>
                    Quý khách cũng có thể thanh toán trực tiếp bằng tiền mặt hoặc thẻ tại quầy lễ tân.
                </div>
            </div>
            <p style=""font-size:13px; color:#6B7280;"">Chúc quý khách một kỳ nghỉ ngập tràn thư thái tại Cố đô!</p>
        </div>
        <div class=""footer"">
            Kim Long EcoStay • Hotline Tiếp Đón: 0905.123.456 (24/7)
        </div>
    </div>
</body>
</html>";
        }

        public string GenerateFullPaymentConfirmedHtml(Order order, string roomName, decimal finalPaidAmount)
        {
            string uniqueId = order.UniqueId ?? "KLE-2026";
            string customerName = order.CustomerName ?? "Quý Khách";
            decimal total = (decimal)order.TotalPrice;

            return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8""><title>Biên Nhận Tất Toán 100% — Kim Long EcoStay</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, sans-serif; background: #F4F6F4; margin: 0; padding: 20px; color: #2D3748; }}
        .email-container {{ max-width: 650px; margin: 0 auto; background: #fff; border-radius: 14px; overflow: hidden; border: 1px solid #E2E8F0; }}
        .header {{ background: #14452F; color: #fff; padding: 28px 24px; text-align: center; border-bottom: 2px solid #D4AF37; }}
        .content {{ padding: 26px 24px; }}
        .paid-stamp {{ display:inline-block; border:2px solid #16A34A; color:#16A34A; font-weight:800; font-size:16px; padding:6px 16px; border-radius:8px; text-transform:uppercase; letter-spacing:2px; margin-bottom:14px; }}
        .box {{ background:#F8FAF9; border-radius:10px; border:1px solid #E2E8F0; padding:18px; margin:16px 0; }}
        .footer {{ background:#2D3748; color:#CBD5E1; padding:20px; text-align:center; font-size:12px; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <h1 style=""margin:0;"">Kim Long EcoStay</h1>
            <p style=""margin:4px 0 0 0; opacity:0.85;"">Biên Nhận Tất Toán Lưu Trú & Dịch Vụ</p>
        </div>
        <div class=""content"">
            <div style=""text-align:center;""><span class=""paid-stamp"">✔ ĐÃ TẤT TOÁN 100%</span></div>
            <p>Kính gửi <strong>{customerName}</strong>,</p>
            <p>Kim Long EcoStay xin trân trọng xác nhận quý khách đã hoàn tất 100% thanh toán cho đơn đặt phòng #{uniqueId}. Số dư còn phải trả hiện tại là: <strong>0 VNĐ</strong>.</p>
            
            <div class=""box"">
                <div>• Mã đặt phòng: <strong>{uniqueId}</strong></div>
                <div>• Buồng phòng: <strong>{roomName}</strong></div>
                <div>• Tổng giá trị: <strong>{total:N0} VNĐ</strong></div>
                <div>• Số tiền vừa thanh toán: <strong>{finalPaidAmount:N0} VNĐ</strong></div>
                <div>• Số dư còn lại: <strong style=""color:#16A34A;"">0 VNĐ (Đã tất toán)</strong></div>
                <div>• Thời điểm xác nhận: <strong>{DateTime.Now:HH:mm dd/MM/yyyy}</strong></div>
            </div>

            <p>Cảm ơn quý khách đã tin tưởng và đồng hành cùng Nhà vườn sinh thái Kim Long EcoStay. Kính chúc quý khách thượng lộ bình an!</p>
        </div>
        <div class=""footer"">
            Kim Long EcoStay • Làng Cổ Kim Long, TP. Huế • Hotline: 0905.123.456
        </div>
    </div>
</body>
</html>";
        }

        public string GenerateSpecialtyOrderHtml(Order order, decimal shippingFee)
        {
            string uniqueId = order.UniqueId ?? "KLE-SP-" + DateTime.Now.ToString("HHmmss");
            string customerName = order.CustomerName ?? "Quý Khách";
            decimal total = (decimal)order.TotalPrice;

            return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8""><title>Xác Nhận Đơn Hàng Đặc Sản & Ẩm Thực Kim Long</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, sans-serif; background: #F4F6F4; margin: 0; padding: 20px; }}
        .email-container {{ max-width: 650px; margin: 0 auto; background: #fff; border-radius: 14px; overflow: hidden; border: 1px solid #E2E8F0; }}
        .header {{ background: #92400E; color: #fff; padding: 24px; text-align: center; border-bottom: 2px solid #D4AF37; }}
        .content {{ padding: 24px; }}
        .footer {{ background: #2D3748; color: #CBD5E1; padding: 18px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <h2 style=""margin:0;"">Đặc Sản & Ẩm Thực Kim Long</h2>
            <p style=""margin:4px 0 0 0; opacity:0.85;"">Kim Long EcoStay • Tinh Hoa Hương Vị Cố Đô</p>
        </div>
        <div class=""content"">
            <p>Kính gửi <strong>{customerName}</strong>,</p>
            <p>Đơn hàng đặc sản & ẩm thực của quý khách mã <strong>#{uniqueId}</strong> đã được tiếp nhận thành công. Bếp và đội ngũ nghệ nhân đang chuẩn bị chu đáo để giao nóng hổi đến tận phòng của quý khách.</p>
            <div style=""background:#F8FAF9; border-radius:8px; border:1px solid #E2E8F0; padding:14px; margin:14px 0;"">
                <div>• Tổng tiền món: <strong>{total:N0} VNĐ</strong></div>
                <div>• Phí phục vụ tận phòng: <strong>{(shippingFee == 0 ? "Miễn phí phục vụ" : shippingFee.ToString("N0") + " VNĐ")}</strong></div>
                <div>• Tổng thanh toán: <strong style=""color:#92400E; font-size:16px;"">{(total + shippingFee):N0} VNĐ</strong></div>
            </div>
            <p style=""font-size:13px; color:#6B7280;"">Mọi thắc mắc vui lòng liên hệ Bếp Kim Long EcoStay: 0905.123.456.</p>
        </div>
        <div class=""footer"">Kim Long EcoStay • TP. Huế</div>
    </div>
</body>
</html>";
        }

        public string GenerateTeaBookingHtml(TeaBookingRequest request)
        {
            return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8""><title>Tiếp Nhận Đặt Lịch Thưởng Trà Cung Đình Huế</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, sans-serif; background: #F4F6F4; margin: 0; padding: 20px; }}
        .email-container {{ max-width: 650px; margin: 0 auto; background: #fff; border-radius: 14px; overflow: hidden; border: 1px solid #E2E8F0; }}
        .header {{ background: #064E3B; color: #fff; padding: 26px; text-align: center; border-bottom: 2px solid #D4AF37; }}
        .content {{ padding: 24px; }}
        .footer {{ background: #2D3748; color: #CBD5E1; padding: 18px; text-align: center; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <div style=""color:#FCD34D; font-size:11px; font-weight:700; letter-spacing:1px;"">✦ KHÔNG GIAN THƯỞNG TRÀ DI SẢN ✦</div>
            <h2 style=""margin:4px 0;"">Trải Nghiệm Trà Cung Đình Huế</h2>
            <p style=""margin:0; opacity:0.85;"">Kim Long EcoStay • Bên Vườn Bưởi Thanh Trà</p>
        </div>
        <div class=""content"">
            <p>Kính gửi <strong>{request.CustomerName}</strong>,</p>
            <p>Kim Long EcoStay đã tiếp nhận yêu cầu đặt lịch trải nghiệm thưởng trà của quý khách với thông tin sau:</p>
            <div style=""background:#F0FDF4; border-radius:8px; border:1px solid #86EFAC; padding:16px; margin:14px 0;"">
                <div>• Mã đặt lịch: <strong>#{request.BookingCode}</strong></div>
                <div>• Gói trải nghiệm: <strong>{request.TeaServiceName}</strong></div>
                <div>• Ngày phục vụ: <strong>{request.BookingDate:dd/MM/yyyy}</strong></div>
                <div>• Khung giờ: <strong>{request.TimeSlot}</strong></div>
                <div>• Số lượng khách: <strong>{request.NumberOfGuests} khách</strong></div>
                <div>• Tổng chi phí: <strong style=""color:#064E3B; font-size:16px;"">{request.TotalAmount:N0} VNĐ</strong></div>
                <div>• Trạng thái: <strong>{(request.Status == "CONFIRMED" ? "Đã xác nhận chỗ" : "Đang chờ đối soát lịch phục vụ")}</strong></div>
            </div>
            <p style=""font-size:13px; color:#4B5563;"">Nghệ nhân trà sẽ chuẩn bị trà sen và các loại bánh tiến vua trước giờ quý khách ghé 15 phút. Hân hạnh đón tiếp quý khách!</p>
        </div>
        <div class=""footer"">Kim Long EcoStay • Làng Cổ Kim Long, TP. Huế • Hotline: 0905.123.456</div>
    </div>
</body>
</html>";
        }

        private static string ExtractPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            string text = Regex.Replace(html, "<style[^>]*>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "<script[^>]*>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</p>", "\n\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</div>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<[^>]+>", "");
            text = WebUtility.HtmlDecode(text);
            return Regex.Replace(text, @"\n{3,}", "\n\n").Trim();
        }

        private static EmailSendResult CreateRedactedHistoryEntry(EmailSendResult source)
        {
            string safeHtml = Regex.Replace(
                source.HtmlContent ?? string.Empty,
                @"(<div class=""pin-code"">)\s*\d{6}\s*(</div>)",
                "$1••••••$2");
            safeHtml = Regex.Replace(
                safeHtml,
                @"(Nhập\s+)\d{6}(\s+➔\s+Bấm\s+#)",
                "$1••••••$2");

            return new EmailSendResult
            {
                EmailId = source.EmailId,
                IsSuccess = source.IsSuccess,
                IsSimulated = source.IsSimulated,
                RecipientEmail = source.RecipientEmail,
                Subject = source.Subject,
                StatusMessage = source.StatusMessage,
                ErrorDetails = source.ErrorDetails,
                SentAt = source.SentAt,
                RetryCount = source.RetryCount,
                HtmlContent = safeHtml,
                PlainTextContent = source.PlainTextContent,
                AssociatedBookingCode = source.AssociatedBookingCode,
                TemplateType = source.TemplateType,
                ProviderUsed = source.ProviderUsed
            };
        }
    }
}
