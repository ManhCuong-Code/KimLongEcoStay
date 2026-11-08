using System;
using System.Text.RegularExpressions;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class AppNotification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public NotificationType Type { get; set; } = NotificationType.Info;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Service { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? TechnicalDetails { get; set; }
        public string? Recipient { get; set; }
        public bool IsDismissed { get; set; } = false;
        public bool ShowTechnicalDetails { get; set; } = false;

        public static AppNotification Success(string title, string message, string? service = null, string? techDetails = null, string? recipient = null)
        {
            return new AppNotification
            {
                Type = NotificationType.Success,
                Title = title,
                Message = message,
                Service = service,
                TechnicalDetails = techDetails,
                Recipient = recipient
            };
        }

        public static AppNotification Error(string title, string message, string? service = null, string? techDetails = null)
        {
            return new AppNotification
            {
                Type = NotificationType.Error,
                Title = title,
                Message = message,
                Service = service,
                TechnicalDetails = techDetails
            };
        }

        public static AppNotification Warning(string title, string message, string? service = null, string? techDetails = null)
        {
            return new AppNotification
            {
                Type = NotificationType.Warning,
                Title = title,
                Message = message,
                Service = service,
                TechnicalDetails = techDetails
            };
        }

        public static AppNotification Info(string title, string message, string? service = null, string? techDetails = null)
        {
            return new AppNotification
            {
                Type = NotificationType.Info,
                Title = title,
                Message = message,
                Service = service,
                TechnicalDetails = techDetails
            };
        }

        /// <summary>
        /// Chuẩn hóa kết quả EmailSendResult thành Notification Card thanh lịch
        /// </summary>
        public static AppNotification FromEmailResult(EmailSendResult res, string? customTitle = null)
        {
            if (res == null)
            {
                return Error("Không xác định", "Không nhận được phản hồi từ hệ thống gửi email.");
            }

            string templateName = GetTemplateFriendlyName(res.TemplateType);
            string maskedRecipient = MaskEmail(res.RecipientEmail);

            if (res.IsSuccess)
            {
                // Trích xuất MsgId kỹ thuật nếu có
                string msgId = "";
                var match = Regex.Match(res.StatusMessage ?? "", @"\(MsgID:\s*([^)]+)\)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    msgId = match.Groups[1].Value.Trim();
                }

                string tech = $"Dịch vụ: {res.ProviderUsed}\n" +
                              $"Mẫu email: {res.TemplateType} ({templateName})\n" +
                              $"Địa chỉ nhận: {res.RecipientEmail}\n" +
                              $"Thời gian gửi: {res.SentAt:HH:mm:ss – dd/MM/yyyy}\n" +
                              (!string.IsNullOrEmpty(msgId) ? $"Google MsgID: {msgId}\n" : "") +
                              (!string.IsNullOrEmpty(res.AssociatedBookingCode) ? $"Mã đơn đặt: #{res.AssociatedBookingCode}\n" : "");

                return new AppNotification
                {
                    Type = NotificationType.Success,
                    Title = customTitle ?? "Gửi email thành công",
                    Message = $"Email {templateName.ToLowerInvariant()} đã được gửi thành công đến địa chỉ email {maskedRecipient}.",
                    Service = string.IsNullOrEmpty(res.ProviderUsed) ? "Gmail API" : res.ProviderUsed,
                    Timestamp = res.SentAt,
                    TechnicalDetails = tech.Trim(),
                    Recipient = maskedRecipient
                };
            }
            else
            {
                string tech = $"Kênh gửi: {res.ProviderUsed}\n" +
                              $"Chi tiết lỗi kỹ thuật: {res.ErrorDetails}\n" +
                              $"Phản hồi trạng thái: {res.StatusMessage}";

                string friendlyError = "Không thể gửi email qua máy chủ. Vui lòng kiểm tra lại kết nối tài khoản hoặc thử lại.";
                if (!string.IsNullOrEmpty(res.ErrorDetails))
                {
                    if (res.ErrorDetails.Contains("invalid_client", StringComparison.OrdinalIgnoreCase))
                    {
                        friendlyError = "Thông tin xác thực Google OAuth không hợp lệ (Client Secret không đúng).";
                    }
                    else if (res.ErrorDetails.Contains("Chưa kết nối", StringComparison.OrdinalIgnoreCase))
                    {
                        friendlyError = "Chưa kết nối tài khoản Google Gmail. Quản trị viên vui lòng bấm nút Kết nối Gmail.";
                    }
                }

                return new AppNotification
                {
                    Type = NotificationType.Error,
                    Title = customTitle ?? "Gửi email không thành công",
                    Message = friendlyError,
                    Service = string.IsNullOrEmpty(res.ProviderUsed) ? "Gmail API" : res.ProviderUsed,
                    Timestamp = res.SentAt,
                    TechnicalDetails = tech.Trim(),
                    Recipient = maskedRecipient
                };
            }
        }

        /// <summary>
        /// Phân tích và chuẩn hóa chuỗi thông báo thô sơ từ hệ thống cũ sang Notification Card
        /// </summary>
        public static AppNotification FromRawString(string raw, bool isError = false, string? defaultTitle = null)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null!;

            raw = raw.Trim();

            // 1. Nhận diện lỗi Google OAuth JSON
            if (raw.Contains("invalid_client") || raw.Contains("client secret is invalid"))
            {
                return new AppNotification
                {
                    Type = NotificationType.Error,
                    Title = "Lỗi xác thực Google OAuth",
                    Message = "Mã bí mật Client Secret không chính xác hoặc đã bị vô hiệu hóa.",
                    Service = "Google Gmail OAuth 2.0",
                    TechnicalDetails = raw
                };
            }

            if (raw.Contains("access_denied") || raw.Contains("403"))
            {
                return new AppNotification
                {
                    Type = NotificationType.Warning,
                    Title = "Quyền truy cập bị từ chối",
                    Message = "Tài khoản Google chưa được cấp quyền trong danh sách Người dùng thử nghiệm (Test users).",
                    Service = "Google Cloud Console",
                    TechnicalDetails = raw
                };
            }

            // 2. Nhận diện chuỗi kết quả gửi Email
            if (raw.Contains("Email [") || raw.Contains("đã gửi thành công qua Gmail API") || raw.Contains("MsgID:"))
            {
                string msgId = "";
                var mMsg = Regex.Match(raw, @"\(MsgID:\s*([^)]+)\)", RegexOptions.IgnoreCase);
                if (mMsg.Success) msgId = mMsg.Groups[1].Value.Trim();

                string email = "";
                var mEmail = Regex.Match(raw, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
                if (mEmail.Success) email = mEmail.Value;

                return new AppNotification
                {
                    Type = isError ? NotificationType.Error : NotificationType.Success,
                    Title = isError ? "Gửi email thất bại" : "Gửi email thành công",
                    Message = isError 
                        ? "Có lỗi xảy ra trong quá trình truyền tải thư tới máy chủ."
                        : $"Email giao dịch đã được gửi thành công đến {MaskEmail(email)}.",
                    Service = "Gmail API",
                    TechnicalDetails = (!string.IsNullOrEmpty(msgId) ? $"MsgID: {msgId}\n" : "") + raw,
                    Recipient = MaskEmail(email)
                };
            }

            // 3. Nhận diện chuỗi SmartLock IoT
            if (raw.StartsWith("[SmartLock IoT]", StringComparison.OrdinalIgnoreCase))
            {
                string clean = raw.Replace("[SmartLock IoT]", "").Trim();
                return new AppNotification
                {
                    Type = isError ? NotificationType.Error : NotificationType.Success,
                    Title = isError ? "Lỗi khóa SmartLock" : "Điều khiển khóa SmartLock thành công",
                    Message = clean,
                    Service = "SmartLock IoT Hub",
                    TechnicalDetails = raw
                };
            }

            // 4. Nhận diện Check-in / Check-out
            if (raw.Contains("Check-in thành công", StringComparison.OrdinalIgnoreCase))
            {
                return new AppNotification
                {
                    Type = NotificationType.Success,
                    Title = "Check-in thành công",
                    Message = raw,
                    Service = "Lễ Tân & Buồng Phòng"
                };
            }

            if (raw.Contains("Check-out thành công", StringComparison.OrdinalIgnoreCase))
            {
                return new AppNotification
                {
                    Type = NotificationType.Success,
                    Title = "Check-out thành công",
                    Message = raw,
                    Service = "Lễ Tân & Buồng Phòng"
                };
            }

            if (raw.Contains("Đã xử lý hủy đơn", StringComparison.OrdinalIgnoreCase))
            {
                return new AppNotification
                {
                    Type = NotificationType.Warning,
                    Title = "Hủy đơn đặt phòng",
                    Message = raw,
                    Service = "Chính Sách Hoàn Hủy"
                };
            }

            // 5. Trường hợp thông báo thông thường
            return new AppNotification
            {
                Type = isError ? NotificationType.Error : NotificationType.Success,
                Title = defaultTitle ?? (isError ? "Thông báo lỗi" : "Thao tác thành công"),
                Message = raw
            };
        }

        public static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@")) return email ?? "";
            var parts = email.Split('@');
            string name = parts[0];
            string domain = parts[1];

            if (name.Length <= 3)
            {
                return name.Substring(0, 1) + "***@" + domain;
            }
            return name.Substring(0, 3) + "***@" + domain;
        }

        private static string GetTemplateFriendlyName(EmailTemplateType type)
        {
            return type switch
            {
                EmailTemplateType.BOOKING_REQUEST_RECEIVED => "Tiếp nhận đơn đặt phòng & Hướng dẫn cọc 50%",
                EmailTemplateType.DEPOSIT_CONFIRMED => "Xác nhận cọc 50% & Cấp mã PIN SmartLock",
                EmailTemplateType.REMAINING_BALANCE_REQUEST => "Yêu cầu thanh toán 50% còn lại",
                EmailTemplateType.FULL_PAYMENT_CONFIRMED => "Xác nhận tất toán 100% hoàn tất",
                EmailTemplateType.SMARTLOCK_PIN_DELIVERY => "Mã PIN mở khóa SmartLock",
                EmailTemplateType.TEA_SERVICE_CONFIRMED => "Xác nhận đặt lịch thưởng trà Cung đình",
                EmailTemplateType.SPECIALTY_ORDER_CONFIRMED => "Xác nhận đơn đặt đặc sản ẩm thực",
                EmailTemplateType.ADMIN_NEW_BOOKING_ALERT => "Thông báo đơn đặt phòng mới",
                EmailTemplateType.ADMIN_NEW_CONTACT_ALERT => "Thông báo thư liên hệ mới từ khách",
                EmailTemplateType.ADMIN_TEA_BOOKING_ALERT => "Thông báo khách đặt bàn trà",
                _ => "Thông báo hệ thống"
            };
        }
    }
}

