using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Shared.Models;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models; // Ensure OpenApi is included

namespace Backend.Api
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public static class AuthApi
    {
        private static IConfiguration _configuration;

        // Method to initialize configuration
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Define routes for registration and login
        public static void MapAuthApi(this WebApplication app)
        {
            // Initialize configuration
            Initialize(app.Services.GetRequiredService<IConfiguration>());

            // Registration endpoint
            app.MapPost("/api/auth/register", RegisterUser);

            // Login endpoint
            app.MapPost("/api/auth/login", LoginUser);
        }

        // User registration logic
        private static async Task<IResult> RegisterUser(UserModel user)
        {
            using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"); // Ensure correct DB path
            await connection.OpenAsync();

            // Check if the user already exists
            var checkUserCommand = connection.CreateCommand();
            checkUserCommand.CommandText = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
            checkUserCommand.Parameters.AddWithValue("@Email", user.Email);

            var exists = Convert.ToInt32(await checkUserCommand.ExecuteScalarAsync()) > 0;
            if (exists)
            {
                return Results.BadRequest(new { Error = "Käyttäjä on jo olemassa." });
            }

            // Hash the password using BCrypt
            user.PasswordHash = HashPassword(user.PasswordHash);

            // Insert the new user into the database
            var command = connection.CreateCommand();
            command.CommandText = @"
                   INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Address, PhoneNumber, CreatedDate, Role)
                   VALUES (@Email, @PasswordHash, @FirstName, @LastName, @Address, @PhoneNumber, CURRENT_TIMESTAMP, @Role)";
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            command.Parameters.AddWithValue("@FirstName", user.FirstName);
            command.Parameters.AddWithValue("@LastName", user.LastName);
            command.Parameters.AddWithValue("@Address", user.Address ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Role", user.Role.ToString());

            await command.ExecuteNonQueryAsync();

            return Results.Ok(new { Message = "Rekisteröinti onnistui." });
        }

        // User login logic
        private static async Task<IResult> LoginUser(LoginRequest loginRequest)
        {
            using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db"); // Ensure correct DB path
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT UserID, PasswordHash, FirstName, LastName, Email, Role FROM Users WHERE Email = @Email";
            command.Parameters.AddWithValue("@Email", loginRequest.Email);

            using (var reader = await command.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    return Results.Json(new { Error = "Käyttäjää ei löytynyt." }, statusCode: 401); // Modified
                }

                var storedHash = reader.GetString(1);
                if (!VerifyPassword(loginRequest.Password, storedHash))
                {
                    return Results.Json(new { Error = "Väärä salasana." }, statusCode: 401); // Modified
                }

                // Retrieve user details
                var userId = reader.GetInt32(0);
                var email = reader.GetString(4);
                var firstName = reader.GetString(2);
                var lastName = reader.GetString(3);
                var role = reader.GetString(5);

                // Generate JWT Token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Name, $"{firstName} {lastName}"),
                        new Claim(ClaimTypes.Role, role)
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Audience"],
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwtToken = tokenHandler.WriteToken(token);

                // Return the token in JSON format
                return Results.Ok(new
                {
                    Token = jwtToken,
                    User = new
                    {
                        UserID = userId,
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        Role = role
                    }
                });
            }
        }

        // Hash the password using BCrypt
        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Verify the password against the stored hash
        private static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(enteredPassword, storedHash);
        }

        // Role assignment logic (Implement as needed)
        private static async Task<IResult> AssignRole(/* parameters */)
        {
            // Role assignment implementation...
            return Results.Ok();
        }
    }
}