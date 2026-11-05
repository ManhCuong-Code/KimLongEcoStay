using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

namespace KimLongEcoStay.Web.Services
{
    public class AiChatRequestDto
    {
        public string? ConversationId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? Role { get; set; }
        public string? UserName { get; set; }
        public string? ActorName { get; set; }
        public string? Provider { get; set; }
        public string? Model { get; set; }
        public List<AiTurnDto>? History { get; set; }
    }

    public class AiTurnDto
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }

    public class AiAssistantGatewayService
    {
        private readonly TwoWaySqlSyncService _sqlSync;
        private readonly HttpClient _httpClient;
        private static readonly ConcurrentDictionary<string, List<DateTime>> _rateLimitBuckets = new();

        private string _activeProvider;
        private string _activeModel;
        private readonly int _maxTokens;
        private readonly int _rateLimitPerMinute;

        public AiAssistantGatewayService(TwoWaySqlSyncService sqlSync)
        {
            _sqlSync = sqlSync;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(22) };

            var openAiKey = GetOpenAiApiKey();
            var geminiKey = GetGeminiApiKey();

            _activeProvider = !string.IsNullOrWhiteSpace(openAiKey)
                ? "OpenAI"
                : (!string.IsNullOrWhiteSpace(geminiKey) ? "Gemini" : "LocalSqlEngine");

            _activeModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
            _maxTokens = int.TryParse(Environment.GetEnvironmentVariable("AI_MAX_TOKENS"), out var mt) ? mt : 900;
            _rateLimitPerMinute = int.TryParse(Environment.GetEnvironmentVariable("AI_RATE_LIMIT_PER_MINUTE"), out var rl) ? rl : 25;
        }

        private static string GetOpenAiApiKey() =>
            (Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty).Trim();

        private static string GetGeminiApiKey() =>
            (Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty).Trim();

        public object GetStatus()
        {
            var openAiKey = GetOpenAiApiKey();
            var geminiKey = GetGeminiApiKey();
            bool hasOpenAi = !string.IsNullOrWhiteSpace(openAiKey);
            bool hasGemini = !string.IsNullOrWhiteSpace(geminiKey);

            return new
            {
                isEnabled = true,
                provider = hasOpenAi ? "OpenAI" : _activeProvider,
                model = _activeModel,
                openAiConfigured = hasOpenAi,
                hasOpenAiKey = hasOpenAi,
                hasGeminiKey = hasGemini,
                maskedApiKey = hasOpenAi ? "sk-proj-•••••••••••••••••••• (Loaded from .env)" : "Not configured",
                sqlGroundingEnabled = true,
                rbacProtectionEnabled = true,
                rateLimitPerMinute = _rateLimitPerMinute,
                maxTokens = _maxTokens,
                availableModels = new[]
                {
                    new { id = "gpt-4o-mini", provider = "OpenAI", label = "OpenAI GPT-4o Mini (Khuyên dùng • Nhanh & Chính xác)" },
                    new { id = "gpt-4o", provider = "OpenAI", label = "OpenAI GPT-4o (Phân tích chuyên sâu)" },
                    new { id = "local-sql", provider = "LocalSqlEngine", label = "Động Cơ Tri Thức SQL Nội Bộ (Offline Fallback)" }
                }
            };
        }

        private bool CheckRateLimit(string clientKey)
        {
            var now = DateTime.UtcNow;
            var list = _rateLimitBuckets.GetOrAdd(clientKey, _ => new List<DateTime>());
            lock (list)
            {
                list.RemoveAll(t => (now - t).TotalMinutes >= 1.0);
                if (list.Count >= _rateLimitPerMinute)
                    return false;
                list.Add(now);
                return true;
            }
        }

        public async Task<object> ProcessChatAsync(AiChatRequestDto request, string clientIp)
        {
            var sw = Stopwatch.StartNew();
            string question = (!string.IsNullOrWhiteSpace(request.Question) ? request.Question : (request.Message ?? string.Empty)).Trim();
            string convId = string.IsNullOrWhiteSpace(request.ConversationId) ? $"conv-{DateTime.UtcNow:yyyyMMddHHmmss}" : request.ConversationId!;
            string normRole = NormalizeRole(request.Role);
            string userName = !string.IsNullOrWhiteSpace(request.UserName)
                ? request.UserName!
                : (!string.IsNullOrWhiteSpace(request.ActorName) ? request.ActorName! : GetDefaultRoleTitle(normRole));

            if (string.IsNullOrWhiteSpace(question))
            {
                return new
                {
                    success = false,
                    conversationId = convId,
                    error = "Vui lòng nhập nội dung câu hỏi cho Trợ lý AI."
                };
            }

            if (!CheckRateLimit($"{clientIp}:{normRole}"))
            {
                return new
                {
                    success = false,
                    conversationId = convId,
                    error = $"Quý khách đã gửi quá giới hạn {_rateLimitPerMinute} câu hỏi/phút. Vui lòng đợi vài giây rồi thử lại ạ."
                };
            }

            // Kiểm tra bảo mật tuyệt đối: Chặn truy vấn yêu cầu tiết lộ mã PIN SmartLock, mật khẩu, API Key hoặc Connection String
            string lowerQ = question.ToLowerInvariant();
            if ((lowerQ.Contains("mã pin") || lowerQ.Contains("pin 6 số") || lowerQ.Contains("pin của phòng") || lowerQ.Contains("cho biết pin") || lowerQ.Contains("xem mã mở cửa") || lowerQ.Contains("xem mã pin")) &&
                (lowerQ.Contains("số mấy") || lowerQ.Contains("là gì") || lowerQ.Contains("hiển thị") || lowerQ.Contains("cho tôi xem") || lowerQ.Contains("tất cả") || lowerQ.Contains("r101") || lowerQ.Contains("r102") || lowerQ.Contains("r201") || lowerQ.Contains("r202") || lowerQ.Contains("r203")))
            {
                string secMsg = @"🔒 **Chính Sách Bảo Mật Khóa Thông Minh SmartLock IoT:**
Theo quy định an ninh của **Kim Long EcoStay**, mã PIN 6 số mở cửa phòng được mã hóa một chiều (`SHA-256`) trong SQL Server và luôn hiển thị ở chế độ ẩn (`••••••`) trên mọi màn hình quản trị cũng như đối với Trợ lý AI.

• **Đối với Khách lưu trú:** Mã PIN chỉ được gửi tự động về Email / SMS chính chủ ngay sau khi đối soát cọc 50% thành công.
• **Đối với Lễ tân & Quản trị viên:** Nếu khách quên mã PIN, nhân viên có thẩm quyền vui lòng vào phân hệ **Vận Hành Dự Án / Phòng → Tra Cứu Khóa & SmartLock**, nhấn nút **Cấp Lại Mã PIN** và nhập lý do để hệ thống tạo mã mới kèm ghi nhận **Audit Log** trong SQL Server.";

                await LogAiAuditAsync(convId, normRole, userName, "SecurityGuard", _activeModel, question, "SECURITY_PIN_MASK_POLICY", 120, (int)sw.ElapsedMilliseconds, "BLOCKED_SENSITIVE");
                return new
                {
                    success = true,
                    conversationId = convId,
                    answer = secMsg,
                    reply = secMsg,
                    provider = "SecurityGuard • RBAC",
                    model = _activeModel,
                    role = normRole,
                    sqlGrounded = true,
                    toolsUsed = new[] { "SECURITY_PIN_MASK_POLICY" },
                    responseTimeMs = (int)sw.ElapsedMilliseconds,
                    latencyMs = (int)sw.ElapsedMilliseconds,
                    timestamp = DateTime.UtcNow
                };
            }

            if (lowerQ.Contains("api key") || lowerQ.Contains("connection string") || lowerQ.Contains("mật khẩu sql") || lowerQ.Contains("passwordhash"))
            {
                string secMsg = @"🛡️ **Chính Sách Bảo Vệ Thông Tin Bí Mật (Zero-Secret Exposure):**
Trợ lý AI tuyệt đối không truy xuất và không hiển thị **API Key**, **Mật khẩu tài khoản**, hoặc **Chuỗi kết nối SQL (Connection String)**. Toàn bộ khóa cấu hình được bảo vệ trong biến môi trường máy chủ (`.env`).";

                await LogAiAuditAsync(convId, normRole, userName, "SecurityGuard", _activeModel, question, "ZERO_SECRET_GUARD", 90, (int)sw.ElapsedMilliseconds, "BLOCKED_SENSITIVE");
                return new
                {
                    success = true,
                    conversationId = convId,
                    answer = secMsg,
                    reply = secMsg,
                    provider = "SecurityGuard • RBAC",
                    model = _activeModel,
                    role = normRole,
                    sqlGrounded = true,
                    toolsUsed = new[] { "ZERO_SECRET_GUARD" },
                    responseTimeMs = (int)sw.ElapsedMilliseconds,
                    latencyMs = (int)sw.ElapsedMilliseconds,
                    timestamp = DateTime.UtcNow
                };
            }

            // Kiểm tra quyền RBAC nếu khách hàng hỏi dữ liệu doanh thu / lương nội bộ
            if ((normRole == "GUEST" || normRole == "RECEPTIONIST" || normRole == "HR_OPS") &&
                (lowerQ.Contains("doanh thu") || lowerQ.Contains("lợi nhuận") || lowerQ.Contains("bảng lương") || lowerQ.Contains("chi phí vận hành") || lowerQ.Contains("ebitda")))
            {
                if (normRole != "ADMIN" && normRole != "ACCOUNTANT")
                {
                    string rbacMsg = $@"🔒 **Giới Hạn Phân Quyền Truy Cập Dữ Liệu (RBAC):**
Hiện tại bạn đang truy cập với vai trò **{GetDefaultRoleTitle(normRole)}**. Các số liệu về **Doanh thu, Chi phí vận hành, Lợi nhuận ròng và Bảng lương** là dữ liệu tài chính mật trong SQL Server, chỉ được phép truy xuất bởi tài khoản **👑 Admin (CEO)** hoặc **💰 Kế Toán Trưởng (CFO)**.

Bạn có thể đăng nhập bằng tài khoản có thẩm quyền hoặc hỏi em về **Tình trạng phòng trống, Danh mục 30 đặc sản Huế, Dịch vụ Trà Cung Đình và Bản đồ đối tác Kim Long** ạ! 🌿";

                    await LogAiAuditAsync(convId, normRole, userName, "RBACGuard", _activeModel, question, "RBAC_FINANCE_RESTRICTED", 110, (int)sw.ElapsedMilliseconds, "RBAC_DENIED");
                    return new
                    {
                        success = true,
                        conversationId = convId,
                        answer = rbacMsg,
                        reply = rbacMsg,
                        provider = "RBAC Guard",
                        model = _activeModel,
                        role = normRole,
                        sqlGrounded = true,
                        toolsUsed = new[] { "RBAC_FINANCE_RESTRICTED" },
                        responseTimeMs = (int)sw.ElapsedMilliseconds,
                        latencyMs = (int)sw.ElapsedMilliseconds,
                        timestamp = DateTime.UtcNow
                    };
                }
            }

            // 1. Truy vấn dữ liệu thời gian thực từ SQL Server theo đúng quyền RBAC
            var (sqlContextText, toolsUsed, localFallbackAnswer) = await BuildLiveSqlContextAsync(normRole, question);

            // 2. Gọi OpenAI API (nếu có OPENAI_API_KEY và không ép dùng local)
            string openAiKey = GetOpenAiApiKey();
            string requestedProvider = (request.Provider ?? _activeProvider).Trim();
            string selectedModel = !string.IsNullOrWhiteSpace(request.Model) ? request.Model!.Trim() : _activeModel;

            if (!string.IsNullOrWhiteSpace(openAiKey) && !requestedProvider.Equals("LocalSqlEngine", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var (cloudAnswer, estimatedTokens) = await CallOpenAiWithSqlContextAsync(
                        openAiKey, selectedModel, normRole, userName, sqlContextText, question, request.History);

                    if (!string.IsNullOrWhiteSpace(cloudAnswer))
                    {
                        sw.Stop();
                        await LogAiAuditAsync(convId, normRole, userName, "OpenAI", selectedModel, question, string.Join(",", toolsUsed), estimatedTokens, (int)sw.ElapsedMilliseconds, "SUCCESS");

                        return new
                        {
                            success = true,
                            conversationId = convId,
                            answer = cloudAnswer,
                            reply = cloudAnswer,
                            provider = "OpenAI",
                            model = selectedModel,
                            role = normRole,
                            sqlGrounded = true,
                            toolsUsed,
                            tokensEstimated = estimatedTokens,
                            responseTimeMs = (int)sw.ElapsedMilliseconds,
                            latencyMs = (int)sw.ElapsedMilliseconds,
                            timestamp = DateTime.UtcNow
                        };
                    }
                }
                catch
                {
                    // Tự động chuyển sang câu trả lời kết xuất trực tiếp từ SQL Server nếu mạng gián đoạn
                }
            }

            sw.Stop();
            await LogAiAuditAsync(convId, normRole, userName, "SqlSmartEngine", "sql-live-engine", question, string.Join(",", toolsUsed), 250, (int)sw.ElapsedMilliseconds, "FALLBACK_SQL_SUCCESS");

            return new
            {
                success = true,
                conversationId = convId,
                answer = localFallbackAnswer,
                reply = localFallbackAnswer,
                provider = "SQL Smart Engine",
                model = "sql-live-engine",
                role = normRole,
                sqlGrounded = true,
                toolsUsed,
                tokensEstimated = 250,
                responseTimeMs = (int)sw.ElapsedMilliseconds,
                latencyMs = (int)sw.ElapsedMilliseconds,
                timestamp = DateTime.UtcNow
            };
        }

        private async Task<(string Answer, int Tokens)> CallOpenAiWithSqlContextAsync(
            string apiKey,
            string model,
            string normRole,
            string userName,
            string sqlContext,
            string question,
            List<AiTurnDto>? history)
        {
            string endpoint = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1/chat/completions";
            if (string.IsNullOrWhiteSpace(model) || model == "local-sql")
                model = "gpt-4o-mini";

            string systemPrompt = $@"Bạn là Trợ lý AI Thông Minh của hệ thống 'Kim Long EcoStay — Nghỉ Dưỡng Nhà Vườn Sinh Thái & Di Sản Cố Đô Huế' tại Phường Kim Long, TP. Huế.
Người dùng hiện tại: {userName} | Vai trò RBAC: {normRole} ({GetDefaultRoleTitle(normRole)}).

NGUYÊN TẮC BẮT BUỘC:
1. Trả lời bằng tiếng Việt lịch sự, chuẩn mực, ấm áp chất Huế ('Dạ', 'thưa quý khách' hoặc 'thưa anh/chị' khi hỗ trợ nội bộ), trình bày Markdown rõ ràng, dùng bảng Markdown hoặc danh sách bullet khi liệt kê phòng, sản phẩm, báo cáo.
2. LUÔN LUÔN sử dụng số liệu thực tế từ CSDL SQL Server (`KimLongEcoStayDB`) được cung cấp ngay dưới đây để trả lời chính xác từng con số (số phòng trống, giá phòng, danh sách 30 đặc sản, tồn kho, đơn đặt phòng, đơn giao tận phòng, và báo cáo doanh thu/nhân sự nếu vai trò được phép).
3. TUYỆT ĐỐI KHÔNG bịa đặt dữ liệu ngoài hệ thống, KHÔNG bao giờ tiết lộ mã PIN SmartLock dạng số thực (chỉ ghi `••••••`), mật khẩu hay khóa API.

=== DỮ LIỆU THỜI GIAN THỰC TỪ SQL SERVER (KimLongEcoStayDB) ===
{sqlContext}
===============================================================";

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            if (history != null && history.Count > 0)
            {
                foreach (var turn in history.TakeLast(6))
                {
                    if (string.IsNullOrWhiteSpace(turn.Content)) continue;
                    string r = turn.Role?.ToLowerInvariant() == "assistant" ? "assistant" : "user";
                    messages.Add(new { role = r, content = turn.Content });
                }
            }

            messages.Add(new { role = "user", content = question });

            var payload = new
            {
                model,
                messages,
                max_tokens = _maxTokens,
                temperature = 0.6
            };

            string json = JsonSerializer.Serialize(payload);
            using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _httpClient.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
                return (string.Empty, 0);

            string body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            string content = string.Empty;
            int totalTokens = 350;

            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
            }
            if (root.TryGetProperty("usage", out var usage) && usage.TryGetProperty("total_tokens", out var tt))
            {
                totalTokens = tt.GetInt32();
            }

            return (content, totalTokens);
        }

        private async Task<(string ContextText, List<string> ToolsUsed, string LocalFallback)> BuildLiveSqlContextAsync(string normRole, string question)
        {
            var sb = new StringBuilder();
            var tools = new List<string> { "SQL_ROOMS_QUERY", "SQL_PRODUCTS_QUERY", "SQL_TEA_PARTNERS_QUERY" };

            try
            {
                await using var conn = new SqlConnection(_sqlSync.ConnectionString);
                await conn.OpenAsync();

                // 1. Dữ liệu 5 Buồng Phòng từ SQL Server
                var rooms = (await conn.QueryAsync<dynamic>(@"
                    SELECT r.RoomNumber, r.Status, r.GuestName, r.BookingCode,
                           rt.TypeName, rt.BasePrice, rt.MaxGuests
                    FROM dbo.Rooms r
                    JOIN dbo.RoomTypes rt ON r.RoomTypeId = rt.Id
                    WHERE r.IsActive = 1
                    ORDER BY r.Id ASC;")).ToList();

                int readyRoomsCount = rooms.Count(r => (string)r.Status == "AVAILABLE" || (string)r.Status == "CLEANED_READY");
                sb.AppendLine($"[1. TÌNH TRẠNG 05 BUỒNG PHÒNG TRONG SQL SERVER - Số phòng sẵn sàng đón khách ngay: {readyRoomsCount}/5 phòng]");
                foreach (var r in rooms)
                {
                    string guestInfo = (normRole != "GUEST" && !string.IsNullOrWhiteSpace((string?)r.GuestName))
                        ? $" | Khách: {r.GuestName} ({r.BookingCode})"
                        : "";
                    sb.AppendLine($"- {r.RoomNumber} ({r.TypeName}): Giá {((decimal)r.BasePrice):N0} VNĐ/đêm | Tối đa {r.MaxGuests} khách | Trạng thái SQL: {r.Status}{guestInfo}");
                }

                // 2. Dữ liệu 30 Sản Phẩm & Đặc Sản từ SQL Server
                var products = (await conn.QueryAsync<dynamic>(@"
                    SELECT ProductCode, Name, Category, Price, Unit, StockQuantity, PartnerName
                    FROM dbo.SpecialtyProducts
                    WHERE IsActive = 1
                    ORDER BY ProductCode ASC;")).ToList();

                int totalStock = products.Sum(p => (int)p.StockQuantity);
                var lowStock = products.Where(p => (int)p.StockQuantity <= 20).ToList();
                sb.AppendLine($"\n[2. DANH MỤC {products.Count} SẢN PHẨM & ĐẶC SẢN TRONG SQL SERVER - Tổng tồn kho: {totalStock} đơn vị]");
                foreach (var p in products)
                {
                    sb.AppendLine($"- {p.ProductCode}: {p.Name} ({p.Category}) | Giá: {((decimal)p.Price):N0}đ/{p.Unit} | Tồn kho: {p.StockQuantity} | Đối tác: {p.PartnerName}");
                }
                if (lowStock.Count > 0)
                {
                    sb.AppendLine($"* Sản phẩm sắp hết hàng (Tồn <= 20): {string.Join(", ", lowStock.Select(x => $"{x.Name} ({x.StockQuantity} {x.Unit})"))}");
                }

                // 3. Dịch vụ Trà Cung Đình & Đối tác
                var teas = (await conn.QueryAsync<dynamic>("SELECT ServiceCode, Name, Price, PriceUnit, AvailableSlots, ServingSchedule FROM dbo.TeaServices WHERE IsActive = 1;")).ToList();
                sb.AppendLine($"\n[3. DỊCH VỤ TRÀ CUNG ĐÌNH HUẾ ({teas.Count} gói)]");
                foreach (var t in teas)
                {
                    sb.AppendLine($"- {t.ServiceCode}: {t.Name} | Giá: {((decimal)t.Price):N0}đ/{t.PriceUnit} | Còn {t.AvailableSlots} chỗ | Khung giờ: {t.ServingSchedule}");
                }

                // 4. Dữ liệu Vận Hành Đơn Đặt Phòng & Giao Tận Phòng (Cho Staff / Admin / Accountant / HR)
                decimal totalBookingRev = 0;
                decimal confirmedDeposit = 0;
                int totalBookingsCount = 0;
                int pendingBookingsCount = 0;
                int activeDeliveriesCount = 0;

                if (normRole != "GUEST")
                {
                    tools.Add("SQL_BOOKINGS_DELIVERIES_QUERY");
                    var bookings = (await conn.QueryAsync<dynamic>(@"
                        SELECT BookingCode, GuestName, RoomName, CheckInDate, CheckOutDate, TotalAmount, DepositAmount, Status, PaymentStatus
                        FROM dbo.Bookings ORDER BY Id DESC;")).ToList();

                    totalBookingsCount = bookings.Count;
                    pendingBookingsCount = bookings.Count(b => (string)b.PaymentStatus == "WAITING_RECONCILIATION" || (string)b.Status == "PENDING_DEPOSIT");
                    totalBookingRev = bookings.Sum(b => (decimal)b.TotalAmount);
                    confirmedDeposit = bookings.Where(b => (string)b.PaymentStatus != "WAITING_RECONCILIATION").Sum(b => (decimal)b.DepositAmount);

                    sb.AppendLine($"\n[4. ĐƠN ĐẶT PHÒNG ({totalBookingsCount} đơn, Chờ đối soát cọc: {pendingBookingsCount} đơn)]");
                    foreach (var b in bookings.Take(8))
                    {
                        sb.AppendLine($"- Mã đơn {b.BookingCode}: Khách {b.GuestName} | Phòng: {b.RoomName} | {((DateTime)b.CheckInDate):dd/MM/yyyy} -> {((DateTime)b.CheckOutDate):dd/MM/yyyy} | Tổng: {((decimal)b.TotalAmount):N0}đ (Cọc: {((decimal)b.DepositAmount):N0}đ) | Trạng thái: {b.PaymentStatus}");
                    }

                    var deliveries = (await conn.QueryAsync<dynamic>(@"
                        SELECT OrderCode, RoomNumber, CustomerName, Total, Status, DeliveryTime
                        FROM dbo.RoomDeliveries ORDER BY Id DESC;")).ToList();
                    activeDeliveriesCount = deliveries.Count(d => (string)d.Status != "DELIVERED" && (string)d.Status != "CANCELLED");

                    sb.AppendLine($"\n[5. ĐƠN GIAO MÓN TẬN PHÒNG ({deliveries.Count} đơn, Đang xử lý/giao: {activeDeliveriesCount} đơn)]");
                    foreach (var d in deliveries.Take(6))
                    {
                        sb.AppendLine($"- {d.OrderCode}: Phòng {d.RoomNumber} (Khách {d.CustomerName}) | Tổng: {((decimal)d.Total):N0}đ | Trạng thái: {d.Status} | Giờ giao: {d.DeliveryTime}");
                    }
                }

                // 5. Dữ liệu Tài Chính & Bảng Lương (Chỉ cấp cho ADMIN và ACCOUNTANT)
                decimal finIncome = 0;
                decimal finExpense = 0;
                if (normRole == "ADMIN" || normRole == "ACCOUNTANT")
                {
                    tools.Add("SQL_FINANCE_PAYROLL_QUERY");
                    finIncome = await conn.ExecuteScalarAsync<decimal?>("SELECT SUM(Amount) FROM dbo.FinancialTransactions WHERE Type = 'INCOME';") ?? 0;
                    finExpense = await conn.ExecuteScalarAsync<decimal?>("SELECT SUM(Amount) FROM dbo.Expenses WHERE Status = 'PAID';") ?? 0;
                    decimal totalPayrollOct = await conn.ExecuteScalarAsync<decimal?>("SELECT SUM(NetIncome) FROM dbo.Payrolls WHERE Month = 10 AND Year = 2026;") ?? 0;

                    sb.AppendLine($"\n[6. BÁO CÁO TÀI CHÍNH & TIỀN LƯƠNG (Dành riêng cho {normRole})]");
                    sb.AppendLine($"- Tổng giá trị đơn đặt phòng lũy kế: {totalBookingRev:N0} VNĐ (Đã đối soát cọc: {confirmedDeposit:N0} VNĐ)");
                    sb.AppendLine($"- Tổng giao dịch thu (FinancialTransactions INCOME): {finIncome:N0} VNĐ");
                    sb.AppendLine($"- Tổng phiếu chi vận hành (Expenses PAID): {finExpense:N0} VNĐ");
                    sb.AppendLine($"- Tổng quỹ lương thực lĩnh kỳ Tháng 10/2026: {totalPayrollOct:N0} VNĐ");
                }

                // 6. Dữ liệu Nhân Sự (Chỉ cấp cho ADMIN và HR_OPS)
                if (normRole == "ADMIN" || normRole == "HR_OPS")
                {
                    tools.Add("SQL_HR_EMPLOYEES_QUERY");
                    var emps = (await conn.QueryAsync<dynamic>(@"
                        SELECT e.EmployeeCode, e.FullName, d.Name AS DeptName, p.Title AS PosTitle, e.WorkStatus
                        FROM dbo.Employees e
                        JOIN dbo.Departments d ON e.DepartmentId = d.Id
                        JOIN dbo.Positions p ON e.PositionId = p.Id
                        WHERE e.IsDeleted = 0;")).ToList();

                    sb.AppendLine($"\n[7. HỒ SƠ NHÂN SỰ ({emps.Count} nhân viên đang làm việc)]");
                    foreach (var e in emps)
                    {
                        sb.AppendLine($"- {e.EmployeeCode}: {e.FullName} | {e.PosTitle} ({e.DeptName}) | Trạng thái: {e.WorkStatus}");
                    }
                }

                sb.AppendLine("\n[CHÍNH SÁCH CỐT LÕI KIM LONG ECOSTAY]");
                sb.AppendLine("- Đặt cọc 50% qua VietQR (TPBank - STK 10787723714 - Trần Viết Mạnh Cường), 50% còn lại thanh toán khi Check-in (14:00).");
                sb.AppendLine("- Luật hủy phòng 48 giờ: Hủy trước 48h hoàn cọc 100% trong 24h làm việc.");
                sb.AppendLine("- Miễn phí xe đạp trợ lực điện Eco-Bike & tai nghe GPS Audio Guide 3 ngôn ngữ khám phá Chùa Thiên Mụ (1.2km), Nhà vườn An Hiên (450m).");

                string fallback = BuildDeterministicSqlAnswer(question, normRole, rooms, products, teas, totalBookingsCount, pendingBookingsCount, activeDeliveriesCount, totalBookingRev, confirmedDeposit, finIncome);
                return (sb.ToString(), tools, fallback);
            }
            catch (Exception ex)
            {
                return ($"Lỗi truy vấn SQL tạm thời: {ex.Message}", tools, "Dạ hệ thống đang đồng bộ dữ liệu SQL Server. Quý khách vui lòng thử lại sau giây lát ạ!");
            }
        }

        private static string BuildDeterministicSqlAnswer(
            string question,
            string normRole,
            List<dynamic> rooms,
            List<dynamic> products,
            List<dynamic> teas,
            int totalBookingsCount,
            int pendingBookingsCount,
            int activeDeliveriesCount,
            decimal totalBookingRev,
            decimal confirmedDeposit,
            decimal finIncome)
        {
            string q = question.ToLowerInvariant();
            int readyCount = rooms.Count(r => (string)r.Status == "AVAILABLE" || (string)r.Status == "CLEANED_READY");

            if (q.Contains("phòng") || q.Contains("trống") || q.Contains("giá") || q.Contains("lưu trú") || q.Contains("đặt"))
            {
                var sb = new StringBuilder();
                sb.AppendLine($"🌿 **Dữ liệu thời gian thực từ SQL Server (`KimLongEcoStayDB`):**");
                sb.AppendLine($"Hiện tại hệ thống có **{readyCount}/{rooms.Count} buồng phòng** sẵn sàng đón khách ngay:\n");
                sb.AppendLine("| Mã Phòng | Tên Hạng Phòng | Giá Niêm Yết | Sức Chứa | Trạng Thái SQL |");
                sb.AppendLine("|---|---|---|---|---|");
                foreach (var r in rooms)
                {
                    sb.AppendLine($"| **{r.RoomNumber}** | {r.TypeName} | **{((decimal)r.BasePrice):N0} đ/đêm** | {r.MaxGuests} khách | `{r.Status}` |");
                }
                sb.AppendLine("\n🎁 **Đặc quyền kèm theo:** Miễn phí 02 xe đạp trợ lực điện Eco-Bike + GPS Audio Guide, trà sen Cung Đình đón tiếp và nhận phòng tự động qua khóa SmartLock IoT sau khi cọc 50% VietQR.");
                return sb.ToString();
            }

            if (q.Contains("sản phẩm") || q.Contains("đặc sản") || q.Contains("mứt") || q.Contains("bánh") || q.Contains("ăn") || q.Contains("tồn kho") || q.Contains("sắp hết"))
            {
                var low = products.Where(p => (int)p.StockQuantity <= 20).ToList();
                var sb = new StringBuilder();
                sb.AppendLine($"🧺 **Thống kê Kho Đặc Sản & Ẩm Thực từ SQL Server (`{products.Count}` sản phẩm):**\n");
                if (q.Contains("tồn kho") || q.Contains("sắp hết"))
                {
                    sb.AppendLine($"• **Các mặt hàng tồn kho thấp (<= 20 đơn vị) cần nhập bổ sung ({low.Count} món):**");
                    foreach (var p in low)
                    {
                        sb.AppendLine($"  - `{p.ProductCode}` **{p.Name}**: còn **{p.StockQuantity} {p.Unit}** *(Giá: {((decimal)p.Price):N0}đ)*");
                    }
                }
                else
                {
                    sb.AppendLine("| Mã SP | Tên Đặc Sản / Món Huế | Danh Mục | Giá Bán | Tồn Kho SQL |");
                    sb.AppendLine("|---|---|---|---|---|");
                    foreach (var p in products.Take(10))
                    {
                        sb.AppendLine($"| `{p.ProductCode}` | **{p.Name}** | {p.Category} | **{((decimal)p.Price):N0} đ/{p.Unit}** | {p.StockQuantity} |");
                    }
                    sb.AppendLine($"\n*(Hiển thị 10/{products.Count} sản phẩm tiêu biểu — hỗ trợ giao nóng tận cửa phòng trong 15–30 phút!)*");
                }
                return sb.ToString();
            }

            if (q.Contains("trà") || q.Contains("cung đình"))
            {
                var sb = new StringBuilder();
                sb.AppendLine("🍵 **Các Gói Trải Nghiệm Thưởng Trà Cung Đình Huế (Dữ liệu SQL Server):**\n");
                foreach (var t in teas)
                {
                    sb.AppendLine($"• **{t.Name}** (`{t.ServiceCode}`): **{((decimal)t.Price):N0} đ/{t.PriceUnit}** — Còn **{t.AvailableSlots} chỗ trống** *(Khung giờ: {t.ServingSchedule})*");
                }
                return sb.ToString();
            }

            if (q.Contains("doanh thu") || q.Contains("báo cáo") || q.Contains("tổng hợp") || q.Contains("vận hành") || q.Contains("hôm nay"))
            {
                return $@"📊 **Báo Cáo Tổng Hợp Vận Hành Thời Gian Thực (SQL Server `KimLongEcoStayDB`):**
• **Buồng phòng sẵn sàng đón khách:** **{readyCount}/5 phòng**
• **Tổng số đơn đặt phòng trên hệ thống:** **{totalBookingsCount} đơn** *(Chờ đối soát cọc 50%: **{pendingBookingsCount} đơn**)*
• **Đơn giao đặc sản tận phòng đang phục vụ:** **{activeDeliveriesCount} đơn**
• **Tổng doanh thu đặt phòng ghi nhận:** **{totalBookingRev:N0} VNĐ** *(Đã thu cọc: **{confirmedDeposit:N0} VNĐ**)*
• **Tổng dòng tiền thu dịch vụ & lưu trú (`FinancialTransactions`):** **{finIncome:N0} VNĐ**";
            }

            return $@"🌿 **Xin chào! Em là Trợ lý AI Kim Long EcoStay (Kết nối trực tiếp CSDL SQL Server `KimLongEcoStayDB`):**
• **Phòng nghỉ sẵn sàng:** **{readyCount}/5 buồng phòng** (Giá từ `390.000 đ` – `820.000 đ/đêm`).
• **Cửa hàng Đặc sản & Ẩm thực Huế:** **{products.Count} sản phẩm** chính gốc Kim Long, hỗ trợ **Giao Tận Phòng** trong 15–30 phút.
• **Trải nghiệm Trà Cung Đình:** **{teas.Count} gói thưởng trà** tại hiên nhà rường cổ.
• **Vai trò hiện tại của bạn:** `{GetDefaultRoleTitle(normRole)}`.

Bạn muốn em tra cứu chi tiết về **phòng trống, đặt món giao tận phòng, lộ trình xe đạp Chùa Thiên Mụ** hay **số liệu vận hành trong SQL** ạ?";
        }

        private async Task LogAiAuditAsync(string convId, string role, string userName, string provider, string model, string prompt, string tools, int tokens, int elapsedMs, string status)
        {
            try
            {
                await using var conn = new SqlConnection(_sqlSync.ConnectionString);
                await conn.OpenAsync();
                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.AiAuditLogs (
                        ConversationId, UserRole, UserName, Provider, Model,
                        PromptText, SqlToolsUsed, TokensEstimated, ResponseTimeMs, Status, CreatedAt
                    ) VALUES (
                        @ConvId, @Role, @UserName, @Provider, @Model,
                        @Prompt, @Tools, @Tokens, @Ms, @Status, SYSUTCDATETIME()
                    );",
                    new
                    {
                        ConvId = convId,
                        Role = role,
                        UserName = userName,
                        Provider = provider,
                        Model = model,
                        Prompt = prompt.Length > 950 ? prompt.Substring(0, 950) : prompt,
                        Tools = tools,
                        Tokens = tokens,
                        Ms = elapsedMs,
                        Status = status
                    });
            }
            catch
            {
                // Không làm gián đoạn phản hồi AI nếu bảng log bận
            }
        }

        private static string NormalizeRole(string? rawRole)
        {
            if (string.IsNullOrWhiteSpace(rawRole)) return "GUEST";
            string r = rawRole.Trim().ToUpperInvariant();
            if (r.Contains("ADMIN") || r.Contains("CEO")) return "ADMIN";
            if (r.Contains("ACCOUNTANT") || r.Contains("KETOAN") || r.Contains("CFO")) return "ACCOUNTANT";
            if (r.Contains("HR") || r.Contains("NHANSU") || r.Contains("COO") || r.Contains("MANAGER")) return "HR_OPS";
            if (r.Contains("RECEPTION") || r.Contains("STAFF") || r.Contains("LETAN")) return "RECEPTIONIST";
            return "GUEST";
        }

        private static string GetDefaultRoleTitle(string normRole) => normRole switch
        {
            "ADMIN" => "Admin (CEO)",
            "ACCOUNTANT" => "Kế Toán Trưởng (CFO)",
            "HR_OPS" => "Nhân Sự & Vận Hành (COO)",
            "RECEPTIONIST" => "Lễ Tân Đón Tiếp",
            _ => "Khách Hàng / Du Khách"
        };
    }
}

