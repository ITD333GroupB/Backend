namespace Class_Project_Database_Portion.ViewModels
{
    public class GroupViewModel
    {
        public int GroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OwnerUsername { get; set; } = string.Empty;
    }
}
