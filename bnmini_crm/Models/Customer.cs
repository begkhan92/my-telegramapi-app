namespace bnmini_crm.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public long TelegramUserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? ConnectionId { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }
}
