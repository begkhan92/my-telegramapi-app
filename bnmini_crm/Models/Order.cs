namespace bnmini_crm.Models
{
   
    public enum OrderStatus
    {
        New = 0,
        Confirmed = 1,
        InTransit = 2,
        Delivered = 3,
        Cancelled = 4
    }

    public class Order
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.New;
        public string? DeliveryAddress { get; set; } // новое
        public string? Phone { get; set; }           // новое
        public string? DeliveryTime { get; set; }
        public int? TelegramMessageId { get; set; }
        public int? OperatorMessageId { get; set; }
        public string? RequestId { get; set; }
        public string? CancelReason { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
