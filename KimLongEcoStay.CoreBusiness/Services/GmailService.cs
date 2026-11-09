using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.CoreBusiness.Services.Interfaces;

namespace KimLongEcoStay.CoreBusiness.Services
{
    public class GmailService : IGmailService
    {
        private const string GoogleOAuthAuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string GoogleOAuthTokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string GmailApiSendEndpoint = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";
        private const string GmailSendScope = "https://www.googleapis.com/auth/gmail.send";

        private readonly EmailSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _tokenLock = new(1, 1);

        // Quản lý CSRF State trong vòng 15 phút
        private static readonly ConcurrentDictionary<string, DateTime> _csrfStates = new();

        // Lưu trữ Token trong bộ nhớ
        private GmailTokenData? _tokenData;
        private DateTime? _lastSentAt;
        private string _lastStatusMessage = "Chưa gửi email nào qua Gmail API.";
        private string _lastError = string.Empty;
        private int _totalSentCount = 0;

        public GmailService(EmailSettings settings, HttpClient? httpClient = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(25) };

            // Tự động tải cấu hình và token đã lưu từ đĩa hoặc biến môi trường
            LoadStoredConfig();
            LoadStoredToken();
        }

        public string EffectiveClientId =>
            !string.IsNullOrWhiteSpace(_settings.GmailClientId)
                ? _settings.GmailClientId.Trim()
                : (Environment.GetEnvironmentVariable("GMAIL_CLIENT_ID") ?? string.Empty).Trim();

        public string EffectiveClientSecret =>
            !string.IsNullOrWhiteSpace(_settings.GmailClientSecret)
                ? _settings.GmailClientSecret.Trim()
                : (Environment.GetEnvironmentVariable("GMAIL_CLIENT_SECRET") ?? string.Empty).Trim();

        public bool IsConfigured => !string.IsNullOrWhiteSpace(EffectiveClientId) &&
                                    !string.IsNullOrWhiteSpace(EffectiveClientSecret);

        public bool IsConnected => _tokenData != null && !string.IsNullOrWhiteSpace(_tokenData.RefreshToken);

        public string SenderEmail
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_settings.GmailSender)) return _settings.GmailSender.Trim();
                var envUser = Environment.GetEnvironmentVariable("GMAIL_USER");
                if (!string.IsNullOrWhiteSpace(envUser)) return envUser.Trim();
                var envSender = Environment.GetEnvironmentVariable("GMAIL_SENDER");
                if (!string.IsNullOrWhiteSpace(envSender)) return envSender.Trim();
                if (!string.IsNullOrWhiteSpace(_settings.AdminEmail)) return _settings.AdminEmail.Trim();
                return "titanio741@gmail.com";
            }
        }

        // =========================================================================
        // 1. QUẢN LÝ CSRF STATE CHO OAUTH 2.0
        // =========================================================================
        public string CreateCsrfState()
        {
            // Dọn dẹp các state đã quá hạn 15 phút
            var now = DateTime.UtcNow;
            foreach (var kvp in _csrfStates)
            {
                if (kvp.Value < now)
                {
                    _csrfStates.TryRemove(kvp.Key, out _);
                }
            }

            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            string state = Convert.ToBase64String(randomBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');

            _csrfStates[state] = now.AddMinutes(15);
            return state;
        }

        public bool ValidateCsrfState(string state)
        {
            if (string.IsNullOrWhiteSpace(state)) return false;

            if (_csrfStates.TryRemove(state, out var expiry))
            {
                return expiry >= DateTime.UtcNow;
            }
            return false;
        }

        // =========================================================================
        // 2. KHỞI TẠO URL XÁC THỰC OAUTH 2.0 WEB SERVER FLOW
        // =========================================================================
        public string GetAuthorizationUrl(string redirectUri, string state)
        {
            if (string.IsNullOrWhiteSpace(EffectiveClientId))
            {
                throw new InvalidOperationException("Chưa cấu hình GMAIL_CLIENT_ID trong biến môi trường hoặc .env.");
            }

            string actualRedirect = !string.IsNullOrWhiteSpace(redirectUri)
                ? redirectUri
                : (!string.IsNullOrWhiteSpace(_settings.GmailRedirectUri) ? _settings.GmailRedirectUri : "http://localhost:5204/api/admin/gmail/oauth2callback");

            var urlBuilder = new StringBuilder(GoogleOAuthAuthEndpoint);
            urlBuilder.Append("?client_id=").Append(Uri.EscapeDataString(EffectiveClientId));
            urlBuilder.Append("&redirect_uri=").Append(Uri.EscapeDataString(actualRedirect.Trim()));
            urlBuilder.Append("&response_type=code");
            urlBuilder.Append("&scope=").Append(Uri.EscapeDataString(GmailSendScope));
            urlBuilder.Append("&access_type=offline");
            urlBuilder.Append("&prompt=consent"); // Đảm bảo luôn cấp Refresh Token
            urlBuilder.Append("&state=").Append(Uri.EscapeDataString(state));

            return urlBuilder.ToString();
        }

        // =========================================================================
        // 3. ĐỔI AUTHORIZATION CODE LẤY TOKENS (ACCESS TOKEN & REFRESH TOKEN)
        // =========================================================================
        public async Task<(bool Success, string Message, string? RefreshToken, string? AccessToken)> ExchangeCodeForTokenAsync(string code, string redirectUri)
        {
            if (string.IsNullOrWhiteSpace(code))
                return (false, "Mã ủy quyền (authorization code) từ Google bị trống.", null, null);

            if (!IsConfigured)
                return (false, "Chưa cấu hình GMAIL_CLIENT_ID hoặc GMAIL_CLIENT_SECRET trên hệ thống.", null, null);

            string actualRedirect = !string.IsNullOrWhiteSpace(redirectUri)
                ? redirectUri
                : (!string.IsNullOrWhiteSpace(_settings.GmailRedirectUri) ? _settings.GmailRedirectUri : "http://localhost:5204/api/admin/gmail/oauth2callback");

            try
            {
                var dict = new Dictionary<string, string>
                {
                    ["code"] = code.Trim(),
                    ["client_id"] = EffectiveClientId,
                    ["client_secret"] = EffectiveClientSecret,
                    ["redirect_uri"] = actualRedirect.Trim(),
                    ["grant_type"] = "authorization_code"
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, GoogleOAuthTokenEndpoint)
                {
                    Content = new FormUrlEncodedContent(dict)
                };

                var res = await _httpClient.SendAsync(req);
                var json = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    _lastError = $"Google OAuth Error ({res.StatusCode}): {json}";
                    return (false, $"Lỗi đổi mã cấp quyền từ Google: {json}", null, null);
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() ?? "" : "";
                string refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "";
                int expiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;

                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    // Nếu Google không trả refresh token mới (do đã kết nối trước đó), dùng refresh token hiện có
                    if (_tokenData != null && !string.IsNullOrWhiteSpace(_tokenData.RefreshToken))
                    {
                        refreshToken = _tokenData.RefreshToken;
                    }
                    else
                    {
                        _lastError = "Google không gửi Refresh Token. Vui lòng bấm Kết nối lại với prompt=consent.";
                        return (false, "Google không gửi Refresh Token. Vui lòng thử lại với tài khoản quản trị.", null, accessToken);
                    }
                }

                await _tokenLock.WaitAsync();
                try
                {
                    _tokenData = new GmailTokenData
                    {
                        RefreshToken = refreshToken,
                        AccessToken = accessToken,
                        ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 120), // 2 phút buffer an toàn
                        AccountEmail = SenderEmail,
                        UpdatedAt = DateTime.UtcNow
                    };
                    SaveTokenToDisk(_tokenData);
                    _lastStatusMessage = $"Đã kết nối thành công với Gmail API ({SenderEmail}) lúc {DateTime.Now:HH:mm:ss dd/MM/yyyy}.";
                    _lastError = string.Empty;
                }
                finally
                {
                    _tokenLock.Release();
                }

                return (true, "Ủy quyền OAuth 2.0 Gmail thành công!", refreshToken, accessToken);
            }
            catch (Exception ex)
            {
                _lastError = $"Ngoại lệ khi đổi OAuth code: {ex.Message}";
                return (false, $"Lỗi xử lý kết nối Google: {ex.Message}", null, null);
            }
        }

        // =========================================================================
        // 4. LẤY ACCESS TOKEN HỢP LỆ (TỰ ĐỘNG LÀM MỚI BẰNG REFRESH TOKEN KHI HẾT HẠN)
        // =========================================================================
        public async Task<string?> GetValidAccessTokenAsync()
        {
            if (_tokenData == null || string.IsNullOrWhiteSpace(_tokenData.RefreshToken))
            {
                return null;
            }

            // Nếu Access Token hiện tại vẫn còn hạn (buffer 2 phút)
            if (!string.IsNullOrWhiteSpace(_tokenData.AccessToken) && _tokenData.ExpiresAt > DateTime.UtcNow)
            {
                return _tokenData.AccessToken;
            }

            await _tokenLock.WaitAsync();
            try
            {
                // Kiểm tra lại sau khi đã có lock (Double-checked locking)
                if (!string.IsNullOrWhiteSpace(_tokenData.AccessToken) && _tokenData.ExpiresAt > DateTime.UtcNow)
                {
                    return _tokenData.AccessToken;
                }

                var dict = new Dictionary<string, string>
                {
                    ["client_id"] = EffectiveClientId,
                    ["client_secret"] = EffectiveClientSecret,
                    ["refresh_token"] = _tokenData.RefreshToken.Trim(),
                    ["grant_type"] = "refresh_token"
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, GoogleOAuthTokenEndpoint)
                {
                    Content = new FormUrlEncodedContent(dict)
                };

                var res = await _httpClient.SendAsync(req);
                var json = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    _lastError = $"Google Refresh Token Error ({res.StatusCode}): {json}";
                    if (json.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
                    {
                        _lastStatusMessage = "Refresh Token đã bị thu hồi hoặc hết hạn. Vui lòng kết nối lại tài khoản Google.";
                    }
                    return null;
                }

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string newAccessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() ?? "" : "";
                int expiresIn = root.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;

                if (!string.IsNullOrWhiteSpace(newAccessToken))
                {
                    _tokenData.AccessToken = newAccessToken;
                    _tokenData.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 120);
                    _tokenData.UpdatedAt = DateTime.UtcNow;

                    SaveTokenToDisk(_tokenData);
                    return newAccessToken;
                }

                return null;
            }
            catch (Exception ex)
            {
                _lastError = $"Ngoại lệ khi làm mới Access Token: {ex.Message}";
                return null;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        // =========================================================================
        // 5. GỬI EMAIL THỰC TẾ QUA GMAIL API (users.messages.send)
        // =========================================================================
        public async Task<(bool Success, string MessageId, string? Error)> SendEmailRawAsync(string toEmail, string subject, string htmlBody, string? plainText = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                return (false, "", "Địa chỉ người nhận không được để trống.");

            var accessToken = await GetValidAccessTokenAsync();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                string msg = _tokenData == null || string.IsNullOrWhiteSpace(_tokenData.RefreshToken)
                    ? "Chưa kết nối tài khoản Google Gmail. Quản trị viên cần bấm 'Kết nối Google Gmail' để cấp quyền gửi thư."
                    : "Không thể lấy Access Token từ Google. Refresh Token có thể đã hết hạn hoặc bị hủy.";
                _lastError = msg;
                return (false, "", msg);
            }

            try
            {
                string fromName = !string.IsNullOrWhiteSpace(_settings.SenderName) ? _settings.SenderName : "Kim Long EcoStay";
                string fromEmail = SenderEmail;

                // Xây dựng email theo chuẩn RFC 2822 và mã hóa Base64Url
                string rawBase64Url = BuildRfc2822Base64Url(fromName, fromEmail, toEmail.Trim(), subject, htmlBody, plainText);

                var payload = new { raw = rawBase64Url };
                var jsonPayload = JsonSerializer.Serialize(payload);

                using var req = new HttpRequestMessage(HttpMethod.Post, GmailApiSendEndpoint);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                req.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var res = await _httpClient.SendAsync(req);
                var resBody = await res.Content.ReadAsStringAsync();

                if (res.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(resBody);
                    string msgId = doc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "SENT";

                    _totalSentCount++;
                    _lastSentAt = DateTime.Now;
                    _lastStatusMessage = $"Gửi email thành công tới {toEmail} qua Gmail API lúc {_lastSentAt:HH:mm:ss dd/MM/yyyy} (ID: {msgId}).";
                    _lastError = string.Empty;

                    return (true, msgId, null);
                }

                // Xử lý khi gặp 401 Unauthorized (thử làm mới token 1 lần)
                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized && _tokenData != null)
                {
                    _tokenData.ExpiresAt = DateTime.UtcNow.AddMinutes(-5); // Ép hết hạn
                    var retryToken = await GetValidAccessTokenAsync();
                    if (!string.IsNullOrWhiteSpace(retryToken))
                    {
                        using var retryReq = new HttpRequestMessage(HttpMethod.Post, GmailApiSendEndpoint);
                        retryReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", retryToken);
                        retryReq.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        var retryRes = await _httpClient.SendAsync(retryReq);
                        var retryBody = await retryRes.Content.ReadAsStringAsync();

                        if (retryRes.IsSuccessStatusCode)
                        {
                            using var retryDoc = JsonDocument.Parse(retryBody);
                            string retryMsgId = retryDoc.RootElement.TryGetProperty("id", out var rIdProp) ? rIdProp.GetString() ?? "" : "SENT";

                            _totalSentCount++;
                            _lastSentAt = DateTime.Now;
                            _lastStatusMessage = $"Gửi email thành công (sau khi cấp lại token) tới {toEmail} (ID: {retryMsgId}).";
                            _lastError = string.Empty;

                            return (true, retryMsgId, null);
                        }
                    }
                }

                _lastError = $"Lỗi gửi thư từ Gmail API ({res.StatusCode}): {resBody}";
                return (false, "", _lastError);
            }
            catch (Exception ex)
            {
                _lastError = $"Ngoại lệ khi gửi qua Gmail API: {ex.Message}";
                return (false, "", _lastError);
            }
        }

        // =========================================================================
        // 6. XÂY DỰNG CHUẨN RFC 2822 VÀ MÃ HÓA BASE64URL (RFC 4648 §5)
        // =========================================================================
        private static string BuildRfc2822Base64Url(string fromName, string fromEmail, string toEmail, string subject, string htmlBody, string? plainText)
        {
            var boundary = "=-=" + Guid.NewGuid().ToString("N");
            string encodedSubject = "=?utf-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(subject)) + "?=";
            string encodedFromName = "=?utf-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(fromName)) + "?=";

            var sb = new StringBuilder();
            sb.AppendLine($"From: {encodedFromName} <{fromEmail}>");
            sb.AppendLine($"To: <{toEmail}>");
            sb.AppendLine($"Subject: {encodedSubject}");
            sb.AppendLine("MIME-Version: 1.0");
            sb.AppendLine($"Content-Type: multipart/alternative; boundary=\"{boundary}\"");
            sb.AppendLine();

            // Phần Plain Text
            if (!string.IsNullOrWhiteSpace(plainText))
            {
                sb.AppendLine($"--{boundary}");
                sb.AppendLine("Content-Type: text/plain; charset=\"UTF-8\"");
                sb.AppendLine("Content-Transfer-Encoding: base64");
                sb.AppendLine();
                sb.AppendLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText)));
                sb.AppendLine();
            }

            // Phần HTML chuyên nghiệp
            sb.AppendLine($"--{boundary}");
            sb.AppendLine("Content-Type: text/html; charset=\"UTF-8\"");
            sb.AppendLine("Content-Transfer-Encoding: base64");
            sb.AppendLine();
            sb.AppendLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(htmlBody)));
            sb.AppendLine();

            sb.AppendLine($"--{boundary}--");

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        // =========================================================================
        // 7. TRẠNG THÁI KẾT NỐI & AN TOÀN DỮ LIỆU
        // =========================================================================
        public GmailConnectionStatus GetConnectionStatus()
        {
            return new GmailConnectionStatus
            {
                IsConfigured = IsConfigured,
                IsConnected = IsConnected,
                SenderEmail = SenderEmail,
                TokenExpiresAt = _tokenData?.ExpiresAt,
                LastSentAt = _lastSentAt,
                LastStatusMessage = _lastStatusMessage,
                LastError = _lastError,
                TotalSentCount = _totalSentCount,
                RedirectUri = _settings.GmailRedirectUri,
                Scope = GmailSendScope
            };
        }

        public void Disconnect()
        {
            _tokenLock.Wait();
            try
            {
                _tokenData = null;
                _lastStatusMessage = "Đã ngắt kết nối tài khoản Google Gmail.";
                _lastError = string.Empty;

                var filePath = GetTokenFilePath();
                if (File.Exists(filePath))
                {
                    try { File.Delete(filePath); } catch { }
                }
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        // =========================================================================
        // 8. ĐỌC / GHI CẤU HÌNH & REFRESH TOKEN AN TOÀN TRÊN MÁY CHỦ
        // =========================================================================
        public void UpdateCredentials(string clientId, string clientSecret, string? sender = null, string? redirectUri = null, string? refreshToken = null, bool persist = true)
        {
            if (!string.IsNullOrWhiteSpace(clientId)) _settings.GmailClientId = clientId.Trim();
            if (!string.IsNullOrWhiteSpace(clientSecret)) _settings.GmailClientSecret = clientSecret.Trim();
            if (!string.IsNullOrWhiteSpace(sender)) _settings.GmailSender = sender.Trim();
            if (!string.IsNullOrWhiteSpace(redirectUri)) _settings.GmailRedirectUri = redirectUri.Trim();

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                _settings.GmailRefreshToken = refreshToken.Trim();
                _tokenData = new GmailTokenData
                {
                    RefreshToken = refreshToken.Trim(),
                    AccountEmail = SenderEmail,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
                    UpdatedAt = DateTime.UtcNow
                };
                if (persist) SaveTokenToDisk(_tokenData);
            }

            if (persist) SaveConfigToDisk();
            _lastStatusMessage = $"Đã cập nhật cấu hình Gmail API lúc {DateTime.Now:HH:mm:ss dd/MM/yyyy}.";
            _lastError = string.Empty;
        }

        private void LoadStoredConfig()
        {
            try
            {
                var candidates = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "data", "gmail_config.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "data", "gmail_config.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "KimLongEcoStay", "data", "gmail_config.json")
                };

                foreach (var p in candidates)
                {
                    if (File.Exists(p))
                    {
                        var json = File.ReadAllText(p);
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        if (string.IsNullOrWhiteSpace(_settings.GmailClientId) && root.TryGetProperty("ClientId", out var cid))
                            _settings.GmailClientId = cid.GetString() ?? "";
                        if (string.IsNullOrWhiteSpace(_settings.GmailClientSecret) && root.TryGetProperty("ClientSecret", out var cs))
                            _settings.GmailClientSecret = cs.GetString() ?? "";
                        if (string.IsNullOrWhiteSpace(_settings.GmailSender) && root.TryGetProperty("SenderEmail", out var se))
                            _settings.GmailSender = se.GetString() ?? "";
                        if (string.IsNullOrWhiteSpace(_settings.GmailRedirectUri) && root.TryGetProperty("RedirectUri", out var ru))
                            _settings.GmailRedirectUri = ru.GetString() ?? "";
                        break;
                    }
                }
            }
            catch { }
        }

        private void SaveConfigToDisk()
        {
            if (_settings.IsSimulationMode) return;
            try
            {
                var configObj = new
                {
                    ClientId = EffectiveClientId,
                    ClientSecret = EffectiveClientSecret,
                    SenderEmail = SenderEmail,
                    RedirectUri = _settings.GmailRedirectUri
                };
                var json = JsonSerializer.Serialize(configObj, new JsonSerializerOptions { WriteIndented = true });
                var candidates = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "data", "gmail_config.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "data", "gmail_config.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "KimLongEcoStay", "data", "gmail_config.json")
                };

                foreach (var filePath in candidates)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        File.WriteAllText(filePath, json);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void LoadStoredToken()
        {
            try
            {
                // 1. Kiểm tra trong biến môi trường
                string envRefresh = Environment.GetEnvironmentVariable("GMAIL_REFRESH_TOKEN") ?? _settings.GmailRefreshToken;
                if (!string.IsNullOrWhiteSpace(envRefresh))
                {
                    _tokenData = new GmailTokenData
                    {
                        RefreshToken = envRefresh.Trim(),
                        AccountEmail = SenderEmail,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Yêu cầu làm mới lần đầu
                        UpdatedAt = DateTime.UtcNow
                    };
                    return;
                }

                // 2. Kiểm tra trong file lưu trữ cục bộ
                var filePath = GetTokenFilePath();
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var data = JsonSerializer.Deserialize<GmailTokenData>(json);
                    if (data != null && !string.IsNullOrWhiteSpace(data.RefreshToken))
                    {
                        _tokenData = data;
                    }
                }
            }
            catch (Exception ex)
            {
                _lastError = $"Không thể tải token lưu trữ: {ex.Message}";
            }
        }

        private void SaveTokenToDisk(GmailTokenData data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                var candidates = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "data", "gmail_token.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "data", "gmail_token.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "KimLongEcoStay", "data", "gmail_token.json")
                };

                foreach (var filePath in candidates)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        File.WriteAllText(filePath, json);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _lastError = $"Không thể lưu token vào file: {ex.Message}";
            }
        }

        private static string GetTokenFilePath()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "data", "gmail_token.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "data", "gmail_token.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "KimLongEcoStay", "data", "gmail_token.json")
            };

            foreach (var p in candidates)
            {
                var dir = Path.GetDirectoryName(p);
                if (Directory.Exists(dir)) return p;
            }

            return candidates[0];
        }
    }
}
