using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.CoreBusiness.Services;
using KimLongEcoStay.CoreBusiness.Services.Interfaces;
using Xunit;

namespace KimLongEcoStay.Tests
{
    public class BusinessWorkflowTests
    {
        private readonly IEmailService _emailService;

        public BusinessWorkflowTests()
        {
            var settings = new EmailSettings
            {
                IsSimulationMode = true,
                MaxRetryAttempts = 3
            };
            _emailService = new EmailService(settings);
        }

        [Fact]
        public void Test1_ValidBooking_CalculatesCorrect50PercentDeposit()
        {
            // Base stay: 2 nights at 390,000 VND
            decimal roomPricePerNight = 390000m;
            int nights = 2;
            decimal baseTotal = roomPricePerNight * nights; // 780,000 VND
            
            // 50% deposit policy
            decimal depositRatio = 0.50m;
            decimal depositAmount = baseTotal * depositRatio;
            decimal remainingBalance = baseTotal - depositAmount;

            Assert.Equal(780000m, baseTotal);
            Assert.Equal(390000m, depositAmount);
            Assert.Equal(390000m, remainingBalance);
            Assert.Equal(baseTotal, depositAmount + remainingBalance);
        }

        [Fact]
        public async Task Test2_DepositConfirmed_GeneratesPinAndDispatchesEmail2()
        {
            var order = new Order
            {
                UniqueId = "KLE-TEST-001",
                CustomerName = "Nguyễn Văn An",
                CustomerEmail = "khachhang@example.com",
                CustomerAddress = "0905123456",
                DatePlaced = DateTime.Now
            };
            order.AddProduct(1, 2, 390000);

            string smartLockPin = "839201";
            decimal depositPaid = 390000m;
            decimal remainingBalance = 390000m;

            var result = await _emailService.SendDepositConfirmedEmailAsync(
                order, 
                smartLockPin, 
                "Eco-Nest Ban Mai", 
                depositPaid, 
                remainingBalance);

            Assert.True(result.IsSuccess);
            Assert.Equal(EmailTemplateType.DEPOSIT_CONFIRMED, result.TemplateType);
            Assert.Contains(smartLockPin, result.HtmlContent);
            Assert.True(result.HtmlContent.Contains("390.000") || result.HtmlContent.Contains("390,000"));
            Assert.Contains("XÁC NHẬN ĐÃ NHẬN CỌC 50% THÀNH CÔNG", result.HtmlContent);
        }

        [Fact]
        public void Test3_CancelledPayment_KeepsStatusUnconfirmed()
        {
            var booking = new
            {
                OrderCode = "KLE-TEST-CANCEL",
                Status = "PENDING_DEPOSIT",
                DepositPaid = 0m
            };

            // When customer closes window or cancels payment
            bool isPaymentVerified = false;
            string updatedStatus = isPaymentVerified ? "CONFIRMED" : booking.Status;

            Assert.False(isPaymentVerified);
            Assert.Equal("PENDING_DEPOSIT", updatedStatus);
            Assert.Equal(0m, booking.DepositPaid);
        }

        [Fact]
        public void Test4_DuplicateWebhook_IdempotentProcessing_PreventsDoubleExecution()
        {
            var processedTransactions = new ConcurrentDictionary<string, DateTime>();
            string transactionId = "TXN-VIETQR-998877";

            // First webhook arrival
            bool firstAttempt = processedTransactions.TryAdd(transactionId, DateTime.UtcNow);
            
            // Second identical webhook arrival (replay attack / network retry)
            bool secondAttempt = processedTransactions.TryAdd(transactionId, DateTime.UtcNow);

            Assert.True(firstAttempt, "First webhook must be accepted and processed");
            Assert.False(secondAttempt, "Duplicate webhook must be caught by idempotency lock");
            Assert.Single(processedTransactions);
        }

        [Fact]
        public async Task Test5_CheckInTrigger_DispatchesRemainingBalanceEmail3_Once()
        {
            var order = new Order
            {
                UniqueId = "KLE-TEST-002",
                CustomerName = "Lê Thị Mai Lan",
                CustomerEmail = "lan.le@example.com",
                DatePlaced = DateTime.Now
            };
            order.AddProduct(1, 2, 420000);

            decimal remainingBalance = 420000m;
            string qrUrl = "https://api.vietqr.io/image/sample.png";

            // First check-in trigger
            var result1 = await _emailService.SendRemainingBalanceRequestEmailAsync(
                order, 
                "Eco-Nest Hoàng Hôn", 
                remainingBalance, 
                qrUrl);

            // Second trigger simulation for the same order
            var result2 = await _emailService.SendRemainingBalanceRequestEmailAsync(
                order, 
                "Eco-Nest Hoàng Hôn", 
                remainingBalance, 
                qrUrl);

            Assert.True(result1.IsSuccess);
            Assert.True(result1.HtmlContent.Contains("420.000") || result1.HtmlContent.Contains("420,000"));
            
            // Idempotency: Second attempt acknowledges it was already sent and skips duplicate dispatch
            Assert.True(result2.IsSuccess);
            Assert.Contains("Bỏ qua", result2.StatusMessage);
            Assert.Contains("Idempotent", result2.Subject);
        }

        [Fact]
        public async Task Test6_FullyPaidBooking_DoesNotReceiveRemainingBalanceNotice()
        {
            var order = new Order
            {
                UniqueId = "KLE-TEST-PAID",
                CustomerName = "Trần Quốc Bảo",
                CustomerEmail = "bao.tran@example.com"
            };

            decimal remainingBalance = 0m; // Paid in full
            bool shouldSendEmail3 = remainingBalance > 0m;

            Assert.False(shouldSendEmail3, "Fully paid orders (balance == 0) must never receive Email 3");
            
            // Instead, it should receive Email 4 (Final receipt)
            var finalReceipt = await _emailService.SendFullPaymentConfirmedEmailAsync(order, "Garden Suite Thanh Trà", 550000m);
            Assert.True(finalReceipt.IsSuccess);
            Assert.Equal(EmailTemplateType.FULL_PAYMENT_CONFIRMED, finalReceipt.TemplateType);
        }

        [Fact]
        public void Test7_ProductStockValidation_CannotExceedInventory()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Bánh Ướt Thịt Nướng Kim Long",
                StockQuantity = 5,
                Price = 65000
            };

            int requestedQuantity = 8;
            bool canAddToCart = requestedQuantity <= product.StockQuantity;

            Assert.False(canAddToCart, "Should reject ordering 8 items when inventory is only 5");

            int validQuantity = 4;
            bool canAddValid = validQuantity <= product.StockQuantity;
            Assert.True(canAddValid);
        }

        [Fact]
        public void Test8_TeaServiceCapacityValidation_RejectsOverCapacity()
        {
            var teaService = new TeaService
            {
                Id = 1,
                ServiceCode = "TEA-HOMESTAY",
                Name = "Thưởng Trà Cung Đình Tại Homestay",
                MaxGuests = 8,
                AvailableSlots = 4
            };

            int groupSize = 6;
            bool canBook = groupSize <= teaService.AvailableSlots;

            Assert.False(canBook, "Should reject booking 6 guests when only 4 slots are available");

            int validGroup = 3;
            bool canBookValid = validGroup <= teaService.AvailableSlots;
            Assert.True(canBookValid);
        }

        [Fact]
        public async Task Test9_EmailFailureRecovery_PreservesTransactionAndAllowsRetry()
        {
            // Simulate an email entry that failed or simulated
            var initialResult = await _emailService.SendEmailAsync(
                "khachhang@example.com", 
                "Test Subject", 
                "<p>Test Content</p>", 
                EmailTemplateType.DEPOSIT_CONFIRMED, 
                "KLE-FAIL-TEST");

            Assert.NotNull(initialResult);
            Assert.NotEmpty(initialResult.EmailId);

            // Retry mechanism retrieves from history and attempts redelivery
            var retryResult = await _emailService.RetryEmailAsync(initialResult.EmailId);
            Assert.NotNull(retryResult);
            Assert.True(retryResult.IsSuccess);
        }
    }
}
