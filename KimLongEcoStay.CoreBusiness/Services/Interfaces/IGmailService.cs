using System;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.CoreBusiness.Services.Interfaces
{
    public interface IGmailService
    {
        bool IsConfigured { get; }
        bool IsConnected { get; }
        string SenderEmail { get; }

        string CreateCsrfState();
        bool ValidateCsrfState(string state);

        string GetAuthorizationUrl(string redirectUri, string state);
        Task<(bool Success, string Message, string? RefreshToken, string? AccessToken)> ExchangeCodeForTokenAsync(string code, string redirectUri);
        Task<string?> GetValidAccessTokenAsync();
        Task<(bool Success, string MessageId, string? Error)> SendEmailRawAsync(string toEmail, string subject, string htmlBody, string? plainText = null);
        GmailConnectionStatus GetConnectionStatus();
        void Disconnect();
        void UpdateCredentials(string clientId, string clientSecret, string? sender = null, string? redirectUri = null, string? refreshToken = null, bool persist = true);
    }
}

