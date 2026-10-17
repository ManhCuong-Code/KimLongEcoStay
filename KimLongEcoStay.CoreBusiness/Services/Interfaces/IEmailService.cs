using System.Collections.Generic;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.CoreBusiness.Services.Interfaces
{
    public interface IEmailService
    {
        Task<EmailSendResult> SendBookingConfirmationEmailAsync(Order order, string smartLockPin, string roomName);
        Task<EmailSendResult> SendEmailAsync(string toEmail, string subject, string htmlBody);
        string GenerateBookingConfirmationHtml(Order order, string smartLockPin, string roomName);
        IReadOnlyList<EmailSendResult> GetSentEmailHistory();
    }
}
