using System.ComponentModel.DataAnnotations;

namespace KimLongEcoStay.Web.CustomerPortal.ViewModels
{
    public class CustomerViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên quý khách.")]
        public string? CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ / số điện thoại liên hệ.")]
        public string? CustomerAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Tỉnh / Thành phố hoặc Số CCCD.")]
        public string? CustomerCity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Quận / Huyện hoặc Giờ nhận phòng dự kiến.")]
        public string? CustomerStateProvince { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Quốc tịch.")]
        public string? CustomerCountry { get; set; }
    }
}

