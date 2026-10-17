using System;
using System.Collections.Generic;
using System.Linq;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.DataStore.HandCoded
{
    public class OrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders;
        private readonly IProductRepository _productRepository;
        private int _currentId = 1;

        public OrderRepository(IProductRepository productRepository)
        {
            _productRepository = productRepository;
            _orders = new List<Order>();
        }

        public int CreateOrder(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            order.OrderId = _currentId++;
            order.DatePlaced = DateTime.Now;
            order.UniqueId = Guid.NewGuid().ToString();

            // Link products
            if (order.LineItems != null)
            {
                foreach (var li in order.LineItems)
                {
                    li.OrderID = order.OrderId;
                    li.Product = _productRepository.GetProduct(li.ProductId);
                }
            }

            _orders.Add(order);
            return order.OrderId.Value;
        }

        public Order? GetOrder(int id)
        {
            var order = _orders.FirstOrDefault(x => x.OrderId == id);
            if (order != null && order.LineItems != null)
            {
                foreach (var li in order.LineItems)
                {
                    if (li.Product == null)
                        li.Product = _productRepository.GetProduct(li.ProductId);
                }
            }
            return order;
        }

        public Order? GetOrderByUniqueId(string uniqueId)
        {
            var order = _orders.FirstOrDefault(x => string.Equals(x.UniqueId, uniqueId, StringComparison.OrdinalIgnoreCase));
            if (order != null && order.LineItems != null)
            {
                foreach (var li in order.LineItems)
                {
                    if (li.Product == null)
                        li.Product = _productRepository.GetProduct(li.ProductId);
                }
            }
            return order;
        }

        public IEnumerable<Order> GetOrders()
        {
            return _orders;
        }

        public IEnumerable<Order> GetOutstandingOrders()
        {
            return _orders.Where(x => x.DateProcessed == null);
        }

        public IEnumerable<Order> GetProcessedOrders()
        {
            return _orders.Where(x => x.DateProcessed != null);
        }

        public IEnumerable<OrderLineItem> GetLineItemsByOrderId(int orderId)
        {
            var order = GetOrder(orderId);
            return order?.LineItems ?? Enumerable.Empty<OrderLineItem>();
        }

        public void UpdateOrder(Order order)
        {
            if (order == null) return;
            var existing = GetOrder(order.OrderId ?? 0);
            if (existing != null)
            {
                existing.AdminUser = order.AdminUser;
                existing.DateProcessing = order.DateProcessing;
                existing.DateProcessed = order.DateProcessed;
            }
        }
    }
}