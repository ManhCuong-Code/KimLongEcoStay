using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.StateStore;
using KimLongEcoStay.UseCases.PluginInterfaces.UI;
using KimLongEcoStay.UseCases.ShoppingCartScreen.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace KimLongEcoStay.UseCases.ShoppingCartScreen
{
    public class UpdateQuantityUseCase : IUpdateQuantityUseCase
    {
        private readonly IShoppingCart shoppingCart;
        private readonly IShoppingCartStateStore shoppingCartStateStore;

        public UpdateQuantityUseCase(IShoppingCart shoppingCart, IShoppingCartStateStore shoppingCartStateStore)
        {
            this.shoppingCart = shoppingCart;
            this.shoppingCartStateStore = shoppingCartStateStore;
        }

        public async Task<Order> Execute(int productId, int quantity)
        {
            var order = await this.shoppingCart.UpdateQuantityAsync(productId, quantity);
            this.shoppingCartStateStore.UpdateLineItemsCount();

            // 3. Trả về Order mới để UI cập nhật tổng tiền
            return order;
        }
    }
}

