using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.PluginInterfaces.DataStore
{
    public interface IBookingRepository : IOrderRepository
    {
        Booking? GetBookingByCode(string bookingCode);
    }
}
