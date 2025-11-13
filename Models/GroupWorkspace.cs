using System.ComponentModel.DataAnnotations.Schema;

namespace Class_Project_Database_Portion.Models
{
    public class GroupWorkspace
    {
        public int GroupID { get; set; }
        public Group Group { get; set; } = null!;

        public int WorkspaceID { get; set; }
        public Workspace Workspace { get; set; } = null!;
    }
}
