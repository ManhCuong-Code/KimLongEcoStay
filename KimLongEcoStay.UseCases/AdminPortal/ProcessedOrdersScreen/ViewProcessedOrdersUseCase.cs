using System.Collections.Generic;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.UseCases.AdminPortal.ProcessedOrdersScreen
{
    public class ViewProcessedOrdersUseCase : IViewProcessedOrdersUseCase
    {
        private readonly IOrderRepository _orderRepository;

        public ViewProcessedOrdersUseCase(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public IEnumerable<Order> Execute()
        {
            return _orderRepository.GetProcessedOrders();
        }
    }
}