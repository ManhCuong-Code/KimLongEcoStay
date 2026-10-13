using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.UseCases.ViewRoomScreen
{
    public class ViewRoomUseCase : IViewRoomUseCase
    {
        private readonly IProductRepository _productRepository;
        public ViewRoomUseCase(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public Product? Execute(int id)
        {
            return _productRepository.GetProduct(id);
        }
    }
}
