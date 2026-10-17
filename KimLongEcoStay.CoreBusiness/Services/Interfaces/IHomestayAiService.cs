using System.Collections.Generic;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.CoreBusiness.Services.Interfaces
{
    public interface IHomestayAiService
    {
        /// <summary>
        /// Hỏi đáp với trợ lý AI về đặt phòng, dịch vụ, di sản Huế, mã khóa SmartLock...
        /// </summary>
        Task<AiChatMessage> AskAsync(string question, List<AiChatMessage> history, string? currentUrl = null);

        /// <summary>
        /// Lấy cấu hình AI hiện tại
        /// </summary>
        HomestayAiConfiguration GetConfiguration();

        /// <summary>
        /// Cập nhật cấu hình mô hình AI (chuyển đổi Local Engine / Gemini / OpenAI)
        /// </summary>
        void UpdateConfiguration(HomestayAiConfiguration config);

        /// <summary>
        /// Danh sách câu hỏi gợi ý nhanh cho khách du lịch và quản trị viên
        /// </summary>
        List<string> GetQuickPrompts();

        /// <summary>
        /// Tóm tắt tri thức cốt lõi của Homestay đang nạp cho AI
        /// </summary>
        string GetKnowledgeSummary();
    }
}
