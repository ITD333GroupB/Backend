namespace Class_Project_Database_Portion.ViewModels
{
    public class MessageViewModel
    {
        public int Id { get; set; }
        public string MessageContent { get; set; } = string.Empty;
        public string AuthorUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Type { get; set; }
        public string OwnerUsername { get; set; } = string.Empty;
    }
}
