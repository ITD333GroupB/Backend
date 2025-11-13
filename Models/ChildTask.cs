using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Class_Project_Database_Portion.Models
{
    public class ChildTask
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public string Contents { get; set; } = null!;

        [Required]
        public int TaskStatus { get; set; }

        [ForeignKey("ParentTask")]
        public int ParentTaskID { get; set; }
        public Task ParentTask { get; set; } = null!;
    }
}
