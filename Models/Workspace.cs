using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Class_Project_Database_Portion.Models
{
    public class Workspace
    {
        [Key]
        public int ID { get; set; }

        [ForeignKey("Owner")]
        public int OwnerID { get; set; }
        public User Owner { get; set; } = null!;

        [ForeignKey("Group")]
        public int? GroupID { get; set; }
        public Group? Group { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public ICollection<Task>? Tasks { get; set; }
        public ICollection<UserWorkspace>? UserWorkspaces { get; set; }
        public ICollection<GroupWorkspace>? GroupWorkspaces { get; set; }
    }

}
