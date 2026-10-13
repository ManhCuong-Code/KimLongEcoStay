using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.OrderConfirmationScreen
{
    public interface IOrderConfirmationUseCase
    {
        Order? Execute(string uniqueId);
    }
}