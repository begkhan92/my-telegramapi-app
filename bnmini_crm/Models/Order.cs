namespace bnmini_crm.Models
{
    public enum OrderStatus { New, Confirmed, Done, Cancelled }

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
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
