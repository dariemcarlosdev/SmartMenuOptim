using Newtonsoft.Json.Linq;
using SmartMenuOptim.Shared.Data.Entities;

namespace SmartMenuOptim.Shared.Data
{
    public static class DbSeeder
    {
        public static void Seed(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Console.WriteLine("🌱 Seeding database...");

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
