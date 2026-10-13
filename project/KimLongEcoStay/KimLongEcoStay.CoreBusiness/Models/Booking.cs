using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public enum BookingStatus
    {
        PENDING_DEPOSIT,
        CONFIRMED,
        CHECKED_IN,
        CHECKED_OUT,
        CANCELLED
    }

    public class Booking : Order
    {
        public int? BookingId { get => OrderID; set => OrderID = value; }
        public string? BookingCode { get => UniqueId; set => UniqueId = value; }
        public DateTime? CheckInDate { get => DatePlaced; set => DatePlaced = value; }
        public DateTime? CheckOutDate { get => DateProcessed; set => DateProcessed = value; }
        public string? GuestName { get => CustomerName; set => CustomerName = value; }
        public string? GuestPhone { get => CustomerAddress; set => CustomerAddress = value; }
        public BookingStatus Status { get; set; } = BookingStatus.PENDING_DEPOSIT;
        public double DepositAmount => TotalPrice * 0.5;
    }

    public class BookingNight : OrderLineItem
    {
        public int? BookingNightId { get => LineItemID; set => LineItemID = value; }
        public int? BookingId { get => OrderID; set => OrderID = value; }
        public int RoomId { get => ProductId; set => ProductId = value; }
        public int Nights { get => Quantity; set => Quantity = value; }
        public DateTime NightDate { get; set; }
    }
}
