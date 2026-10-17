using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;
using System.Collections.Generic;

namespace KimLongEcoStay.UseCases.SearchRoomScreen
{
    public class SearchRoomUseCase : ISearchRoomUseCase
    {
        private readonly IProductRepository _productRepository;
        public SearchRoomUseCase(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public IEnumerable<Product> Execute(string? filter = null)
        {
            return _productRepository.GetProducts(filter);
        }
    }
}
