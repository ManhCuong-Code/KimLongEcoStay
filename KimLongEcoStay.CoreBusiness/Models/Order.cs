using System;
using System.Collections.Generic;
using System.Linq;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class Order
    {
        public Order()
        {
            LineItems = new List<OrderLineItem>();
        }
        public int? OrderID { get; set; }
        public int? OrderId { get => OrderID; set => OrderID = value; }
        public DateTime? DatePlaced { get; set; }
        public DateTime? DateProcessed { get; set; }
        public DateTime? DateProcessing { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerCity { get; set; }
        public string? CustomerStateProvince { get; set; }
        public string? CustomerCountry { get; set; }
        public string? AdminUser { get; set; }
        public List<OrderLineItem> LineItems { get; set; }
        public string? UniqueId { get; set; }

        public int LineItemCount => LineItems?.Sum(x => x.Quantity) ?? 0;
        public double TotalPrice => LineItems?.Sum(x => x.Price * x.Quantity) ?? 0;

        public void AddProduct(int productId, int qty, double price)
        {
            var item = LineItems.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
                item.Quantity += qty;
            else
                LineItems.Add(new OrderLineItem { ProductId = productId, Quantity = qty, Price = price, OrderID = OrderID });
        }

        public void RemoveProduct(int productId)
        {
            var item = LineItems.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
                LineItems.Remove(item);
        }
    }
}