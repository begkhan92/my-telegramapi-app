namespace bnmini_crm.Models
{
    public enum VenueRole { Customer, Operator, Boss }
    public class VenueUser
    {
        public int Id { get; set; }

        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;

        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;

        public VenueRole Role { get; set; } = VenueRole.Customer;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
