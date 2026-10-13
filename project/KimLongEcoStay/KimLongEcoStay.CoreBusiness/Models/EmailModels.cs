using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = "booking@kimlongecostay.vn";
        public string SenderName { get; set; } = "Kim Long EcoStay — Nhà Vườn Cố Đô";
        public string SenderPassword { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public bool IsSimulationMode { get; set; } = false;
    }

    public class EmailSendResult
    {
        public bool IsSuccess { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsSimulated { get; set; }
        public string HtmlContent { get; set; } = string.Empty;
    }
}
