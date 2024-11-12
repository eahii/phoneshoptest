// shared/Models/LoginResponse.cs
namespace Shared.Models
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public UserModel User { get; set; }
    }
}