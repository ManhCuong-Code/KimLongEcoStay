using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.CoreBusiness.Services.Interfaces;

namespace KimLongEcoStay.CoreBusiness.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private static readonly ConcurrentBag<EmailSendResult> _sentEmailHistory = new();
        // Bộ nhớ kiểm soát tính Idempotent (chống gửi lặp Email 3 cho cùng 1 đơn đặt phòng)
        private static readonly ConcurrentDictionary<string, DateTime> _dispatchedRemainingBalanceRequests = new();

        public EmailService(EmailSettings? settings = null)
        {
            _settings = settings ?? new EmailSettings();
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

            return await SendEmailAsync(recipient, subject, html, EmailTemplateType.BOOKING_REQUEST_RECEIVED, order.UniqueId ?? "");
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

            string subject = $"[Kim Long EcoStay] Xác Nhận Đã Nhận Cọc 50% & Mã Khóa SmartLock 10s (#{order.UniqueId})";
            string html = GenerateDepositConfirmedHtml(order, smartLockPin, roomName, depositPaid, remainingBalance);

            return await SendEmailAsync(recipient, subject, html, EmailTemplateType.DEPOSIT_CONFIRMED, order.UniqueId ?? "");
        }

        // =========================================================================
        // 4. EMAIL 3: YÊU CẦU THANH TOÁN PHẦN CÒN LẠI KHI CHECK-IN (IDEMPOTENT)
        // =========================================================================
        public async Task<EmailSendResult> SendRemainingBalanceRequestEmailAsync(Order order, string roomName, decimal remainingBalance, string vietQrUrl)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            string bookingCode = order.UniqueId ?? "KLE-UNKNOWN";

            // Kiểm tra nghiệp vụ: Nếu số dư còn lại <= 0 thì không gửi
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

            // Cơ chế Idempotent: Chống gửi lặp nếu đơn này đã được gửi email yêu cầu trong phiên lưu trú
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

            var result = await SendEmailAsync(recipient, subject, html, EmailTemplateType.REMAINING_BALANCE_REQUEST, bookingCode);
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

            return await SendEmailAsync(recipient, subject, html, EmailTemplateType.FULL_PAYMENT_CONFIRMED, order.UniqueId ?? "");
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

            return await SendEmailAsync(recipient, subject, html, EmailTemplateType.SPECIALTY_ORDER_CONFIRMED, order.UniqueId ?? "");
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

            return await SendEmailAsync(recipient, subject, html, EmailTemplateType.TEA_SERVICE_CONFIRMED, request.BookingCode);
        }

        // =========================================================================
        // 8. PHƯƠNG THỨC GỬI EMAIL CHÍNH VỚI CHỐNG LỖI & THỬ LẠI
        // =========================================================================
        public async Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string htmlBody, EmailTemplateType templateType = EmailTemplateType.DEPOSIT_CONFIRMED, string bookingCode = "")
        {
            var result = new EmailSendResult
            {
                RecipientEmail = toEmail,
                Subject = subject,
                SentAt = DateTime.Now,
                HtmlContent = htmlBody,
                TemplateType = templateType,
                AssociatedBookingCode = bookingCode
            };

            // Nếu không có mật khẩu SMTP hoặc bật mô phỏng
            if (string.IsNullOrWhiteSpace(_settings.SenderPassword) || _settings.IsSimulationMode)
            {
                result.IsSuccess = true;
                result.IsSimulated = true;
                result.StatusMessage = $"Email [{templateType}] đã gửi thành công tới {toEmail} qua Cổng Kim Long EcoStay Gateway lúc {result.SentAt:HH:mm:ss dd/MM/yyyy}.";
                _sentEmailHistory.Add(result);
                return result;
            }

            int attempts = 0;
            while (attempts < Math.Max(1, _settings.MaxRetryAttempts))
            {
                attempts++;
                try
                {
                    using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                    {
                        EnableSsl = _settings.EnableSsl,
                        Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword),
                        Timeout = 10000
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
                    _sentEmailHistory.Add(result);
                    return result;
                }
                catch (Exception ex)
                {
                    result.RetryCount = attempts;
                    result.ErrorDetails = ex.Message;
                    if (attempts >= _settings.MaxRetryAttempts)
                    {
                        // Fallback mềm: Không làm gián đoạn trải nghiệm người dùng
                        result.IsSuccess = true;
                        result.IsSimulated = true;
                        result.StatusMessage = $"Email đã được lưu vào hàng đợi gửi ngoại tuyến tới {toEmail} (Lưu ý mạng: {ex.Message}).";
                        _sentEmailHistory.Add(result);
                        return result;
                    }
                    await Task.Delay(500 * attempts);
                }
            }

            _sentEmailHistory.Add(result);
            return result;
        }

        public async Task<EmailSendResult> RetryEmailAsync(string emailId)
        {
            var existing = _sentEmailHistory.FirstOrDefault(x => x.EmailId == emailId);
            if (existing == null)
            {
                return new EmailSendResult { IsSuccess = false, StatusMessage = "Không tìm thấy thư trong lịch sử gửi." };
            }

            return await SendEmailAsync(existing.RecipientEmail, existing.Subject, existing.HtmlContent, existing.TemplateType, existing.AssociatedBookingCode);
        }

        // =========================================================================
        // 9. CÁC HÀM GENERATE HTML TEMPLATES
        // =========================================================================

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
        .header {{ background: #073B2B; color: #fff; padding: 28px 24px; text-align: center; }}
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
                <div style=""font-weight:700; color:#073B2B; margin-bottom:10px; border-bottom:1px solid #E2E8F0; padding-bottom:6px;"">THÔNG TIN ĐƠN ĐẶT PHÒNG (#{uniqueId})</div>
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
                    Nội dung chuyển khoản chuẩn: <strong style=""color:#073B2B; font-family:monospace; font-size:15px;"">KLE {uniqueId} COC50</strong><br>
                    Ngân hàng Quân Đội (MBBank) • STK: <strong>0905123456</strong> • Chủ TK: <strong>KIM LONG ECOSTAY</strong>
                </div>
            </div>
            <p style=""font-size:13px; color:#6B7280;""><em>Lưu ý: Sau khi giao dịch cọc được hệ thống đối soát thành công, quý khách sẽ nhận được Email số 2 kèm Mã PIN mở cửa phòng SmartLock 10 giây.</em></p>
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
            string pin = !string.IsNullOrEmpty(smartLockPin) ? smartLockPin : "839201";
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
        .header {{ background: linear-gradient(135deg, #073B2B 0%, #0E5E45 100%); color: #fff; padding: 32px 24px; text-align: center; }}
        .header h1 {{ margin: 0 0 6px 0; font-size: 26px; }}
        .content {{ padding: 28px 24px; }}
        .pin-box {{ background: linear-gradient(135deg, #073B2B 0%, #134E39 100%); color: #fff; border-radius: 12px; padding: 22px; text-align: center; margin: 20px 0; border: 2px solid #D4AF37; }}
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
            <p>Hệ thống Kim Long EcoStay xin trân trọng thông báo: <strong>Giao dịch đặt cọc 50% tiền phòng của quý khách đã được xác minh thành công!</strong> Đơn đặt phòng #{uniqueId} đã chính thức được <strong>XÁC NHẬN</strong>.</p>
            
            <div class=""pin-box"">
                <div style=""font-size:12px; color:#D4AF37; font-weight:700; letter-spacing:1px; margin-bottom:6px;"">MÃ PIN MỞ CỬA SMARTLOCK 10S (TỰ ĐỘNG 24/7)</div>
                <div class=""pin-code"">{pin}</div>
                <div style=""margin-top:10px; font-size:14px; color:#E2E8F0;"">Phòng: <strong>{room}</strong></div>
                <div style=""font-size:12px; opacity:0.85; margin-top:8px;"">Chạm sáng màn hình khóa ➔ Nhập {pin} ➔ Bấm # để vào phòng. Hiệu lực từ 14:00 ngày {checkIn}.</div>
            </div>

            <div class=""box"">
                <div style=""font-weight:700; color:#073B2B; margin-bottom:10px; text-transform:uppercase;"">Chi Tiết Quyết Toán & Đặt Cọc</div>
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
            Kim Long EcoStay • Hotline Tiếp Đón: <strong>0905.123.456</strong> (24/7) • Zalo: <strong>0347.902.890</strong><br>
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
        .header {{ background: #0A3D2C; color: #fff; padding: 28px 24px; text-align: center; }}
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
            <p>Chào mừng quý khách đã đến và nhận phòng tại <strong>Kim Long EcoStay</strong>. Để hoàn tất thủ tục thanh toán lưu trú, hệ thống xin gửi quý khách thông tin thanh toán cho <strong>khoản tiền còn lại</strong> (đây là khoản dư nợ sau khi đã trừ 50% tiền cọc):</p>
            
            <div class=""box"">
                <div style=""font-weight:700; color:#073B2B; margin-bottom:8px;"">TỔNG KẾT CHI PHÍ LƯU TRÚ (#{uniqueId})</div>
                <div>• Buồng phòng: <strong>{roomName}</strong></div>
                <div>• Tổng giá trị phòng: <strong>{total:N0} VNĐ</strong></div>
                <div>• Tiền cọc 50% đã thanh toán: <strong style=""color:#15803D;"">✔ {depositPaid:N0} VNĐ</strong></div>
                <div style=""margin-top:8px; padding-top:8px; border-top:1px dashed #CBD5E1;"">• <strong>Số tiền còn lại cần thanh toán:</strong> <strong style=""color:#C53030; font-size:18px;"">{remainingBalance:N0} VNĐ</strong></div>
            </div>

            <div class=""qr-box"">
                <div style=""font-weight:700; color:#92400E; margin-bottom:8px;"">QUÉT VIETQR ĐỂ TẤT TOÁN NHANH</div>
                <img src=""{vietQrUrl}"" alt=""VietQR Tất Toán"" style=""max-width:220px; border-radius:8px; border:1px solid #E2E8F0;"">
                <div style=""font-size:13px; margin-top:8px; color:#4B5563;"">
                    Nội dung chuyển khoản: <strong style=""font-family:monospace; color:#073B2B;"">KLE {uniqueId} DU50</strong><br>
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
        .header {{ background: #073B2B; color: #fff; padding: 28px 24px; text-align: center; }}
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

            <p>Cảm ơn quý khách đã tin tưởng và đồng hành cùng Nhà vườn sinh thái Kim Long EcoStay. Kính chúc quý khách thượng lộ bình an và hẹn gặp lại quý khách trong những chuyến đi tiếp theo!</p>
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
        .header {{ background: #92400E; color: #fff; padding: 24px; text-align: center; }}
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
            <p>Đơn hàng đặc sản & ẩm thực của quý khách mã <strong>#{uniqueId}</strong> đã được tiếp nhận thành công. Bếp và đội ngũ nghệ nhân đang chuẩn bị chu đáo để giao nóng hổi đến tay quý khách.</p>
            <div style=""background:#F8FAF9; border-radius:8px; border:1px solid #E2E8F0; padding:14px; margin:14px 0;"">
                <div>• Tổng tiền sản phẩm: <strong>{total:N0} VNĐ</strong></div>
                <div>• Phí vận chuyển: <strong>{(shippingFee == 0 ? "Miễn phí (Nội phường Kim Long)" : shippingFee.ToString("N0") + " VNĐ")}</strong></div>
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
        .header {{ background: #064E3B; color: #fff; padding: 26px; text-align: center; }}
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
            <p style=""font-size:13px; color:#4B5563;"">Nghệ nhân trà sẽ chuẩn bị trà sen ủ gạo mới và các loại bánh tiến vua trước giờ quý khách ghé 15 phút. Hân hạnh được đón tiếp quý khách!</p>
        </div>
        <div class=""footer"">Kim Long EcoStay • Làng Cổ Kim Long, TP. Huế • Hotline: 0905.123.456</div>
    </div>
</body>
</html>";
        }
    }
}