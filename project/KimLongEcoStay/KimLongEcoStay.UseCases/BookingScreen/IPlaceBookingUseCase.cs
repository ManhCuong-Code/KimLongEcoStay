using KimLongEcoStay.CoreBusiness.Models;
using System.Threading.Tasks;

namespace KimLongEcoStay.UseCases.BookingScreen
{
    public interface IPlaceBookingUseCase
    {
        Task<string> Execute(Order order);
    }
}
