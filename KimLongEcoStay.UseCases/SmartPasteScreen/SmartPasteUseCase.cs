using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.CoreBusiness.Services;

namespace KimLongEcoStay.UseCases.SmartPasteScreen
{
    public interface ISmartPasteUseCase
    {
        SmartPasteExtractedBooking Execute(string rawZaloMessage);
    }

    public class SmartPasteUseCase : ISmartPasteUseCase
    {
        private readonly IBookingService _bookingService;

        public SmartPasteUseCase(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public SmartPasteExtractedBooking Execute(string rawZaloMessage)
        {
            return _bookingService.ParseZaloMessage(rawZaloMessage);
        }
    }
}
