namespace bnmini_crm.Models
{
    public class Venue
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TelegramBotToken { get; set; } = string.Empty;
        public string? WebAppUrl { get; set; }
        public string? LogoUrl { get; set; }
        public string? About { get; set; }

        public bool IsOpen { get; set; } = true;
        public string ManagerPassword { get; set; } = "";
        public string Currency { get; set; } = "USD";
        public decimal DeliveryFee { get; set; } = 0;
        public string Language { get; set; } = "en";

        public ICollection<Item> Items { get; set; } = new List<Item>();
        public ICollection<VenueUser> VenueUsers { get; set; } = new List<VenueUser>();
    }
}
