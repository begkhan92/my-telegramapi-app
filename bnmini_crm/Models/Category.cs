namespace bnmini_crm.Models
{
    public class Category
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;
        public bool IsDefault { get; set; } = false;
    }
}
