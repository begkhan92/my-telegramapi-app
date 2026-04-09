namespace bnmini_crm.Models
{
    public class ItemImage
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string Url { get; set; } = "";
        public int SortOrder { get; set; }

        public Item Item { get; set; } = null!;
    }
}
