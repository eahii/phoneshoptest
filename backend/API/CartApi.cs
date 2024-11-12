using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Shared.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims; // Added for ClaimTypes

namespace Backend.Api
{
    public static class CartApi
    {
        public static void MapCartApi(this WebApplication app)
        {
            app.MapGet("/api/cart", GetCart);
            app.MapPost("/api/cart/items", AddToCart);
            app.MapPut("/api/cart/items/{id}", UpdateCartItem);
            app.MapDelete("/api/cart/items/{id}", RemoveFromCart);
        }

        private static async Task<IResult> GetCart(HttpContext context)
        {
            var user = context.User;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Results.Unauthorized();
            }

            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
                await connection.OpenAsync();

                // First, ensure user has a cart
                var cartId = await EnsureUserHasCart(connection, userId);

                // Retrieve cart items
                var cartItems = new List<CartItemModel>();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT 
                        CartItems.CartItemID,
                        CartItems.Quantity,
                        Phones.PhoneID,
                        Phones.Brand,
                        Phones.Model,
                        Phones.Price,
                        Phones.Description,
                        Phones.Condition,
                        Phones.StockQuantity
                    FROM CartItems
                    JOIN Phones ON CartItems.PhoneID = Phones.PhoneID
                    WHERE CartItems.CartID = @CartID";

                command.Parameters.AddWithValue("@CartID", cartId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    cartItems.Add(new CartItemModel
                    {
                        CartItemID = reader.GetInt32(0),
                        Quantity = reader.GetInt32(1),
                        PhoneID = reader.GetInt32(2),
                        Phone = new PhoneModel
                        {
                            PhoneID = reader.GetInt32(2),
                            Brand = reader.GetString(3),
                            Model = reader.GetString(4),
                            Price = reader.GetDecimal(5),
                            Description = reader.GetString(6),
                            Condition = reader.GetString(7),
                            StockQuantity = reader.GetInt32(8)
                        }
                    });
                }

                return Results.Ok(cartItems);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error getting cart: {ex.Message}");
            }
        }

        private static async Task<IResult> AddToCart(CartItemModel cartItem, HttpContext context)
        {
            var user = context.User;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                return Results.Unauthorized();
            }

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
                await connection.OpenAsync();

                // Ensure user has a cart
                var cartId = await EnsureUserHasCart(connection, userId);

                // Check if the item already exists in the cart
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "SELECT CartItemID, Quantity FROM CartItems WHERE CartID = @CartID AND PhoneID = @PhoneID";
                checkCommand.Parameters.AddWithValue("@CartID", cartId);
                checkCommand.Parameters.AddWithValue("@PhoneID", cartItem.PhoneID);

                using var reader = await checkCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    // Item exists, update quantity
                    int existingCartItemId = reader.GetInt32(0);
                    int existingQuantity = reader.GetInt32(1);
                    reader.Close();

                    var updateCommand = connection.CreateCommand();
                    updateCommand.CommandText = "UPDATE CartItems SET Quantity = @Quantity WHERE CartItemID = @CartItemID";
                    updateCommand.Parameters.AddWithValue("@Quantity", existingQuantity + cartItem.Quantity);
                    updateCommand.Parameters.AddWithValue("@CartItemID", existingCartItemId);

                    await updateCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    // Item does not exist, insert new
                    reader.Close();

                    var insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = "INSERT INTO CartItems (CartID, PhoneID, Quantity) VALUES (@CartID, @PhoneID, @Quantity)";
                    insertCommand.Parameters.AddWithValue("@CartID", cartId);
                    insertCommand.Parameters.AddWithValue("@PhoneID", cartItem.PhoneID);
                    insertCommand.Parameters.AddWithValue("@Quantity", cartItem.Quantity);

                    await insertCommand.ExecuteNonQueryAsync();
                }

                return Results.Ok();
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error adding to cart: {ex.Message}");
            }
        }

        // Other methods (UpdateCartItem, RemoveFromCart, EnsureUserHasCart)...


        private static async Task<int> EnsureUserHasCart(SqliteConnection connection, int userId)
        {
            var command = connection.CreateCommand();
            command.CommandText = "SELECT CartID FROM ShoppingCart WHERE UserID = @UserID";
            command.Parameters.AddWithValue("@UserID", userId);

            var cartId = await command.ExecuteScalarAsync();

            if (cartId == null)
            {
                command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO ShoppingCart (UserID) 
                    VALUES (@UserID);
                    SELECT last_insert_rowid();";
                command.Parameters.AddWithValue("@UserID", userId);
                cartId = await command.ExecuteScalarAsync();
            }

            return Convert.ToInt32(cartId);
        }
    }
}