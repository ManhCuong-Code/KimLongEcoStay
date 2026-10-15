using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.CoreBusiness.Services.Interfaces;

namespace KimLongEcoStay.CoreBusiness.Services
{
    public class HomestayAiService : IHomestayAiService
    {
        private readonly HttpClient _httpClient;
        private HomestayAiConfiguration _config;

        public HomestayAiService(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            _config = new HomestayAiConfiguration
            {
                Provider = AiProviderType.LocalSmartEngine,
                Model = "gemini-2.5-flash",
                IsEnabled = true,
                ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty
            };
        }

        public HomestayAiConfiguration GetConfiguration()
        {
            return new HomestayAiConfiguration
            {
                Provider = _config.Provider,
                ApiKey = _config.ApiKey,
                Model = _config.Model,
                IsEnabled = _config.IsEnabled,
                CustomNotes = _config.CustomNotes
            };
        }

        public void UpdateConfiguration(HomestayAiConfiguration config)
        {
            if (config != null)
            {
                _config = config;
            }
        }

        public List<string> GetQuickPrompts()
        {
            return new List<string>
            {
                "🌿 Gợi ý phòng nghỉ dưỡng 2 người view vườn thanh trà?",
                "🔑 Nhận phòng 10 giây với khóa SmartLock như thế nào?",
                "💳 Chính sách cọc 50% VietQR và quy định hủy phòng?",
                "🚴 Trải nghiệm xe đạp Eco-Bike & GPS Audio Guide ra sao?",
                "🍜 Homestay có đặc sản bánh ướt Kim Long giao tận phòng không?",
                "🗺️ Từ homestay đi Chùa Thiên Mụ và Đại Nội bao xa?"
            };
        }

        public string GetKnowledgeSummary()
        {
            return @"• Vị trí: Kim Long EcoStay, Phường Kim Long, TP. Huế (Cạnh Chùa Thiên Mụ 1.2km, ven sông Hương).
• Mô hình: Homestay Sinh Thái Nhà Vườn Nông Nghiệp kết hợp Nền Tảng Du Lịch Di Sản Thông Minh.
• Hệ thống buồng phòng:
  1. Eco-Nest Ban Mai: 390.000 đ/đêm (2 người, view vườn bưởi thanh trà).
  2. Eco-Nest Hoàng Hôn: 450.000 đ/đêm (2 người, view sông Hương lãng mạn).
  3. Garden Suite Cố Đô Thanh Trà: 680.000 đ/đêm (3 người, nhà rường gỗ cổ truyền).
  4. Family Eco-Villa: 1.450.000 đ/đêm (6-8 người, nguyên căn 300m2 sân vườn riêng).
  5. Heritage Studio Nghệ Nhân: 580.000 đ/đêm (2-3 người, góc trà đạo & tranh sen).
• Đặc quyền miễn phí: 2 xe đạp trợ lực điện Eco-Bike kèm GPS Audio Guide 3 ngôn ngữ, ngâm chân thảo mộc hoa bưởi, trà sen Cung đình đón tiếp.
• Thanh toán & Cọc: Chuyển cọc 50% qua VietQR vào TPBank: 10787723714 (Trần Viết Mạnh Cường). 50% còn lại thanh toán khi check-in.
• Khóa SmartLock Cloud 10s: Cấp mã PIN 6 số tự động sau khi cọc VietQR, mở cửa tự do 24/7 không cần gặp lễ tân.
• Quy định hủy phòng (Luật 48 giờ): Hủy trước 48h nhận phòng hoàn 100% tiền cọc trong 24h. Hủy sau 48h mất tiền cọc.
• Giờ quy định: Check-in 14:00 | Check-out 12:00 hôm sau.";
        }

        public async Task<AiChatMessage> AskAsync(string question, List<AiChatMessage> history, string? currentUrl = null)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return new AiChatMessage
                {
                    Sender = "Assistant",
                    Content = "Dạ, em là Trợ lý AI Kim Long EcoStay. Quý khách vui lòng nhập câu hỏi hoặc chọn một trong các gợi ý bên dưới để em hỗ trợ tư vấn chu đáo nhất ạ! 🌿"
                };
            }

            // Nếu người dùng chọn mô hình Cloud (Gemini / OpenAI) và đã nhập API Key
            if (_config.Provider != AiProviderType.LocalSmartEngine && !string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                try
                {
                    if (_config.Provider == AiProviderType.Gemini)
                    {
                        var geminiResponse = await CallGeminiAsync(question, history);
                        if (!string.IsNullOrWhiteSpace(geminiResponse))
                        {
                            return new AiChatMessage
                            {
                                Sender = "Assistant",
                                Content = geminiResponse,
                                Category = "CloudAi",
                                ActionUrl = DetermineActionUrl(question),
                                ActionLabel = DetermineActionLabel(question)
                            };
                        }
                    }
                    else if (_config.Provider == AiProviderType.OpenAi)
                    {
                        var openAiResponse = await CallOpenAiAsync(question, history);
                        if (!string.IsNullOrWhiteSpace(openAiResponse))
                        {
                            return new AiChatMessage
                            {
                                Sender = "Assistant",
                                Content = openAiResponse,
                                Category = "CloudAi",
                                ActionUrl = DetermineActionUrl(question),
                                ActionLabel = DetermineActionLabel(question)
                            };
                        }
                    }
                }
                catch
                {
                    // Khi mạng lỗi hoặc API Key hết quota, tự động fallback an toàn sang Động cơ Nội bộ
                }
            }

            // ĐỘNG CƠ TRI THỨC BẢN ĐỊA NỘI BỘ (LOCAL SMART KNOWLEDGE ENGINE)
            return GenerateLocalSmartResponse(question);
        }

        private AiChatMessage GenerateLocalSmartResponse(string question)
        {
            string q = question.ToLowerInvariant().Trim();

            // 1. NHÓM TƯ VẤN PHÒNG & GIÁ PHÒNG
            if (q.Contains("phòng") || q.Contains("giá") || q.Contains("đặt") || q.Contains("view") || q.Contains("ở mấy người") || q.Contains("phòng nào") || q.Contains("eco-nest") || q.Contains("suite") || q.Contains("villa"))
            {
                if (q.Contains("gia đình") || q.Contains("nhóm") || q.Contains("nhiều người") || q.Contains("6 người") || q.Contains("8 người") || q.Contains("villa"))
                {
                    return new AiChatMessage
                    {
                        Sender = "Assistant",
                        Content = @"🏡 **Lựa chọn lý tưởng nhất cho gia đình/nhóm:** **Family Eco-Villa (Sân Vườn Di Sản)**
• **Giá ưu đãi:** **1.450.000 VNĐ / đêm** *(giá gốc qua sàn OTA: 1.800.000 VNĐ)*
• **Sức chứa:** 6 – 8 khách người lớn.
• **Đặc điểm:** Khuôn viên biệt lập rộng 300m², 2 phòng ngủ lớn, bếp gia đình đầy đủ gia vị truyền thống, hiên ngắm vườn bưởi thanh trà và sân nướng BBQ ngoài trời.
• **Đặc quyền kèm theo:** 04 xe đạp trợ lực điện Eco-Bike miễn phí và set trà sen Cung đình Huế đón tiếp! 🍵",
                        ActionUrl = "/room/4",
                        ActionLabel = "👉 Xem Chi Tiết Family Eco-Villa",
                        Category = "Booking"
                    };
                }

                if (q.Contains("hoàng hôn") || q.Contains("sông hương") || q.Contains("view sông"))
                {
                    return new AiChatMessage
                    {
                        Sender = "Assistant",
                        Content = @"🌅 **Phòng Eco-Nest Hoàng Hôn (Sunset Room)**
• **Giá đặt trực tiếp:** **450.000 VNĐ / đêm** *(Tiết kiệm 10% so với Agoda/Booking)*
• **Sức chứa:** 2 người lớn (phù hợp cặp đôi, bạn thân).
• **Không gian:** Ban công hướng thẳng ra ngã ba sông Hương, ngắm ánh hoàng hôn tím xứ Huế và thuyền rồng qua lại cực kỳ thơ mộng.
• **Tiện nghi:** Giường King đệm cao su thiên nhiên, Smart TV, trà hoa cúc Huế và bồn tắm thư giãn. 🌸",
                        ActionUrl = "/room/2",
                        ActionLabel = "👉 Xem Phòng Eco-Nest Hoàng Hôn",
                        Category = "Booking"
                    };
                }

                if (q.Contains("rường") || q.Contains("gỗ") || q.Contains("cổ") || q.Contains("suite") || q.Contains("thanh trà"))
                {
                    return new AiChatMessage
                    {
                        Sender = "Assistant",
                        Content = @"🌿 **Garden Suite Cố Đô Thanh Trà (Nhà Rường Di Sản)**
• **Giá đặt trực tiếp:** **680.000 VNĐ / đêm**
• **Sức chứa:** 3 người lớn (01 giường King + 01 giường phụ sinh thái).
• **Kiến trúc:** Dựng theo lối nhà rường Huế cổ 1 gian 2 chái, vì kèo gỗ chạm hoa sen tinh xảo, nằm giữa vườn bưởi Thanh Trà Kim Long râm mát.
• **Đặc quyền riêng:** Tặng kèm 01 bồn ngâm chân thảo mộc hoa bưởi Kim Long và xe đạp Eco-Bike miễn phí! 🛁",
                        ActionUrl = "/room/3",
                        ActionLabel = "👉 Xem Chi Tiết Garden Suite",
                        Category = "Booking"
                    };
                }

                // Tổng quan bảng giá chung
                return new AiChatMessage
                {
                    Sender = "Assistant",
                    Content = @"🌿 **Bảng Giá Buồng Phòng Trực Tiếp Tại Kim Long EcoStay:**
1. **Eco-Nest Ban Mai:** `390.000 đ/đêm` (2 khách, view đón nắng sớm vườn thanh trà).
2. **Eco-Nest Hoàng Hôn:** `450.000 đ/đêm` (2 khách, view hoàng hôn sông Hương tuyệt đẹp).
3. **Garden Suite Cố Đô:** `680.000 đ/đêm` (3 khách, phong cách nhà rường gỗ cổ truyền).
4. **Heritage Studio Nghệ Nhân:** `580.000 đ/đêm` (2-3 khách, góc trà đạo & tranh sen).
5. **Family Eco-Villa:** `1.450.000 đ/đêm` (6-8 khách, nguyên căn sân vườn 300m²).

🎁 **Đặc quyền khi đặt trực tiếp trên Web:**
• Tiết kiệm **10%** tiền hoa hồng so với đặt qua các sàn OTA trung gian.
• Tặng 01 phần bánh ướt thịt nướng than hoa Kim Long và mứt gừng Huế nồng ấm.
• Miễn phí 100% xe đạp trợ lực điện Eco-Bike & tai nghe thuyết minh GPS di sản!",
                    ActionUrl = "/#rooms",
                    ActionLabel = "👉 Chọn Buồng Phòng Nghỉ Dưỡng",
                    Category = "Booking"
                };
            }

            // 2. NHÓM KHÓA THÔNG MINH SMARTLOCK & NHẬN PHÒNG 10 GIÂY
            if (q.Contains("khóa") || q.Contains("smartlock") || q.Contains("mã pin") || q.Contains("nhận phòng") || q.Contains("check in") || q.Contains("check-in") || q.Contains("check out") || q.Contains("check-out") || q.Contains("mở cửa") || q.Contains("lễ tân"))
            {
                return new AiChatMessage
                {
                    Sender = "Assistant",
                    Content = @"🔑 **Quy Trình Nhận Phòng Tự Động 10 Giây (SmartLock IoT Cloud):**
• **Giờ quy định:** Giờ nhận phòng (Check-in) là **14:00** | Giờ trả phòng (Check-out) là **12:00** trưa hôm sau.
• **Không cần chờ lễ tân:** Ngay sau khi quý khách chuyển cọc 50% qua VietQR, hệ thống IoT Cloud sẽ tự động sinh một **Mã PIN 6 số bí mật** gắn liền với số điện thoại của quý khách.
• **Cách mở cửa khi đến nơi:**
  1. Chạm nhẹ vào bàn phím cảm ứng trên cửa phòng để đánh thức khóa.
  2. Nhập mã PIN 6 số và nhấn dấu `#`.
  3. Khóa sẽ bật chốt xanh trong 0.5 giây và quý khách tự do vào phòng nghỉ ngơi 24/7!
• Quý khách có thể tra cứu lại mã PIN bất cứ lúc nào bằng mã đơn hoặc số điện thoại.",
                    ActionUrl = "/lookup",
                    ActionLabel = "🔑 Tra Cứu Mã Khóa SmartLock",
                    Category = "SmartLock"
                };
            }

            // 3. NHÓM THANH TOÁN, VIETQR, SỐ TÀI KHOẢN, TIỀN CỌC
            if (q.Contains("cọc") || q.Contains("thanh toán") || q.Contains("vietqr") || q.Contains("ngân hàng") || q.Contains("stk") || q.Contains("số tài khoản") || q.Contains("chuyển khoản") || q.Contains("tiền"))
            {
                return new AiChatMessage
                {
                    Sender = "Assistant",
                    Content = @"💳 **Chính Sách Đặt Cọc & Thông Tin Ngân Hàng Thụ Hưởng:**
• **Mức cọc:** Quý khách chuyển cọc **50% tổng tiền phòng** để hệ thống khóa phòng trên lịch chống trùng lặp (Overbooking). 50% còn lại thanh toán khi nhận phòng.
• **Thông tin tài khoản chính thức:**
  - Ngân hàng: **TPBank (Ngân hàng TMCP Tiên Phong)**
  - Số tài khoản: **10787723714**
  - Chủ tài khoản: **TRẦN VIẾT MẠNH CƯỜNG**
  - Cú pháp chuyển khoản: `KLE [Mã đơn đặt phòng]` *(Ví dụ: `KLE BK1234`)*
• **Mã VietQR tự động:** Khi đặt phòng, màn hình sẽ hiển thị sẵn mã VietQR có đúng số tiền cọc và nội dung. Quý khách chỉ cần mở app ngân hàng bất kỳ để quét mã trong 10 giây!",
                    ActionUrl = "/#rooms",
                    ActionLabel = "👉 Đặt Phòng & Nhận Mã VietQR",
                    Category = "Policy"
                };
            }

            // 4. NHÓM HỦY PHÒNG & HOÀN TIỀN CỌC (LUẬT 48 GIỜ)
            if (q.Contains("hủy") || q.Contains("hoàn tiền") || q.Contains("đổi ngày") || q.Contains("trả cọc") || q.Contains("chính sách hủy") || q.Contains("luật 48"))
            {
                return new AiChatMessage
                {
                    Sender = "Assistant",
                    Content = @"🛡️ **Quy Chế Hủy Phòng Minh Bạch (Luật 48 Giờ của Kim Long EcoStay):**
• **Hủy trước 48 giờ** *(tính từ mốc 14:00 ngày nhận phòng)*:
  - Quý khách được **HOÀN 100% TIỀN CỌC** mà không chịu bất kỳ chi phí phạt nào.
  - Tiền cọc được hệ thống hoàn về số tài khoản của quý khách trong vòng 24 giờ làm việc.
• **Hủy trong vòng dưới 48 giờ:**
  - Tiền cọc không được hoàn lại nhằm hỗ trợ nhà vườn bù đắp chi phí chuẩn bị buồng phòng sinh thái và hoa quả tươi đón tiếp.
• **Thao tác hủy:** Quý khách chỉ cần vào mục **Tra Cứu Mã Khóa / Đơn Phòng**, nhập số điện thoại hoặc mã đơn và nhấn nút *""Yêu Cầu Hủy Phòng""*.",
                    ActionUrl = "/lookup",
                    ActionLabel = "🔍 Tra Cứu & Quản Lý Đơn Phòng",
                    Category = "Policy"
                };
            }

            // 5. NHÓM XE ĐẠP, TOUR DI SẢN, GPS AUDIO GUIDE
            if (q.Contains("xe đạp") || q.Contains("eco-bike") || q.Contains("gps") || q.Contains("audio") || q.Contains("tour") || q.Contains("thiên mụ") || q.Contains("đại nội") || q.Contains("chùa") || q.Contains("bản đồ") || q.Contains("di tích") || q.Contains("làng nghề"))
            {
                return new AiChatMessage
                {
                    Sender = "Assistant",
                    Content = @"🚴 **Trải Nghiệm Du Lịch Di Sản Bằng Xe Đạp Trợ Lực Điện (Eco-Bike & GPS Audio Guide):**
• **Miễn phí 100% cho khách lưu trú:** Mỗi phòng được mượn 02 xe đạp trợ lực điện sạch có gắn giá kẹp điện thoại và sạc pin năng lượng mặt trời.
• **Công nghệ GPS Audio Guide:** 
  - Tích hợp sẵn bản đồ số hơn 30 di tích Cố đô và làng nghề truyền thống Kim Long.
  - Khi xe đạp tiến đến gần di tích (như Chùa Thiên Mụ, Nhà vườn An Hiên, Bến thuyền Kim Long), hệ thống tự động phát bài thuyết minh lịch sử trầm ấm bằng giọng Huế truyền cảm (hỗ trợ 3 ngôn ngữ: Tiếng Việt, Tiếng Anh, Tiếng Pháp).
• **Cự ly di chuyển từ Kim Long EcoStay:**
  - Chùa Thiên Mụ: 1.2 km (5 phút đạp xe)
  - Nhà Vườn Cổ An Hiên: 600 m (3 phút)
  - Cầu Dã Viên & Bờ bắc sông Hương: 800 m (4 phút)
  - Đại Nội Huế (Kinh Thành): 3.5 km (12 phút)",
                    ActionUrl = "/map",
                    ActionLabel = "🗺️ Mở Bản Đồ Di Tích & Làng Nghề",
                    Category = "Heritage"
                };
            }

            // 6. NHÓM ẨM THỰC, ĂN UỐNG, BÁNH ƯỚT KIM LONG
            if (q.Contains("ăn") || q.Contains("uống") || q.Contains("ẩm thực") || q.Contains("bánh ướt") || q.Contains("thịt nướng") || q.Contains("đặc sản") || q.Contains("trà sen") || q.Contains("bún bò") || q.Contains("chè"))
            {
                return new AiChatMessage
                {
                    Sender = "Assistant",
                    Content = @"🍜 **Tinh Hoa Ẩm Thực Nhà Vườn Kim Long:**
• **Bánh ướt thịt nướng than hoa Kim Long (Giao tận phòng 15 phút):** 
  - Món ăn trứ danh nức tiếng Cố đô từ thời chúa Nguyễn! Thịt ba chỉ ướp mật ong, tiêu rừng nướng than hoa thơm lừng cuộn trong lớp bánh ướt mỏng mềm như lụa, chấm cùng nước lèo tương đậu mè béo ngậy.
  - Homestay liên kết trực tiếp với các nghệ nhân làng bánh ướt Kim Long để phục vụ bữa sáng hoặc ăn xế nóng hổi tận phòng.
• **Trà sen Cung đình & Mứt gừng Huế:** Đón tiếp khách lúc check-in bằng trà ủ trong búp sen tươi hồ Tịnh Tâm ấm nồng.
• **Gợi ý ẩm thực quanh homestay:** Bánh ướt Huyền Anh Kim Long (cách 400m), Quán bún bò bà Rơi (cách 900m), Bánh ép Cầu Dã Viên và Chè hẻm Cố đô.",
                    ActionUrl = "/#smart-tour",
                    ActionLabel = "🍱 Khám Phá Văn Hóa Ẩm Thực",
                    Category = "Cuisine"
                };
            }

            // 7. NHÓM QUẢN TRỊ VIÊN & CHỦ HOMESTAY / LỄ TÂN
            if (q.Contains("quản trị") || q.Contains("admin") || q.Contains("lễ tân") || q.Contains("host") || q.Contains("dọn phòng") || q.Contains("sop") || q.Contains("công suất") || q.Contains("báo cáo"))
            {
                return new AiChatMessage
                {
                    Sender = "Assistant",
                    Content = @"💼 **Hỗ Trợ Nghiệp Vụ Vận Hành Cho Chủ Nhà & Lễ Tân (Host / Staff):**
• **Phân hệ Quản trị buồng phòng:** Truy cập `/admin` bằng tài khoản `letan` / `123456` hoặc `admin` / `123456`.
• **Công suất buồng phòng (Occupancy Rate):** Xem biểu đồ tỉ lệ lấp đầy phòng theo tháng và doanh thu thực nhận tại `/admin/occupancy-report`.
• **Kiểm định vệ sinh SOP 8 bước:** Giám sát trạng thái dọn buồng phòng sinh thái (Cleaned, Inspected, Occupied) tại `/admin/room-inspection`.
• **Quản lý khóa SmartLock IoT:** Cấp mã PIN thủ công từ xa, theo dõi mức pin chốt khóa tại `/admin/smartlock`.
• **SmartPaste:** Trích xuất nhanh dữ liệu tin nhắn Zalo của khách thành đơn phòng trong 1 giây tại `/admin/orders`.",
                    ActionUrl = "/admin",
                    ActionLabel = "🔐 Vào Phân Hệ Quản Trị Admin",
                    Category = "Admin"
                };
            }

            // 8. NHÓM CHÀO HỎI & CÂU HỎI CHUNG
            return new AiChatMessage
            {
                Sender = "Assistant",
                Content = $@"Dạ, em chào quý khách! Em là **Trợ lý AI Du Lịch & Buồng Phòng của Kim Long EcoStay** 🌿

Em có thể hỗ trợ quý khách tức thì các thông tin sau:
• 🏡 **Tư vấn chọn phòng** view vườn bưởi thanh trà hoặc ngắm hoàng hôn sông Hương.
• 🔑 **Hướng dẫn nhận phòng tự động 10 giây** bằng mã PIN khóa thông minh SmartLock IoT.
• 💳 **Cách đặt cọc 50% qua VietQR** (TPBank: `10787723714` - `Trần Viết Mạnh Cường`) và luật hoàn tiền trước 48 giờ.
• 🚴 **Tour xe đạp trợ lực điện Eco-Bike miễn phí** kèm thuyết minh GPS Audio Guide đến Chùa Thiên Mụ & Đại Nội.
• 🍜 **Thưởng thức bánh ướt thịt nướng Kim Long** giao nóng hổi tận phòng.

Quý khách cần em tư vấn thêm về nội dung nào ạ?",
                ActionUrl = "/#rooms",
                ActionLabel = "🌿 Xem Danh Sách Buồng Phòng",
                Category = "General"
            };
        }

        private async Task<string> CallGeminiAsync(string question, List<AiChatMessage> history)
        {
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_config.Model}:generateContent?key={_config.ApiKey}";

            string systemPrompt = @"Bạn là Trợ lý AI du lịch và quản lý buồng phòng thông minh của 'Kim Long EcoStay' - một mô hình Homestay Nhà Vườn Sinh Thái kết hợp Du lịch Di sản Cố Đô Huế tại phường Kim Long, TP. Huế.
Hãy trả lời khách bằng văn phong nhã nhặn, hiếu khách, chân thành đặc trưng xứ Huế ('Dạ', 'thưa quý khách').
Các thông tin chính xác cốt lõi cần nhớ:
1. Giá phòng: Eco-Nest Ban Mai (390k), Eco-Nest Hoàng Hôn (450k), Garden Suite Cố Đô (680k), Heritage Studio (580k), Family Eco-Villa (1.450k).
2. Khóa thông minh SmartLock: Cấp mã PIN 6 số tự động sau khi cọc VietQR, mở cửa 24/7 không cần gặp lễ tân.
3. Thanh toán: Cọc 50% VietQR qua TPBank - STK 10787723714 (Trần Viết Mạnh Cường).
4. Chính sách hủy cọc: Hủy trước 48 giờ hoàn 100% tiền cọc trong 24 giờ làm việc. Hủy sau 48h mất tiền cọc.
5. Đặc quyền miễn phí: 2 xe đạp trợ lực điện Eco-Bike kèm GPS Audio Guide 3 ngôn ngữ, ngâm chân thảo mộc hoa bưởi, trà sen Cung đình đón tiếp.
Ghi chú bổ sung từ chủ nhà: " + _config.CustomNotes;

            var contents = new List<object>
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = systemPrompt + "\n\nCâu hỏi của khách: " + question } }
                }
            };

            var payload = new { contents = contents };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            if (!response.IsSuccessStatusCode) return string.Empty;

            var resString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resString);
            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var first = candidates[0];
                if (first.TryGetProperty("content", out var cObj) && cObj.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    return parts[0].GetProperty("text").GetString() ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private async Task<string> CallOpenAiAsync(string question, List<AiChatMessage> history)
        {
            var endpoint = "https://api.openai.com/v1/chat/completions";
            string model = string.IsNullOrWhiteSpace(_config.Model) ? "gpt-4o-mini" : _config.Model;

            string systemPrompt = @"Bạn là Trợ lý AI du lịch của Kim Long EcoStay tại phường Kim Long, TP. Huế. 
Giá phòng: Ban Mai 390k, Hoàng Hôn 450k, Garden Suite 680k, Family Villa 1450k. 
Khóa SmartLock cấp mã PIN 6 số qua VietQR cọc 50% vào TPBank 10787723714 (Trần Viết Mạnh Cường). Hủy trước 48h hoàn 100% cọc. Miễn phí xe đạp trợ lực điện Eco-Bike & GPS Audio Guide. 
Ghi chú chủ nhà: " + _config.CustomNotes;

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = question }
            };

            var payload = new
            {
                model = model,
                messages = messages,
                max_tokens = 800,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.ApiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return string.Empty;

            var resString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resString);
            var root = doc.RootElement;
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                return choices[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        private string? DetermineActionUrl(string question)
        {
            string q = question.ToLowerInvariant();
            if (q.Contains("phòng") || q.Contains("đặt") || q.Contains("giá")) return "/#rooms";
            if (q.Contains("khóa") || q.Contains("smartlock") || q.Contains("pin") || q.Contains("tra cứu")) return "/lookup";
            if (q.Contains("bản đồ") || q.Contains("di tích") || q.Contains("thiên mụ") || q.Contains("xe đạp")) return "/map";
            if (q.Contains("quản trị") || q.Contains("admin") || q.Contains("công suất")) return "/admin";
            return null;
        }

        private string? DetermineActionLabel(string question)
        {
            string q = question.ToLowerInvariant();
            if (q.Contains("phòng") || q.Contains("đặt") || q.Contains("giá")) return "🌿 Xem Danh Sách Phòng";
            if (q.Contains("khóa") || q.Contains("smartlock") || q.Contains("pin") || q.Contains("tra cứu")) return "🔑 Tra Cứu Mã Khóa";
            if (q.Contains("bản đồ") || q.Contains("di tích") || q.Contains("thiên mụ") || q.Contains("xe đạp")) return "🗺️ Mở Bản Đồ Di Sản";
            if (q.Contains("quản trị") || q.Contains("admin") || q.Contains("công suất")) return "🔐 Bảng Điều Khiển Admin";
            return null;
        }
    }
}
