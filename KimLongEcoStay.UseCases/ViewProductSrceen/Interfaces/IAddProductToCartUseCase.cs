using System.Threading.Tasks;

namespace KimLongEcoStay.UseCases.ViewProductSrceen.Interfaces
{
    public interface IAddProductToCartUseCase
    {
        void Execute(int productId);
        Task ExecuteAsync(int productId);
    }
}