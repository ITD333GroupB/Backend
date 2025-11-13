using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Class_Project_Database_Portion.Models
{
    public class Group
    {
        [Key]
        public int ID { get; set; }

        [ForeignKey("Owner")]
        public int? OwnerID { get; set; }
        public User? Owner { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public ICollection<Workspace>? Workspaces { get; set; }
        public ICollection<GroupWorkspace>? GroupWorkspaces { get; set; }
    }
}
