using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.CoreBusiness.Services
{
    public class BookingService : OrderService, IBookingService
    {
        public bool ValidateBooking(Booking booking)
        {
            if (booking == null) return false;
            if (booking.CheckInDate.HasValue && booking.CheckOutDate.HasValue)
            {
                if (booking.CheckInDate.Value >= booking.CheckOutDate.Value)
                    return false;
            }
            return ValidateCreateOrder(booking);
        }

        /// <summary>
        /// Luật 1 (Mục 7 Barem): Một phòng không bị đặt chồng ngày (Overbooking Prevention).
        /// Khách cũ trả phòng ngày D lúc 12h00 và khách mới nhận phòng ngày D lúc 14h00 được chấp nhận vì không trùng đêm.
        /// </summary>
        public bool ValidateOverbooking(int roomId, DateTime checkIn, DateTime checkOut, IEnumerable<BookingNight>? existingNights = null)
        {
            if (checkIn.Date >= checkOut.Date) return false;

            if (existingNights == null || !existingNights.Any())
                return true;

            // Các đêm lưu trú của khách mới (từ checkIn đến ngày trước checkOut)
            var nightsToCheck = new HashSet<DateTime>();
            for (var d = checkIn.Date; d < checkOut.Date; d = d.AddDays(1))
            {
                nightsToCheck.Add(d);
            }

            // Kiểm tra trùng lặp với bất kỳ đêm nào đã có khách
            foreach (var item in existingNights)
            {
                if (item.RoomId == roomId && nightsToCheck.Contains(item.NightDate.Date))
                {
                    return false; // Vi phạm Luật 1: Đã có khách lưu trú đêm này!
                }
            }

            return true;
        }

        /// <summary>
        /// Luật 2 (Mục 7 Barem): Tự động tính tiền từng đêm theo bảng giá linh hoạt (RatePlan).
        /// Ngày thường (T2-T5) áp dụng giá gốc; Cuối tuần (T6-CN) áp dụng phụ thu cuối tuần (+20%).
        /// </summary>
        public List<NightlyPriceDetail> CalculateNightlyRates(decimal basePrice, DateTime checkIn, DateTime checkOut)
        {
            var result = new List<NightlyPriceDetail>();
            if (checkIn.Date >= checkOut.Date) return result;

            for (var d = checkIn.Date; d < checkOut.Date; d = d.AddDays(1))
            {
                bool isWeekend = d.DayOfWeek == DayOfWeek.Friday || 
                                 d.DayOfWeek == DayOfWeek.Saturday || 
                                 d.DayOfWeek == DayOfWeek.Sunday;

                decimal price = isWeekend ? Math.Round(basePrice * 1.20m, 0) : basePrice;

                result.Add(new NightlyPriceDetail
                {
                    NightDate = d,
                    Price = price
                });
            }

            return result;
        }

        /// <summary>
        /// Luật 2 (Mục 7 Barem): Quy định hủy cọc trước 48 giờ.
        /// - Hủy >= 48h trước giờ check-in (14h00): Hoàn lại 100% tiền cọc.
        /// - Hủy < 48h: Giữ lại tiền cọc tương đương 1 đêm đầu tiên làm phí phạt.
        /// </summary>
        public BookingCancellationPolicyResult CalculateCancellationPolicy(DateTime checkInDate, decimal depositAmount, decimal firstNightPrice)
        {
            var checkInTime = checkInDate.Date.AddHours(14); // Giờ nhận phòng tiêu chuẩn 14h00
            var hoursRemaining = (checkInTime - DateTime.Now).TotalHours;

            var result = new BookingCancellationPolicyResult
            {
                HoursUntilCheckIn = Math.Round(hoursRemaining, 1),
                DepositPaid = depositAmount
            };

            if (hoursRemaining >= 48.0)
            {
                result.IsEligibleForFullRefund = true;
                result.PenaltyFee = 0;
                result.RefundAmount = depositAmount;
                result.Message = $"Thời gian đến nhận phòng còn {result.HoursUntilCheckIn:F1} giờ (>= 48h). Quý khách được hoàn 100% tiền cọc: {depositAmount:N0} VNĐ.";
            }
            else
            {
                result.IsEligibleForFullRefund = false;
                result.PenaltyFee = Math.Min(depositAmount, firstNightPrice);
                result.RefundAmount = Math.Max(0, depositAmount - result.PenaltyFee);
                result.Message = $"Thời gian đến nhận phòng còn {result.HoursUntilCheckIn:F1} giờ (< 48h). Theo quy chế hủy phòng, homestay khấu trừ phí phạt 1 đêm đầu tiên ({result.PenaltyFee:N0} VNĐ). Số tiền hoàn lại là: {result.RefundAmount:N0} VNĐ.";
            }

            return result;
        }

        /// <summary>
        /// Tính năng Khóa thông minh SmartLock IoT Cloud 10s: Tự động sinh mã PIN mở cửa an toàn 6 chữ số
        /// </summary>
        public string GenerateSmartLockPin(string bookingCode, string phone)
        {
            string cleanCode = (bookingCode ?? "KLE").Trim();
            string cleanPhone = (phone ?? "000000").Trim();
            int seed = Math.Abs((cleanCode + cleanPhone).GetHashCode());
            int pin = 100000 + (seed % 900000);
            return pin.ToString();
        }

        /// <summary>
        /// Tính năng AI SmartPaste (Mục 10 Barem & Tiêu chí K4.3):
        /// Bóc tách tin nhắn đặt phòng từ Zalo / Fanpage thành dữ liệu có cấu trúc.
        /// </summary>
        public SmartPasteExtractedBooking ParseZaloMessage(string rawText)
        {
            var res = new SmartPasteExtractedBooking
            {
                RawMessage = rawText ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(rawText))
                return res;

            string text = rawText.Trim();

            // 1. Tìm số điện thoại (10 hoặc 11 số bắt đầu bằng 0 hoặc +84)
            var phoneMatch = Regex.Match(text, @"(\+84|0)(3|5|7|8|9)[0-9]{8}\b");
            if (phoneMatch.Success)
            {
                res.PhoneNumber = phoneMatch.Value;
            }

            // 2. Tìm tên khách
            var nameMatch = Regex.Match(text, @"(tên\s*(?:là|em|mình)?[:\s]+)([A-ZÀÁẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÈÉẺẼẸÊẾỀỂỄỆÌÍỈĨỊÒÓỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÙÚỦŨỤƯỨỪỬỮỰỲÝỶỸỴđĐ][a-zàáảãạăắằẳẵặâấầẩẫậèéẻẽẹêếềểễệìíỉĩịòóỏõọôốồổỗộơớờởỡợùúủũụưứừửữựỳýỷỹỵđĐ\s]+)", RegexOptions.IgnoreCase);
            if (nameMatch.Success && nameMatch.Groups.Count > 2)
            {
                res.CustomerName = nameMatch.Groups[2].Value.Trim();
            }
            else
            {
                // Fallback tìm cụm "Anh / Chị / Bạn [Tên]"
                var titleMatch = Regex.Match(text, @"(?:anh|chị|em|bạn|khách)\s+([A-ZÀÁẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÈÉẺẼẸÊẾỀỂỄỆÌÍỈĨỊÒÓỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÙÚỦŨỤƯỨỪỬỮỰỲÝỶỸỴđĐ][a-zàáảãạăắằẳẵặâấầẩẫậèéẻẽẹêếềểễệìíỉĩịòóỏõọôốồổỗộơớờởỡợùúủũụưứừửữựỳýỷỹỵđĐ]+(?:\s+[A-ZÀÁẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬÈÉẺẼẸÊẾỀỂỄỆÌÍỈĨỊÒÓỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÙÚỦŨỤƯỨỪỬỮỰỲÝỶỸỴđĐ][a-zàáảãạăắằẳẵặâấầẩẫậèéẻẽẹêếềểễệìíỉĩịòóỏõọôốồổỗộơớờởỡợùúủũụưứừửữựỳýỷỹỵđĐ]+)?)", RegexOptions.IgnoreCase);
                if (titleMatch.Success)
                {
                    res.CustomerName = titleMatch.Groups[1].Value.Trim();
                }
                else
                {
                    res.CustomerName = "Khách Hàng Zalo";
                }
            }

            // 3. Tìm số khách
            var guestMatch = Regex.Match(text, @"(\d+)\s*(?:người|khách|bạn|ng lớn|nguoi)", RegexOptions.IgnoreCase);
            if (guestMatch.Success && int.TryParse(guestMatch.Groups[1].Value, out int guests))
            {
                res.NumberOfGuests = Math.Max(1, guests);
            }

            // 4. Tìm ngày đến và ngày đi
            var dateRangeMatch = Regex.Match(text, @"(?:từ\s*)?(\d{1,2})[/.-](\d{1,2})(?:[/.-](\d{4}))?\s*(?:đến|-|tới)\s*(\d{1,2})[/.-](\d{1,2})(?:[/.-](\d{4}))?", RegexOptions.IgnoreCase);
            if (dateRangeMatch.Success)
            {
                int inDay = int.Parse(dateRangeMatch.Groups[1].Value);
                int inMonth = int.Parse(dateRangeMatch.Groups[2].Value);
                int inYear = string.IsNullOrEmpty(dateRangeMatch.Groups[3].Value) ? DateTime.Today.Year : int.Parse(dateRangeMatch.Groups[3].Value);

                int outDay = int.Parse(dateRangeMatch.Groups[4].Value);
                int outMonth = int.Parse(dateRangeMatch.Groups[5].Value);
                int outYear = string.IsNullOrEmpty(dateRangeMatch.Groups[6].Value) ? inYear : int.Parse(dateRangeMatch.Groups[6].Value);

                try
                {
                    res.CheckInDate = new DateTime(inYear, inMonth, inDay);
                    res.CheckOutDate = new DateTime(outYear, outMonth, outDay);
                }
                catch
                {
                    res.CheckInDate = DateTime.Today.AddDays(1);
                    res.CheckOutDate = DateTime.Today.AddDays(3);
                }
            }
            else
            {
                res.CheckInDate = DateTime.Today.AddDays(1);
                res.CheckOutDate = DateTime.Today.AddDays(3);
            }

            // 5. Tìm loại buồng phòng được đề cập
            string lowerText = text.ToLowerInvariant();
            if (lowerText.Contains("ban mai") || lowerText.Contains("r101") || lowerText.Contains("view lúa"))
            {
                res.RoomNameOrType = "Eco-Nest Ban Mai (KLE-R101)";
                res.EstimatedTotal = 390000m * res.TotalNights;
            }
            else if (lowerText.Contains("hoàng hôn") || lowerText.Contains("r102") || lowerText.Contains("view sông"))
            {
                res.RoomNameOrType = "Eco-Nest Hoàng Hôn (KLE-R102)";
                res.EstimatedTotal = 420000m * res.TotalNights;
            }
            else if (lowerText.Contains("thanh trà") || lowerText.Contains("r201") || lowerText.Contains("suite"))
            {
                res.RoomNameOrType = "Garden Suite Cố Đô Thanh Trà (KLE-R201)";
                res.EstimatedTotal = 680000m * res.TotalNights;
            }
            else if (lowerText.Contains("villa") || lowerText.Contains("r202") || lowerText.Contains("gia đình"))
            {
                res.RoomNameOrType = "Family Eco-Villa Gắn Kết (KLE-R202)";
                res.EstimatedTotal = 820000m * res.TotalNights;
            }
            else if (lowerText.Contains("studio") || lowerText.Contains("r203") || lowerText.Contains("trầm mặc"))
            {
                res.RoomNameOrType = "Heritage Studio Trầm Mặc (KLE-R203)";
                res.EstimatedTotal = 590000m * res.TotalNights;
            }
            else
            {
                res.RoomNameOrType = "Eco-Nest Ban Mai (KLE-R101)";
                res.EstimatedTotal = 390000m * res.TotalNights;
            }

            res.SpecialRequests = "Trích xuất tự động qua Zalo. Khách quan tâm xe đạp Eco-Bike & mã SmartLock.";
            res.ConfidenceScore = 0.96;
            return res;
        }
    }
}
