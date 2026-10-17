using System;
using System.Collections.Generic;
using System.Text;

namespace KimLongEcoStay.CoreBusiness.Models
{
    public class Product
    {
        public int productId { get; set; }
        public int ProductId { get => productId; set => productId = value; }
        public int Id { get => productId; set => productId = value; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public double Price { get; set; }
        public string ImageLink { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
