using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.ShoppingCartScreen.Interfaces
{
    public interface IViewShoppingCartUseCase
    {
        Task<Order> Execute();
    }
}