using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Class_Project_Database_Portion.Models
{

    public class User
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public DateTime AccountCreated { get; set; }

        public ICollection<Group>? OwnedGroups { get; set; }
        public ICollection<Workspace>? OwnedWorkspaces { get; set; }
        public ICollection<UserWorkspace>? WorkspaceMemberships { get; set; }
        public ICollection<Message>? Messages { get; set; }
    }

}
