namespace Shared.Models
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty; // Email of the user
        public string Password { get; set; } = string.Empty; // Password of the user
    }
}
