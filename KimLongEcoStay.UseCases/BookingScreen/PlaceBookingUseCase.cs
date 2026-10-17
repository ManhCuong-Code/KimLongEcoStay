using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.CoreBusiness.Services;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;
using KimLongEcoStay.UseCases.PluginInterfaces.UI;
using System.Threading.Tasks;

namespace KimLongEcoStay.UseCases.BookingScreen
{
    public class PlaceBookingUseCase : IPlaceBookingUseCase
    {
        private readonly IOrderService _orderService;
        private readonly IOrderRepository _orderRepository;
        private readonly IShoppingCart _shoppingCart;

        public PlaceBookingUseCase(IOrderService orderService, IOrderRepository orderRepository, IShoppingCart shoppingCart)
        {
            _orderService = orderService;
            _orderRepository = orderRepository;
            _shoppingCart = shoppingCart;
        }

        public async Task<string> Execute(Order order)
        {
            if (_orderService.ValidateCreateOrder(order))
            {
                order.DatePlaced = System.DateTime.Now;
                order.UniqueId = System.Guid.NewGuid().ToString();
                _orderRepository.CreateOrder(order);
                await _shoppingCart.EmptyAsync();
                return order.UniqueId;
            }
            return string.Empty;
        }
    }
}
