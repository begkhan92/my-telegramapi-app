namespace bnmini_crm.Models
{

    public class Item
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
    }
}
