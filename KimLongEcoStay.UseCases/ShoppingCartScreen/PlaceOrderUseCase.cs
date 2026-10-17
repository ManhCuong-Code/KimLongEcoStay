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
            // 1. Tự động sinh mã UniqueId nếu chưa có
            if (string.IsNullOrEmpty(order.UniqueId))
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd");
                string randomSuffix = Random.Shared.Next(100, 999).ToString();
                order.UniqueId = $"KLE-{timestamp}-{randomSuffix}";
            }

            // 2. Kiểm tra tính hợp lệ của đơn hàng và thông tin khách hàng qua OrderService
            if (!orderService.ValidateCreateOrder(order)) return null;

            // 3. Thiết lập ngày đặt hàng hiện tại (UTC)
            order.DatePlaced = DateTime.UtcNow;

            // 4. Lưu đơn hàng vào kho lưu trữ OrderRepository
            orderRepository.CreateOrder(order);

            // 5. Dọn sạch giỏ hàng trong LocalStorage của trình duyệt
            try
            {
                await shoppingCart.EmptyAsync();
            }
            catch
            {
                // Bỏ qua lỗi giỏ hàng nếu LocalStorage chưa sẵn sàng
            }

            // 6. Cập nhật huy hiệu giỏ hàng trên TopNavbar về số 0
            try
            {
                shoppingCartStateStore.UpdateLineItemsCount();
            }
            catch
            {
            }

            // 7. Trả về mã UniqueId để chuyển sang trang xác nhận đơn hàng
            return order.UniqueId;
        }
    }
}
