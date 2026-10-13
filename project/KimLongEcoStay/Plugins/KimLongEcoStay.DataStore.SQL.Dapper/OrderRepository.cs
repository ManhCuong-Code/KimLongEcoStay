using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Microsoft.Data.SqlClient;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.DataStore.SQL.Dapper
{
    public class OrderRepository : IOrderRepository, IBookingRepository
    {
        private readonly IDataAccess _dataAccess;

        private static readonly List<Order> _fallbackOrders = new()
        {
            new Order
            {
                OrderID = 1,
                UniqueId = "KLE-20261015-01",
                CustomerName = "Trần Hải Đăng",
                CustomerAddress = "0912.345.678 — 45 Lê Lợi, TP. Huế",
                CustomerCity = "046098001234",
                CustomerStateProvince = "14:00 chiều",
                CustomerCountry = "Việt Nam",
                DatePlaced = DateTime.Now.AddHours(-6),
                LineItems = new List<OrderLineItem>
                {
                    new OrderLineItem
                    {
                        LineItemId = 1,
                        ProductId = 1,
                        Quantity = 2,
                        Price = 390000,
                        Product = new Product
                        {
                            Id = 1,
                            Name = "Phòng Eco-Nest Ban Mai (KLE-R101)",
                            Brand = "Nhà Vườn Hương Sen (Kim Long)",
                            Price = 390000,
                            ImageLink = "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=800&q=80"
                        }
                    }
                }
            },
            new Order
            {
                OrderID = 2,
                UniqueId = "KLE-20261016-02",
                CustomerName = "Lê Thị Mai Anh",
                CustomerAddress = "0988.765.432 — Cầu Giấy, Hà Nội",
                CustomerCity = "001198005678",
                CustomerStateProvince = "15:30 chiều",
                CustomerCountry = "Việt Nam",
                DatePlaced = DateTime.Now.AddHours(-18),
                DateProcessing = DateTime.Now.AddHours(-12),
                DateProcessed = DateTime.Now.AddHours(-10),
                AdminUser = "letan",
                LineItems = new List<OrderLineItem>
                {
                    new OrderLineItem
                    {
                        LineItemId = 2,
                        ProductId = 3,
                        Quantity = 3,
                        Price = 680000,
                        Product = new Product
                        {
                            Id = 3,
                            Name = "Garden Suite Cố Đô Thanh Trà (KLE-R201)",
                            Brand = "Nhà Vườn Nguyệt Biều (Kim Long)",
                            Price = 680000,
                            ImageLink = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=800&q=80"
                        }
                    }
                }
            }
        };

        public OrderRepository(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public int CreateOrder(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            if (string.IsNullOrEmpty(order.UniqueId))
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd");
                string randomSuffix = new Random().Next(100, 999).ToString();
                order.UniqueId = $"KLE-{timestamp}-{randomSuffix}";
            }

            order.DatePlaced = DateTime.Now;

            try
            {
                using IDbConnection connection = new SqlConnection(_dataAccess.ConnectionString);
                connection.Open();
                using IDbTransaction transaction = connection.BeginTransaction();

                string sqlOrder = @"
                    INSERT INTO [dbo].[Order] 
                    (
                        [DatePlaced], [DateProcessing], [DateProcessed],
                        [CustomerName], [CustomerAddress], [CustomerCity],
                        [CustomerStateProvince], [CustomerCountry], [AdminUser], [UniqueId]
                    )
                    VALUES 
                    (
                        @DatePlaced, @DateProcessing, @DateProcessed,
                        @CustomerName, @CustomerAddress, @CustomerCity,
                        @CustomerStateProvince, @CustomerCountry, @AdminUser, @UniqueId
                    );
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                int orderId = connection.QuerySingle<int>(sqlOrder, new
                {
                    order.DatePlaced,
                    order.DateProcessing,
                    order.DateProcessed,
                    order.CustomerName,
                    order.CustomerAddress,
                    order.CustomerCity,
                    order.CustomerStateProvince,
                    order.CustomerCountry,
                    order.AdminUser,
                    order.UniqueId
                }, transaction: transaction);

                order.OrderId = orderId;
                order.OrderID = orderId;

                string sqlLineItem = @"
                    INSERT INTO [dbo].[OrderLineItem] 
                    ([ProductId], [OrderId], [Quantity], [Price])
                    VALUES 
                    (@ProductId, @OrderId, @Quantity, @Price);";

                if (order.LineItems != null && order.LineItems.Any())
                {
                    foreach (var item in order.LineItems)
                    {
                        connection.Execute(sqlLineItem, new
                        {
                            item.ProductId,
                            OrderId = orderId,
                            item.Quantity,
                            item.Price
                        }, transaction: transaction);
                    }
                }

                transaction.Commit();
                return orderId;
            }
            catch
            {
                // Fallback In-Memory Storage khi SQL Server chưa sẵn sàng
                int fallbackId = _fallbackOrders.Count > 0 ? _fallbackOrders.Max(o => o.OrderID ?? 0) + 1 : 1;
                order.OrderId = fallbackId;
                order.OrderID = fallbackId;
                _fallbackOrders.Insert(0, order);
                return fallbackId;
            }
        }

        public Order? GetOrder(int id)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_dataAccess.ConnectionString);
                string sql = @"
                    SELECT o.OrderId as OrderID, o.*, 
                           li.LineItemId, li.ProductId, li.OrderId as LineItemOrderId, li.Quantity, li.Price,
                           p.ProductId as Id, p.*
                    FROM [dbo].[Order] o
                    LEFT JOIN [dbo].[OrderLineItem] li ON o.OrderId = li.OrderId
                    LEFT JOIN [dbo].[Product] p ON li.ProductId = p.ProductId
                    WHERE o.OrderId = @OrderId;";

                var orderDictionary = new Dictionary<int, Order>();

                connection.Query<Order, OrderLineItem, Product, Order>(
                    sql,
                    (order, lineItem, product) =>
                    {
                        if (!orderDictionary.TryGetValue(order.OrderID ?? 0, out var currentOrder))
                        {
                            currentOrder = order;
                            currentOrder.LineItems = new List<OrderLineItem>();
                            orderDictionary.Add(currentOrder.OrderID ?? 0, currentOrder);
                        }

                        if (lineItem != null)
                        {
                            lineItem.Product = product;
                            currentOrder.LineItems.Add(lineItem);
                        }

                        return currentOrder;
                    },
                    new { OrderId = id },
                    splitOn: "LineItemId,Id");

                var res = orderDictionary.Values.FirstOrDefault();
                if (res != null) return res;
            }
            catch
            {
                // Fallback
            }

            return _fallbackOrders.FirstOrDefault(o => o.OrderID == id);
        }

        public Order? GetOrderByUniqueId(string uniqueId)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_dataAccess.ConnectionString);
                string sql = @"
                    SELECT o.OrderId as OrderID, o.*, 
                           li.LineItemId, li.ProductId, li.OrderId as LineItemOrderId, li.Quantity, li.Price,
                           p.ProductId as Id, p.*
                    FROM [dbo].[Order] o
                    LEFT JOIN [dbo].[OrderLineItem] li ON o.OrderId = li.OrderId
                    LEFT JOIN [dbo].[Product] p ON li.ProductId = p.ProductId
                    WHERE LOWER(o.UniqueId) = LOWER(@UniqueId);";

                var orderDictionary = new Dictionary<int, Order>();

                connection.Query<Order, OrderLineItem, Product, Order>(
                    sql,
                    (order, lineItem, product) =>
                    {
                        if (!orderDictionary.TryGetValue(order.OrderID ?? 0, out var currentOrder))
                        {
                            currentOrder = order;
                            currentOrder.LineItems = new List<OrderLineItem>();
                            orderDictionary.Add(currentOrder.OrderID ?? 0, currentOrder);
                        }

                        if (lineItem != null)
                        {
                            lineItem.Product = product;
                            currentOrder.LineItems.Add(lineItem);
                        }

                        return currentOrder;
                    },
                    new { UniqueId = uniqueId },
                    splitOn: "LineItemId,Id");

                var res = orderDictionary.Values.FirstOrDefault();
                if (res != null) return res;
            }
            catch
            {
                // Fallback
            }

            return _fallbackOrders.FirstOrDefault(o => string.Equals(o.UniqueId, uniqueId, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<Order> GetOrders()
        {
            try
            {
                string sql = @"SELECT OrderId as OrderID, * FROM [dbo].[Order] ORDER BY DatePlaced DESC;";
                var orders = _dataAccess.Query<Order, dynamic>(sql, new { });
                if (orders != null && orders.Any())
                {
                    foreach (var o in orders)
                    {
                        o.LineItems = GetLineItemsByOrderId(o.OrderID ?? 0).ToList();
                    }
                    return orders;
                }
            }
            catch
            {
                // Fallback
            }

            return _fallbackOrders;
        }

        public IEnumerable<Order> GetOutstandingOrders()
        {
            try
            {
                string sql = @"SELECT OrderId as OrderID, * FROM [dbo].[Order] WHERE DateProcessed IS NULL ORDER BY DatePlaced DESC;";
                var orders = _dataAccess.Query<Order, dynamic>(sql, new { });
                if (orders != null && orders.Any())
                {
                    foreach (var o in orders)
                    {
                        o.LineItems = GetLineItemsByOrderId(o.OrderID ?? 0).ToList();
                    }
                    return orders;
                }
            }
            catch
            {
                // Fallback
            }

            return _fallbackOrders.Where(o => !o.DateProcessed.HasValue);
        }

        public IEnumerable<Order> GetProcessedOrders()
        {
            try
            {
                string sql = @"SELECT OrderId as OrderID, * FROM [dbo].[Order] WHERE DateProcessed IS NOT NULL ORDER BY DateProcessed DESC;";
                var orders = _dataAccess.Query<Order, dynamic>(sql, new { });
                if (orders != null && orders.Any())
                {
                    foreach (var o in orders)
                    {
                        o.LineItems = GetLineItemsByOrderId(o.OrderID ?? 0).ToList();
                    }
                    return orders;
                }
            }
            catch
            {
                // Fallback
            }

            return _fallbackOrders.Where(o => o.DateProcessed.HasValue);
        }

        public IEnumerable<OrderLineItem> GetLineItemsByOrderId(int orderId)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_dataAccess.ConnectionString);
                string sql = @"
                    SELECT li.LineItemId, li.ProductId, li.OrderId as OrderID, li.Quantity, li.Price,
                           p.ProductId as Id, p.*
                    FROM [dbo].[OrderLineItem] li
                    LEFT JOIN [dbo].[Product] p ON li.ProductId = p.ProductId
                    WHERE li.OrderId = @OrderId;";

                return connection.Query<OrderLineItem, Product, OrderLineItem>(
                    sql,
                    (lineItem, product) =>
                    {
                        lineItem.Product = product;
                        return lineItem;
                    },
                    new { OrderId = orderId },
                    splitOn: "Id");
            }
            catch
            {
                var ord = _fallbackOrders.FirstOrDefault(o => o.OrderID == orderId);
                return ord?.LineItems ?? Enumerable.Empty<OrderLineItem>();
            }
        }

        public void UpdateOrder(Order order)
        {
            try
            {
                string sql = @"UPDATE [dbo].[Order] 
                               SET [DateProcessing] = @DateProcessing,
                                   [DateProcessed] = @DateProcessed,
                                   [AdminUser] = @AdminUser
                               WHERE [OrderId] = @OrderId;";

                _dataAccess.ExecuteCommand(sql, new
                {
                    order.DateProcessing,
                    order.DateProcessed,
                    order.AdminUser,
                    OrderId = order.OrderID
                });
            }
            catch
            {
                // Fallback update
                var existing = _fallbackOrders.FirstOrDefault(o => o.OrderID == order.OrderID);
                if (existing != null)
                {
                    existing.DateProcessing = order.DateProcessing;
                    existing.DateProcessed = order.DateProcessed;
                    existing.AdminUser = order.AdminUser;
                }
            }
        }

        public Booking? GetBookingByCode(string bookingCode)
        {
            var order = GetOrderByUniqueId(bookingCode);
            if (order == null) return null;
            return new Booking
            {
                OrderID = order.OrderID,
                UniqueId = order.UniqueId,
                DatePlaced = order.DatePlaced,
                DateProcessed = order.DateProcessed,
                CustomerName = order.CustomerName,
                CustomerAddress = order.CustomerAddress,
                LineItems = order.LineItems
            };
        }
    }
}