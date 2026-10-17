namespace KimLongEcoStay.CoreBusiness.Models
{
    public class OrderLineItem
    {
        public int? LineItemID { get; set; }
        public int? LineItemId { get => LineItemID; set => LineItemID = value; }
        public int? OrderID { get; set; }
        public int? OrderId { get => OrderID; set => OrderID = value; }
        public int ProductId { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public Product? Product { get; set; }

    }
}
