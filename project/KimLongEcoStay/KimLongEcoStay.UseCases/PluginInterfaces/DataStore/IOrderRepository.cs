using KimLongEcoStay.CoreBusiness.Models;
using System.Collections.Generic;

namespace KimLongEcoStay.UseCases.PluginInterfaces.DataStore
{
    public interface IOrderRepository
    {
        int CreateOrder(Order order);
        Order? GetOrder(int id);
        Order? GetOrderByUniqueId(string uniqueId);
        void UpdateOrder(Order order);
        IEnumerable<Order> GetOrders();
        IEnumerable<Order> GetOutstandingOrders();
        IEnumerable<Order> GetProcessedOrders();
        IEnumerable<OrderLineItem> GetLineItemsByOrderId(int orderId);
    }
}
