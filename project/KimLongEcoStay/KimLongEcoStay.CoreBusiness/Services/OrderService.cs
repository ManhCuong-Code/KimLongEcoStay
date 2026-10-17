using KimLongEcoStay.CoreBusiness.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace KimLongEcoStay.CoreBusiness.Services
{
    public class OrderService : IOrderService
    {
        public bool ValidateCustomerInformation(string? name, string? address, string? city, string? province, string? country)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(address)) return false;
            return true;
        }
        public bool ValidateCreateOrder(Order order)
        {
            // Phải tồn tại 1 order
            if (order == null) return false;

            // Order ít nhất phải có LineItems
            if (order.LineItems == null || order.LineItems.Count <= 0) return false;

            foreach (var item in order.LineItems)
            {
                if (item.ProductId <= 0 ||
                    item.Price < 0 ||
                    item.Quantity <= 0) return false;
            }

            // Tự động gán mặc định nếu các trường địa lý không bắt buộc bị trống
            if (string.IsNullOrWhiteSpace(order.CustomerCity)) order.CustomerCity = "Huế";
            if (string.IsNullOrWhiteSpace(order.CustomerStateProvince)) order.CustomerStateProvince = "Thừa Thiên Huế";
            if (string.IsNullOrWhiteSpace(order.CustomerCountry)) order.CustomerCountry = "Việt Nam";

            // Validate customer info
            if (!ValidateCustomerInformation(order.CustomerName,
                order.CustomerAddress,
                order.CustomerCity,
                order.CustomerStateProvince,
                order.CustomerCountry)) return false;
            return true;
        }
        public bool ValidateUpdateOrder(Order order)
        {
            if (order == null) return false;
            if (!order.OrderID.HasValue) return false;

            // order has to have order line items
            if (order.LineItems == null || order.LineItems.Count <= 0) return false;

            // Placed Date has to be populated
            if (!order.DatePlaced.HasValue) return false;

            // Other dates
            if (order.DateProcessed.HasValue || order.DateProcessing.HasValue) return false;

            // valiate uniqueId
            if (string.IsNullOrWhiteSpace(order.UniqueId)) return false;

            // validating line items
            foreach (var item in order.LineItems)
            {
                if (item.ProductId <= 0 ||
                    item.Price < 0 ||
                    item.Quantity <= 0 ||
                    item.OrderID != order.OrderID) return false;
            }

            // Tự động gán mặc định nếu các trường địa lý bị trống
            if (string.IsNullOrWhiteSpace(order.CustomerCity)) order.CustomerCity = "Huế";
            if (string.IsNullOrWhiteSpace(order.CustomerStateProvince)) order.CustomerStateProvince = "Thừa Thiên Huế";
            if (string.IsNullOrWhiteSpace(order.CustomerCountry)) order.CustomerCountry = "Việt Nam";

            // validate customer info
            if (!ValidateCustomerInformation(order.CustomerName,
                order.CustomerAddress,
                order.CustomerCity,
                order.CustomerStateProvince,
                order.CustomerCountry)) return false;

            return true;
        }
        public bool ValidateProcessOrder(Order order)
        {
            if (!order.DateProcessed.HasValue ||
                string.IsNullOrWhiteSpace(order.AdminUser)) return false;

            return true;
        }
    }
}
