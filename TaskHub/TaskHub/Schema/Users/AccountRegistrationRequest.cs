namespace TaskHub.Schema.Users
{
    public record class AccountRegistrationRequest
    {
        public string Username { get; init; }
        public string Email { get; init; }
        public string Password { get; init; } // MUST BE SALTED AND HASHED - NOT PLAIN TEXT - ACTUALLY THIS IS A SCHOOL PROJECT SO I MIGHT NOT ACTUALLY GO THROUGH THE TROUBLE OF SALT AND HASHING AND WE'LL JUST STORE PLAINTEXT PASSWORDS IN THE DATABASE LOL
        public DateTime AccountCreated { get; init; } = DateTime.UtcNow;
    }
}
