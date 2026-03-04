namespace bnmini_crm.Models
{
    public class DeliveryAddress
    {
        public int Id { get; set; }
        public int AppUserId { get; set; }
        public AppUser AppUser { get; set; } = null!;
        public string Label { get; set; } = string.Empty; // "Дом", "Работа" или свободный текст
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
