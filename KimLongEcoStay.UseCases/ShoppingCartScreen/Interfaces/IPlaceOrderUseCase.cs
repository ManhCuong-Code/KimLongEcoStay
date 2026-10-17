using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.ShoppingCartScreen.Interfaces
{
    public interface IPlaceOrderUseCase
    {
        Task<string?> Execute(Order order);
    }
}