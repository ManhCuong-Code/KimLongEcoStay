using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class RatePlan
    {
        public int Id { get; set; }
        public int RoomTypeId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public decimal Price { get; set; }
        public bool IsWeekend { get; set; }
        public decimal HolidaySurcharge { get; set; } = 0;
    }

    public class NightlyPriceDetail
    {
        public DateTime NightDate { get; set; }
        public DayOfWeek DayOfWeek => NightDate.DayOfWeek;
        public bool IsWeekend => DayOfWeek == DayOfWeek.Friday || DayOfWeek == DayOfWeek.Saturday || DayOfWeek == DayOfWeek.Sunday;
        public decimal Price { get; set; }
        public string Note => IsWeekend ? "Cuối tuần (+20%)" : "Ngày thường";
    }

    public class BookingCancellationPolicyResult
    {
        public bool IsEligibleForFullRefund { get; set; }
        public double HoursUntilCheckIn { get; set; }
        public decimal DepositPaid { get; set; }
        public decimal PenaltyFee { get; set; }
        public decimal RefundAmount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
