namespace KimLongEcoStay.CoreBusiness.Models
{
    public enum RoomStatus
    {
        AVAILABLE,
        OCCUPIED,
        CLEANING,
        MAINTENANCE
    }

    public class Room : Product
    {
        public int RoomId { get => ProductId; set => ProductId = value; }
        public string RoomNumber { get => Name; set => Name = value; }
        public string RoomTypeName { get => Brand; set => Brand = value; }
        public RoomStatus Status { get; set; } = RoomStatus.AVAILABLE;
    }

    public class RoomType : Product
    {
        public int RoomTypeId { get => ProductId; set => ProductId = value; }
        public string TypeName { get => Name; set => Name = value; }
        public double BasePrice { get => Price; set => Price = value; }
        public string Amenities { get => Brand; set => Brand = value; }
        public int MaxGuests { get; set; } = 2;
    }
}
