using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.UI;
using KimLongEcoStay.UseCases.ShoppingCartScreen.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace KimLongEcoStay.UseCases.ShoppingCartScreen
{
    public class ViewShoppingCartUseCase : IViewShoppingCartUseCase
    {
        private readonly IShoppingCart shoppingCart;
        public ViewShoppingCartUseCase(IShoppingCart shoppingCart)
        {
            this.shoppingCart = shoppingCart;
        }
        public Task<Order> Execute()
        {
            return shoppingCart.GetOrderAsync();
        }
    }
}
