using KimLongEcoStay.UseCases.PluginInterfaces.StateStore;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;
using KimLongEcoStay.UseCases.PluginInterfaces.UI;
using KimLongEcoStay.UseCases.ViewProductSrceen.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace KimLongEcoStay.UseCases.ViewProductSrceen
{
    public class AddProductToCartUseCase : IAddProductToCartUseCase
    {
        private readonly IProductRepository productRepository;
        private readonly IShoppingCart shoppingCart;
        private readonly IShoppingCartStateStore shoppingCartStateStore;

        public AddProductToCartUseCase(
            IProductRepository productRepository, 
            IShoppingCart shoppingCart,
            IShoppingCartStateStore shoppingCartStateStore)
        {
            this.productRepository = productRepository;
            this.shoppingCart = shoppingCart;
            this.shoppingCartStateStore = shoppingCartStateStore;
        }

        public async Task ExecuteAsync(int productId)
        {
            var product = productRepository.GetProduct(productId);
            if (product != null)
            {
                await shoppingCart.AddProductAsync(product);
                this.shoppingCartStateStore.UpdateLineItemsCount();
            }
        }

        public async void Execute(int productId)
        {
            await ExecuteAsync(productId);
        }
    }
}
