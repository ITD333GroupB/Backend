using Class_Project_Database_Portion.Models;

namespace Class_Project_Database_Portion.ViewModels
{
    public class UserDashboardViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public List<GroupViewModel> GroupsOwned { get; set; } = new();
        public List<WorkspaceViewModel> WorkspacesOwned { get; set; } = new();
        public List<TaskViewModel> TasksAssigned { get; set; } = new();
        public List<MessageViewModel> UserMessages { get; set; }
    }
}
