using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Class_Project_Database_Portion.Models
{
    public class Task
    {
        [Key]
        public int TaskID { get; set; }

        [ForeignKey("Group")]
        public int? GroupID { get; set; }
        public Group? Group { get; set; }

        [ForeignKey("Workspace")]
        public int WorkspaceID { get; set; }
        public Workspace Workspace { get; set; } = null!;

        public string? Assignee { get; set; }

        [Required]
        public int TaskStatus { get; set; }

        [Required]
        public string TaskContents { get; set; } = null!;

        [Required]
        public string TaskName { get; set; } = null!;

        public ICollection<ChildTask>? ChildTasks { get; set; }
    }
}
