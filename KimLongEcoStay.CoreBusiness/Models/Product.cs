using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class Product
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonIgnore]
        public int ProductId { get => Id; set => Id = value; }

        [JsonIgnore]
        public int productId { get => Id; set => Id = value; }

        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public double Price { get; set; }
        public string ImageLink { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Bổ sung các thuộc tính phục vụ Đặc sản & Ẩm thực Kim Long
        public string Category { get; set; } = "Ẩm Thực";
        public int StockQuantity { get; set; } = 50;
        public string Unit { get; set; } = "phần";
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public bool IsSample { get; set; } = false; // Nhãn mẫu demo nếu chưa có dữ liệu thật
        public string PartnerId { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
    }
}
