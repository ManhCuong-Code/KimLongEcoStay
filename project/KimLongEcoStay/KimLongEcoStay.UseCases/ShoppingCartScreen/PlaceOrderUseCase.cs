using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.CoreBusiness.Services;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;
using KimLongEcoStay.UseCases.PluginInterfaces.StateStore;
using KimLongEcoStay.UseCases.PluginInterfaces.UI;
using KimLongEcoStay.UseCases.ShoppingCartScreen.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace KimLongEcoStay.UseCases.ShoppingCartScreen
{
    public class PlaceOrderUseCase : IPlaceOrderUseCase
    {
        private readonly IOrderRepository orderRepository;
        private readonly IShoppingCart shoppingCart;
        private readonly IOrderService orderService;
        private readonly IShoppingCartStateStore shoppingCartStateStore;

        public PlaceOrderUseCase(
            IOrderRepository orderRepository,
            IShoppingCart shoppingCart,
            IOrderService orderService,
            IShoppingCartStateStore shoppingCartStateStore)
        {
            this.orderRepository = orderRepository;
            this.shoppingCart = shoppingCart;
            this.orderService = orderService;
            this.shoppingCartStateStore = shoppingCartStateStore;
        }

        public async Task<string?> Execute(Order order)
        {
            // 1. Kiểm tra tính hợp lệ của đơn hàng và thông tin khách hàng qua OrderService
            if (!orderService.ValidateCreateOrder(order)) return null;

            // 2. Thiết lập ngày đặt hàng hiện tại (UTC)
            order.DatePlaced = DateTime.UtcNow;

            // 3. Lưu đơn hàng vào kho lưu trữ OrderRepository
            orderRepository.CreateOrder(order);

            // 4. Dọn sạch giỏ hàng trong LocalStorage của trình duyệt
            await shoppingCart.EmptyAsync();

            // 5. Cập nhật huy hiệu giỏ hàng trên TopNavbar về số 0
            shoppingCartStateStore.UpdateLineItemsCount();

            // 6. Trả về mã UniqueId (GUID) để chuyển sang trang xác nhận đơn hàng
            return order.UniqueId;
        }
    }
}
