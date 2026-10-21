using System;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public enum PaymentTransactionType
    {
        DEPOSIT_50,          // Tiền đặt cọc 50%
        REMAINING_BALANCE,   // Thanh toán 50% tiền còn lại khi nhận phòng
        FULL_PAYMENT,        // Thanh toán 100%
        SPECIALTY_ORDER,     // Đơn hàng ẩm thực & đặc sản
        TEA_SERVICE          // Đặt trải nghiệm thưởng trà
    }

    public enum PaymentTransactionStatus
    {
        PENDING_VERIFICATION, // Khách vừa đặt lệnh / quét QR (chưa đối soát)
        VERIFIED_SUCCESS,     // Đã đối soát thành công qua cổng thanh toán / ngân hàng / quản trị viên
        REJECTED_FAILED,      // Thất bại hoặc hủy giao dịch
        REFUNDED              // Hoàn tiền
    }

    public enum PaymentChannel
    {
        VIETQR_BANK_TRANSFER, // Chuyển khoản QR ngân hàng (Nội dung mã đặt phòng)
        ONLINE_GATEWAY,       // Cổng thanh toán trực tuyến (VNPAY / MoMo / Stripe)
        CASH_AT_COUNTER       // Tiền mặt tại quầy lễ tân
    }

    public class PaymentTransaction
    {
        public int Id { get; set; }
        public string TransactionCode { get; set; } = string.Empty; // Mã giao dịch duy nhất
        public string OrderOrBookingCode { get; set; } = string.Empty; // Mã đặt phòng hoặc mã đơn hàng
        public int? BookingId { get; set; }
        public decimal Amount { get; set; }
        public PaymentTransactionType TransactionType { get; set; } = PaymentTransactionType.DEPOSIT_50;
        public PaymentChannel PaymentMethod { get; set; } = PaymentChannel.VIETQR_BANK_TRANSFER;
        public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.PENDING_VERIFICATION;
        public string TransferContent { get; set; } = string.Empty; // Nội dung CK: KLE <OrderCode> COC50
        public string BankName { get; set; } = "MBBank (Ngân hàng Quân Đội)";
        public string BankAccountNumber { get; set; } = "0905123456";
        public string AccountHolder { get; set; } = "KIM LONG ECOSTAY";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
        public string VerifiedBy { get; set; } = string.Empty; // Quản trị viên / Hệ thống Webhook
        public string ExternalReferenceId { get; set; } = string.Empty; // Mã tham chiếu từ ngân hàng
        public string Notes { get; set; } = string.Empty;
    }
}
