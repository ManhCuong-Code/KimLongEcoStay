using System;
using System.Linq;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.CoreBusiness.Services;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.UseCases.BookingLookupScreen
{
    public class BookingLookupDetail
    {
        public Order? Order { get; set; }
        public string SmartLockPin { get; set; } = string.Empty;
        public string PinValidity { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public int BatteryLevel { get; set; } = 95;
        public BookingCancellationPolicyResult? CancellationPolicy { get; set; }
    }

    public interface ILookupBookingUseCase
    {
        BookingLookupDetail? Execute(string codeOrPhone);
    }

    public class LookupBookingUseCase : ILookupBookingUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IBookingService _bookingService;

        public LookupBookingUseCase(IOrderRepository orderRepository, IBookingService bookingService)
        {
            _orderRepository = orderRepository;
            _bookingService = bookingService;
        }

        public BookingLookupDetail? Execute(string codeOrPhone)
        {
            if (string.IsNullOrWhiteSpace(codeOrPhone))
                return null;

            string query = codeOrPhone.Trim();

            // 1. Thử tìm theo UniqueId (mã booking)
            var order = _orderRepository.GetOrderByUniqueId(query);

            // 2. Nếu không thấy, tìm theo số điện thoại trong CustomerAddress
            if (order == null)
            {
                var allOrders = _orderRepository.GetOrders();
                order = allOrders.FirstOrDefault(o => 
                    (!string.IsNullOrEmpty(o.CustomerAddress) && o.CustomerAddress.Contains(query)) ||
                    (!string.IsNullOrEmpty(o.UniqueId) && o.UniqueId.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    (o.OrderID.HasValue && o.OrderID.Value.ToString() == query));
            }

            if (order == null)
                return null;

            // Đảm bảo có LineItems
            if (order.LineItems == null || !order.LineItems.Any())
            {
                order.LineItems = _orderRepository.GetLineItemsByOrderId(order.OrderID ?? 0).ToList();
            }

            string pin = _bookingService.GenerateSmartLockPin(order.UniqueId ?? "KLE", order.CustomerAddress ?? "000000");
            string roomName = order.LineItems?.FirstOrDefault()?.Product?.Name ?? "Buồng phòng sinh thái Kim Long";

            DateTime checkInDate = order.DatePlaced?.AddDays(1) ?? DateTime.Today.AddDays(1);
            decimal deposit = (decimal)(order.TotalPrice * 0.5);
            decimal firstNight = order.LineItems?.FirstOrDefault() != null ? (decimal)order.LineItems.First().Price : deposit;

            var cancelPolicy = _bookingService.CalculateCancellationPolicy(checkInDate, deposit, firstNight);

            return new BookingLookupDetail
            {
                Order = order,
                SmartLockPin = pin,
                PinValidity = $"Hiệu lực từ 14:00 ngày {checkInDate:dd/MM/yyyy} đến 12:00 ngày kết thúc lưu trú",
                RoomName = roomName,
                BatteryLevel = 94,
                CancellationPolicy = cancelPolicy
            };
        }
    }
}
