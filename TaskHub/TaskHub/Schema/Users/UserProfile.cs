using System.Text.Json.Serialization;

namespace TaskHub.Schema.Users
{
    public class UserProfile
    {
        public string Username { get; set; }
        public string UserID { get; set; }
        public string Email { get; set; }
        public string JwtToken { get; set; } // used for auth, should be retrieved upon login
        public DateTime AccountCreated { get; set; }

        [JsonConstructor]
        public UserProfile() { }

    }
}
