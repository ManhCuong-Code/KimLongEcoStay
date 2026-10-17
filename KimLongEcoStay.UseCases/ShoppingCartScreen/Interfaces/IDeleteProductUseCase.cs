using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.UseCases.ShoppingCartScreen.Interfaces
{
    public interface IDeleteProductUseCase
    {
        Task<Order> Execute(int productId);
    }
}