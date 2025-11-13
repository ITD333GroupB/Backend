using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Class_Project_Database_Portion.Models
{
    public class Message
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public string MessageText { get; set; } = null!;

        [ForeignKey("AuthorUser")]
        public int AuthorUserId { get; set; }
        public User AuthorUser { get; set; } = null!;

        [Required]
        public string AuthorUsername { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public int Type { get; set; }

        [Required]
        public int OwnerId { get; set; }
    }

}
