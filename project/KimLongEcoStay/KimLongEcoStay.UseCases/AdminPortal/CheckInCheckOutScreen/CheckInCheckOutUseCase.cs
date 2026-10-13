using System;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.CoreBusiness.Services;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.UseCases.AdminPortal.CheckInCheckOutScreen
{
    public interface ICheckInCheckOutUseCase
    {
        bool CheckIn(int orderId, string adminUser);
        bool CheckOut(int orderId, string adminUser);
        BookingCancellationPolicyResult CancelBooking(int orderId, string reason, string adminUser);
    }

    public class CheckInCheckOutUseCase : ICheckInCheckOutUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IBookingService _bookingService;

        public CheckInCheckOutUseCase(IOrderRepository orderRepository, IBookingService bookingService)
        {
            _orderRepository = orderRepository;
            _bookingService = bookingService;
        }

        public bool CheckIn(int orderId, string adminUser)
        {
            var order = _orderRepository.GetOrder(orderId);
            if (order == null) return false;

            order.DateProcessing = DateTime.Now;
            order.AdminUser = adminUser;
            _orderRepository.UpdateOrder(order);
            return true;
        }

        public bool CheckOut(int orderId, string adminUser)
        {
            var order = _orderRepository.GetOrder(orderId);
            if (order == null) return false;

            order.DateProcessed = DateTime.Now;
            order.AdminUser = adminUser;
            _orderRepository.UpdateOrder(order);
            return true;
        }

        public BookingCancellationPolicyResult CancelBooking(int orderId, string reason, string adminUser)
        {
            var order = _orderRepository.GetOrder(orderId);
            if (order == null)
            {
                return new BookingCancellationPolicyResult
                {
                    Message = "Không tìm thấy đơn đặt phòng để hủy."
                };
            }

            DateTime checkIn = order.DatePlaced?.AddDays(1) ?? DateTime.Today.AddDays(1);
            decimal deposit = (decimal)(order.TotalPrice * 0.5);
            decimal firstNight = order.LineItems?.Count > 0 ? (decimal)order.LineItems[0].Price : deposit;

            var policy = _bookingService.CalculateCancellationPolicy(checkIn, deposit, firstNight);

            // Cập nhật thông tin hủy trên đơn
            order.AdminUser = $"{adminUser} (Đã hủy: {reason} - Hoàn cọc: {policy.RefundAmount:N0}đ)";
            order.DateProcessed = DateTime.Now;
            _orderRepository.UpdateOrder(order);

            return policy;
        }
    }
}
