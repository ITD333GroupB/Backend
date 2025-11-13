using System.ComponentModel.DataAnnotations.Schema;

namespace Class_Project_Database_Portion.Models
{
    public class UserWorkspace
    {
        public int UserID { get; set; }
        public User User { get; set; } = null!;

        public int WorkspaceID { get; set; }
        public Workspace Workspace { get; set; } = null!;
    }
}
