using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;
using KimLongEcoStay.UseCases.PluginInterfaces.UI;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace KimLongEcoStay.ShoppingCart.LocalStorage
{
    public class ShoppingCart : IShoppingCart
    {
        private const string cstrShoppingCart = "KimLongEcoStay.ShoppingCart";
        private static Order? _memoryOrder;
        private readonly IJSRuntime jSRuntime;
        private readonly IProductRepository productRepository;
        public ShoppingCart(IJSRuntime jSRuntime, IProductRepository productRepository )
        {
            this.jSRuntime = jSRuntime;
            this.productRepository = productRepository;
        }
        public async Task<Order> AddProductAsync(Product product)
        {
            var order = await GetOrder();
            order.AddProduct(product.productId, 1, product.Price);
            await SetOrder(order);
            return order;
        }

        public async Task<Order> DeleteProductAsync(int productId)
        {
            var order = await GetOrder();
            order.RemoveProduct(productId);
            await SetOrder(order);
            return order;
        }

        public Task EmptyAsync()
        {
            return this.SetOrder(null);
        }

        public async Task<Order> GetOrderAsync()
        {
            return await GetOrder();
        }

        public Task<Order> PlaceOrderAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            await this.SetOrder(order);
            return order;
        }

        public async Task<Order> UpdateQuantityAsync(int productId, int quantity)
        {
            var order = await GetOrder();
            if (quantity < 0)
                return order;
            else if (quantity == 0)
                return await DeleteProductAsync(productId);

            var lineItem = order.LineItems.SingleOrDefault(x => x.ProductId == productId);
            if (lineItem != null) lineItem.Quantity = quantity;
            await SetOrder(order);
            return order;

        }

        private async Task<Order> GetOrder()
        {
            Order? order = null;
            try
            {
                var strOrder = await jSRuntime.InvokeAsync<string>("localStorage.getItem", cstrShoppingCart);
                if (!string.IsNullOrEmpty(strOrder) && strOrder.ToLower() != "null")
                {
                    order = JsonConvert.DeserializeObject<Order>(strOrder);
                }
            }
            catch
            {
                // Fallback to in-memory order when JS interop is not ready or during SSR
            }

            if (order == null && _memoryOrder != null && _memoryOrder.LineItems != null && _memoryOrder.LineItems.Count > 0)
            {
                order = _memoryOrder;
            }

            if (order == null)
            {
                order = new Order();
                _memoryOrder = order;
                try
                {
                    await SetOrder(order);
                }
                catch { }
            }

            if (order?.LineItems != null)
            {
                foreach (var item in order.LineItems)
                {
                    item.Product = productRepository.GetProduct(item.ProductId);
                }
            }

            _memoryOrder = order;
            return order ?? new Order();
        }

        private async Task SetOrder(Order? order)
        {
            _memoryOrder = order ?? new Order();
            try
            {
                await jSRuntime.InvokeVoidAsync("localStorage.setItem", cstrShoppingCart, JsonConvert.SerializeObject(order));
            }
            catch
            {
                // Fallback silently if JS is not ready
            }
        }
    }

}
