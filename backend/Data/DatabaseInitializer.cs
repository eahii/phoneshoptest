using Microsoft.Data.Sqlite;
using Shared.Models;
using System.Threading.Tasks;

namespace Backend.Data
{
    public class DatabaseInitializer
    {
        private readonly IConfiguration _configuration;

        public DatabaseInitializer(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        // This method initializes the database tables
        public static async Task Initialize()
        {
            using (var connection = new SqliteConnection("Data Source=UsedPhonesShop.db")) // Ensure correct DB
            {
                await connection.OpenAsync();

                // Create the Users table with the Role column
                var createUsersTable = connection.CreateCommand();
                createUsersTable.CommandText = @"
                       CREATE TABLE IF NOT EXISTS Users (
                           UserID INTEGER PRIMARY KEY AUTOINCREMENT, 
                           Email TEXT NOT NULL UNIQUE,                
                           PasswordHash TEXT NOT NULL,                 
                           FirstName TEXT NOT NULL,                    
                           LastName TEXT NOT NULL,                     
                           Address TEXT,                               
                           PhoneNumber TEXT,                           
                           CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP, 
                           Role TEXT NOT NULL DEFAULT 'Customer'       
                       )";
                await createUsersTable.ExecuteNonQueryAsync();

                // Create other tables (Phones, Orders, etc.) similarly...
                // Example for Phones table:
                var createPhonesTable = connection.CreateCommand();
                createPhonesTable.CommandText = @"
                       CREATE TABLE IF NOT EXISTS Phones (
                           PhoneID INTEGER PRIMARY KEY AUTOINCREMENT,
                           Brand TEXT NOT NULL,
                           Model TEXT NOT NULL,
                           Price REAL NOT NULL,
                           Description TEXT,
                           Condition TEXT NOT NULL,
                           StockQuantity INTEGER
                       )";
                await createPhonesTable.ExecuteNonQueryAsync();

                // Repeat for Orders, OrderItems, ShoppingCart, CartItems, Offers, etc.
                // Ensure all CREATE TABLE commands target UsedPhonesShop.db
            }
        }
    }
}