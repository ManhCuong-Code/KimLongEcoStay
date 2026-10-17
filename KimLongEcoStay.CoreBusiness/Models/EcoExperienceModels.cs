using System;
using System.Collections.Generic;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class EcoExperiencePackage
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Unit { get; set; } = "người";
        public string Description { get; set; } = string.Empty;
        public string IconClass { get; set; } = "bi-stars";
        public bool IsComplimentary { get; set; } = false;
        public bool IsSelected { get; set; } = false;
    }

    public class HourlyStayOption
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Hours { get; set; }
        public decimal PricePerPackage { get; set; }
        public string TimeSlotSuggestion { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class SmartPasteExtractedBooking
    {
        public string RawMessage { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string RoomNameOrType { get; set; } = string.Empty;
        public int NumberOfGuests { get; set; } = 2;
        public DateTime CheckInDate { get; set; } = DateTime.Today;
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(2);
        public int TotalNights => Math.Max(1, (int)(CheckOutDate.Date - CheckInDate.Date).TotalDays);
        public decimal EstimatedTotal { get; set; }
        public decimal EstimatedDeposit => EstimatedTotal * 0.5m;
        public string SpecialRequests { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; } = 0.95;
    }
}
