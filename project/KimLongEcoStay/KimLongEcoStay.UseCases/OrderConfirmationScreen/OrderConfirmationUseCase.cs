using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.UseCases.OrderConfirmationScreen
{
    public class OrderConfirmationUseCase : IOrderConfirmationUseCase
    {
        private readonly IOrderRepository _orderRepository;

        public OrderConfirmationUseCase(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public Order? Execute(string uniqueId)
        {
            return _orderRepository.GetOrderByUniqueId(uniqueId);
        }
    }
}