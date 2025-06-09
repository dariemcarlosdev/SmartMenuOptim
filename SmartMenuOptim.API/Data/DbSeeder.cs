using SmartMenuOptim.Shared.Models;

namespace SmartMenuOptim.API.Data
{
    public static class DbSeeder
    {
        public static void SeedData(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Check if the database is already seeded
            if (!dbContext.Reviews.Any() && !dbContext.SaleRecords.Any())
            {
                // Seed SaleRecords
                dbContext.SaleRecords.AddRange(new List<SaleRecord>
                {
                    new SaleRecord { DishName = "Pizza Margherita", QuantitySold = 50, SaleDate = DateTime.Now.AddDays(-1) },
                    new SaleRecord { DishName = "Spaghetti Carbonara", QuantitySold = 30, SaleDate = DateTime.Now.AddDays(-2) },
                    new SaleRecord { DishName = "Caesar Salad", QuantitySold = 20, SaleDate = DateTime.Now.AddDays(-3) }
                });
                // Seed Reviews
                dbContext.Reviews.AddRange(new List<Review>
                {
                    new Review { CustomerName = "Alice", Comment = "Great food!", SentimentScore = 0.9 },
                    new Review { CustomerName = "Bob", Comment = "Average service.", SentimentScore = 0.5 },
                    new Review { CustomerName = "Charlie", Comment = "Will come back again!", SentimentScore = 0.8 }
                });
                dbContext.SaveChanges();
            }
        }
    }
}
