using System;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.CoreBusiness.Services;
using Xunit;

namespace KimLongEcoStay.Tests
{
    public class GmailServiceTests
    {
        [Fact]
        public void GmailService_IsConfigured_ReturnsTrueWhenCredentialsProvided()
        {
            var settings = new EmailSettings
            {
                GmailClientId = "mock-client-id.apps.googleusercontent.com",
                GmailClientSecret = "mock-client-secret"
            };
            var service = new GmailService(settings);

            Assert.True(service.IsConfigured);
        }

        [Fact]
        public void GmailService_IsConfigured_ReturnsFalseWhenCredentialsMissing()
        {
            string? origId = Environment.GetEnvironmentVariable("GMAIL_CLIENT_ID");
            string? origSec = Environment.GetEnvironmentVariable("GMAIL_CLIENT_SECRET");
            try
            {
                Environment.SetEnvironmentVariable("GMAIL_CLIENT_ID", null);
                Environment.SetEnvironmentVariable("GMAIL_CLIENT_SECRET", null);
                var settings = new EmailSettings
                {
                    GmailClientId = "",
                    GmailClientSecret = ""
                };
                var service = new GmailService(settings);

                Assert.False(service.IsConfigured);
            }
            finally
            {
                Environment.SetEnvironmentVariable("GMAIL_CLIENT_ID", origId);
                Environment.SetEnvironmentVariable("GMAIL_CLIENT_SECRET", origSec);
            }
        }

        [Fact]
        public void GmailService_CreateCsrfState_ValidatesOnlyOnce()
        {
            var settings = new EmailSettings
            {
                GmailClientId = "test-id",
                GmailClientSecret = "test-secret"
            };
            var service = new GmailService(settings);

            string state = service.CreateCsrfState();
            Assert.False(string.IsNullOrWhiteSpace(state));

            // Lần 1: Xác thực thành công
            bool validFirstTime = service.ValidateCsrfState(state);
            Assert.True(validFirstTime);

            // Lần 2 (Replay Attack): Phải bị từ chối
            bool validSecondTime = service.ValidateCsrfState(state);
            Assert.False(validSecondTime);
        }

        [Fact]
        public void GmailService_ValidateCsrfState_RejectsRandomState()
        {
            var service = new GmailService(new EmailSettings());
            bool valid = service.ValidateCsrfState("tampered-or-fake-state");
            Assert.False(valid);
        }

        [Fact]
        public void GmailService_GetAuthorizationUrl_ContainsRequiredParametersAndSendScope()
        {
            var settings = new EmailSettings
            {
                GmailClientId = "375783123528-mock.apps.googleusercontent.com",
                GmailClientSecret = "mock-secret",
                GmailRedirectUri = "http://localhost:5204/api/admin/gmail/oauth2callback"
            };
            var service = new GmailService(settings);

            string state = service.CreateCsrfState();
            string authUrl = service.GetAuthorizationUrl(settings.GmailRedirectUri, state);

            Assert.Contains("https://accounts.google.com/o/oauth2/v2/auth", authUrl);
            Assert.Contains("client_id=375783123528-mock.apps.googleusercontent.com", authUrl);
            Assert.Contains("scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fgmail.send", authUrl);
            Assert.Contains("access_type=offline", authUrl);
            Assert.Contains("prompt=consent", authUrl);
            Assert.Contains($"state={state}", authUrl);
        }

        [Fact]
        public void GmailService_GetConnectionStatus_NeverExposesClientSecretOrRefreshToken()
        {
            var settings = new EmailSettings
            {
                GmailClientId = "public-client-id",
                GmailClientSecret = "SUPER_SECRET_KEY_12345",
                GmailRefreshToken = "SECRET_REFRESH_TOKEN_67890",
                GmailSender = "titanio741@gmail.com"
            };
            var service = new GmailService(settings);
            var status = service.GetConnectionStatus();

            Assert.True(status.IsConfigured);
            Assert.True(status.IsConnected);
            Assert.Equal("titanio741@gmail.com", status.SenderEmail);

            // Đảm bảo kiểu dữ liệu status không có thuộc tính chứa secret / token
            var properties = typeof(GmailConnectionStatus).GetProperties();
            foreach (var prop in properties)
            {
                Assert.DoesNotContain("Secret", prop.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Token", prop.Name.Replace("TokenExpiresAt", ""));
            }
        }

        [Fact]
        public void GmailService_UpdateCredentials_UpdatesRuntimeConfigurationAndEffectiveCredentials()
        {
            var settings = new EmailSettings { IsSimulationMode = true };
            var service = new GmailService(settings);

            service.UpdateCredentials("new-client-id", "new-client-secret", "admin@kimlongecostay.vn", persist: false);

            Assert.True(service.IsConfigured);
            Assert.Equal("admin@kimlongecostay.vn", service.SenderEmail);
        }

        [Fact]
        public async System.Threading.Tasks.Task EmailService_SendBookingCancellationEmailAsync_DispatchesCancellationTemplate()
        {
            var settings = new EmailSettings { IsSimulationMode = true };
            var service = new EmailService(settings);
            var order = new Order
            {
                UniqueId = "KLE-CANCEL-001",
                CustomerName = "Nguyễn Văn Test",
                CustomerEmail = "test@example.com"
            };

            var result = await service.SendBookingCancellationEmailAsync(order, "Eco-Nest Ban Mai (KLE-R101)", "Khách có việc đột xuất", 750000);

            Assert.True(result.IsSuccess);
            Assert.Equal(EmailTemplateType.BOOKING_CANCELLED, result.TemplateType);
            Assert.Contains("KLE-CANCEL-001", result.HtmlContent);
            Assert.Contains("750,000 VNĐ", result.HtmlContent);
            Assert.Contains("Khách có việc đột xuất", result.HtmlContent);
        }
    }
}

