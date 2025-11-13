namespace Class_Project_Database_Portion.ViewModels
{
    public class WorkspaceViewModel
    {
        public int WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OwnerUsername { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public List<TaskViewModel> Tasks { get; set; } = new();
        public List<MessageViewModel> Messages { get; set; } = new();
    }
}
