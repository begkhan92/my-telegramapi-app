namespace bnmini_crm.Models
{
    public class AppUser
    {
        public int Id { get; set; }
        public long TelegramUserId { get; set; }
        public string? Username { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? Phone { get; set; } // новое поле

        public ICollection<VenueUser> VenueUsers { get; set; } = new List<VenueUser>();
        public ICollection<DeliveryAddress> Addresses { get; set; } = new List<DeliveryAddress>();
    }
}
