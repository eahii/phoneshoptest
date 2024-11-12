using Microsoft.Data.Sqlite;
using Shared.Models;
using System.Security.Claims;

namespace Backend.Api
{
    public static class UserApi
    {
        public static void MapUserApi(this WebApplication app)
        {
            app.MapGet("/api/users", GetUsers)
                .RequireAuthorization("RequireAdminRole");
        }

        static async Task<IResult> GetUsers(HttpContext context)
        {
            var user = context.User;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            // Further role validation if needed
            if (!user.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT UserID, Email, FirstName, LastName, Address, PhoneNumber, CreatedDate, Role FROM Users";

            var users = new List<UserModel>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new UserModel
                {
                    UserID = reader.GetInt32(0),
                    Email = reader.GetString(1),
                    FirstName = reader.GetString(2),
                    LastName = reader.GetString(3),
                    Address = reader.IsDBNull(4) ? null : reader.GetString(4),
                    PhoneNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
                    CreatedDate = reader.GetDateTime(6),
                    Role = reader.GetString(7)
                });
            }

            return Results.Ok(users);
        }
    }
}