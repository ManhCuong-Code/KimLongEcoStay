using System;
using System.Collections.Generic;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.CoreBusiness.Services
{
    public interface IBookingService : IOrderService
    {
        bool ValidateBooking(Booking booking);
        bool ValidateOverbooking(int roomId, DateTime checkIn, DateTime checkOut, IEnumerable<BookingNight>? existingNights = null);
        List<NightlyPriceDetail> CalculateNightlyRates(decimal basePrice, DateTime checkIn, DateTime checkOut);
        BookingCancellationPolicyResult CalculateCancellationPolicy(DateTime checkInDate, decimal depositAmount, decimal firstNightPrice);
        string GenerateSmartLockPin(string bookingCode, string phone);
        SmartPasteExtractedBooking ParseZaloMessage(string rawText);
    }
}
