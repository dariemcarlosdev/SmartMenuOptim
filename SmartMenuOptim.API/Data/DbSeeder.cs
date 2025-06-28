using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Npgsql;
using SmartMenuOptim.Shared.Data.Context;
using SmartMenuOptim.Shared.Data.Entities;

namespace SmartMenuOptim.API.Data
{
    public static class DbSeeder
    {
        public static void Seed(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Console.WriteLine("🌱 Seeding database...");

            // adding retry logic for database connection. This ensures that the application waits for the database to be ready before applying migrations or seeding data.

            const int maxRetries = 10;
            int retryCount = 0;
            bool dbReady = false;

            while (!dbReady && retryCount < maxRetries)
            {
                try
                {
                    
                    dbContext.Database.Migrate(); // Ensure schema is created
                    dbReady = true;
                }
                catch (NpgsqlException ex)
                {
                    retryCount++;
                    Console.WriteLine($"⏳ Waiting for database connection... attempt {retryCount}/{maxRetries} - { ex.Message}");
                    Thread.Sleep(3000);
                }
            }
            if (!dbReady)
                throw new Exception("❌ Could not connect to DB or apply migrations after retries.");

            // Check if the database is already seeded
            if (!dbContext.SaleRecords.Any())
            {
                // Seed SaleRecords
                dbContext.SaleRecords.AddRange(
                
                    new SaleRecord { DishName = "Pizza Margherita", QuantitySold = 50, SaleDate = DateTime.Today.AddDays(-1).ToUniversalTime() },
                    new SaleRecord { DishName = "Spaghetti Carbonara", QuantitySold = 30, SaleDate = DateTime.Today.AddDays(-2).ToUniversalTime() },
                    new SaleRecord { DishName = "Caesar Salad", QuantitySold = 20, SaleDate = DateTime.Today.AddDays(-3).ToUniversalTime() }
                );  
            }
            if (!dbContext.Reviews.Any())
            {
                // Seed Reviews
                dbContext.Reviews.AddRange(                
                    new Review { CustomerName = "Alice", Comment = "Great food!", SentimentScore = 0.9 },
                    new Review { CustomerName = "Bob", Comment = "Average service.", SentimentScore = 0.5 },
                    new Review { CustomerName = "Charlie", Comment = "Will come back again!", SentimentScore = 0.8 }
                );
            }

            // Save changes to the database
            dbContext.SaveChanges();

            Console.WriteLine("✅ Done seeding database.");
        }
    }
}
