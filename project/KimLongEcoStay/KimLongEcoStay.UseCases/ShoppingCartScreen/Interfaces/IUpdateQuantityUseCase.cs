using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.ShoppingCartScreen.Interfaces
{
    public interface IUpdateQuantityUseCase
    {
        Task<Order> Execute(int productId, int quantity);
    }
}