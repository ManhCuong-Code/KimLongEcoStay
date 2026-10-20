using System;
using System.Collections.Generic;

namespace KimLongEcoStay.CoreBusiness.Models
{
    /// <summary>
    /// Các nhà cung cấp mô hình trí tuệ nhân tạo được hỗ trợ
    /// </summary>
    public enum AiProviderType
    {
        /// <summary>
        /// Động cơ tri thức chuyên biệt cho Kim Long EcoStay & Di sản Cố Đô Huế.
        /// Chạy 100% offline, phản hồi dưới 1 giây, không cần API Key, không ảo giác (Zero-Hallucination).
        /// </summary>
        LocalSmartEngine = 0,

        /// <summary>
        /// Mô hình Google Gemini (Gemini 2.5 Flash / Pro) qua Google AI Studio API.
        /// </summary>
        Gemini = 1,

        /// <summary>
        /// Mô hình OpenAI (GPT-4o-mini / GPT-4o) qua OpenAI API.
        /// </summary>
        OpenAi = 2
    }

    /// <summary>
    /// Tin nhắn hội thoại giữa Người dùng và Trợ lý AI Homestay
    /// </summary>
    public class AiChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Sender { get; set; } = "Assistant"; // "User", "Assistant", "System"
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? ActionUrl { get; set; }
        public string? ActionLabel { get; set; }
        public string? Category { get; set; } // "Booking", "SmartLock", "Heritage", "Cuisine", "Policy", "Admin"
    }

    /// <summary>
    /// Cấu hình kết nối và tri thức cho Trợ lý AI Homestay
    /// </summary>
    public class HomestayAiConfiguration
    {
        public AiProviderType Provider { get; set; } = AiProviderType.LocalSmartEngine;
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-3.8-flash";
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Ghi chú tri thức bổ sung của chủ homestay (ví dụ: ưu đãi hôm nay, giờ đón khách đặc biệt...)
        /// </summary>
        public string CustomNotes { get; set; } = "Ưu đãi tuần này: Giảm 10% khi đặt trực tiếp, tặng kèm 01 phần bánh ướt thịt nướng than hoa Kim Long và miễn phí 100% xe đạp trợ lực điện Eco-Bike.";
    }
}
