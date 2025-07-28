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

            const int maxRetries = 10;
            int retryCount = 0;
            bool dbReady = false;

            while (!dbReady && retryCount < maxRetries)
            {
                try
                {
                    dbContext.Database.Migrate();
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

            // Seed Categories
            if (!dbContext.Categories.Any())
            {
                var italian = new Category { Name = "Italian" };
                var salad = new Category { Name = "Salad" };
                dbContext.Categories.AddRange(italian, salad);
                dbContext.SaveChanges();
            }

            // Seed Dishes
            if (!dbContext.Dishes.Any())
            {
                var italian = dbContext.Categories.FirstOrDefault(c => c.Name == "Italian");
                var salad = dbContext.Categories.FirstOrDefault(c => c.Name == "Salad");
                var pizza = new Dish { Name = "Pizza Margherita", CategoryId = italian!.Id };
                var spaghetti = new Dish { Name = "Spaghetti Carbonara", CategoryId = italian!.Id };
                var caesar = new Dish { Name = "Caesar Salad", CategoryId = salad!.Id };
                dbContext.Dishes.AddRange(pizza, spaghetti, caesar);
                dbContext.SaveChanges();
            }

            // Seed Customers
            if (!dbContext.Customers.Any())
            {
                var alice = new Customer {
                    Name = "Alice",
                    Email = "alice@example.com",
                    Username = "aliceuser",
                    PasswordHash = "hashedpassword1", // Replace with real hash in production
                    IsActive = true,
                    DateRegistered = DateTime.UtcNow.AddDays(-10),
                    Role = "User"
                };
                var bob = new Customer {
                    Name = "Bob",
                    Email = "bob@example.com",
                    Username = "bobuser",
                    PasswordHash = "hashedpassword2",
                    IsActive = true,
                    DateRegistered = DateTime.UtcNow.AddDays(-8),
                    Role = "User"
                };
                var charlie = new Customer {
                    Name = "Charlie",
                    Email = "charlie@example.com",
                    Username = "charlieuser",
                    PasswordHash = "hashedpassword3",
                    IsActive = false,
                    DateRegistered = DateTime.UtcNow.AddDays(-5),
                    Role = "User"
                };
                dbContext.Customers.AddRange(alice, bob, charlie);
                dbContext.SaveChanges();
            }

            // Seed SaleRecords
            if (!dbContext.SaleRecords.Any())
            {
                var pizza = dbContext.Dishes.FirstOrDefault(d => d.Name == "Pizza Margherita");
                var spaghetti = dbContext.Dishes.FirstOrDefault(d => d.Name == "Spaghetti Carbonara");
                var caesar = dbContext.Dishes.FirstOrDefault(d => d.Name == "Caesar Salad");
                dbContext.SaleRecords.AddRange(
                    new SaleRecord { DishId = pizza!.Id, QuantitySold = 50, SaleDate = DateTime.Today.AddDays(-1).ToUniversalTime() },
                    new SaleRecord { DishId = spaghetti!.Id, QuantitySold = 30, SaleDate = DateTime.Today.AddDays(-2).ToUniversalTime() },
                    new SaleRecord { DishId = caesar!.Id, QuantitySold = 20, SaleDate = DateTime.Today.AddDays(-3).ToUniversalTime() }
                );
            }

            // Seed Reviews
            if (!dbContext.Reviews.Any())
            {
                var pizza = dbContext.Dishes.FirstOrDefault(d => d.Name == "Pizza Margherita");
                var spaghetti = dbContext.Dishes.FirstOrDefault(d => d.Name == "Spaghetti Carbonara");
                var caesar = dbContext.Dishes.FirstOrDefault(d => d.Name == "Caesar Salad");
                var alice = dbContext.Customers.FirstOrDefault(c => c.Name == "Alice");
                var bob = dbContext.Customers.FirstOrDefault(c => c.Name == "Bob");
                var charlie = dbContext.Customers.FirstOrDefault(c => c.Name == "Charlie");
                
                dbContext.Reviews.AddRange(
                    // Linked reviews
                    new Review { CustomerId = alice!.Id, Customer = alice, Comment = "Great food!", SentimentScore = 0.9, DishId = pizza!.Id, DateCreated = DateTime.UtcNow.AddDays(-3), Rating = 5 },
                    new Review { CustomerId = bob!.Id, Customer = bob, Comment = "Average service.", SentimentScore = 0.5, DishId = spaghetti!.Id, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 3 },
                    new Review { CustomerId = charlie!.Id, Customer = charlie, Comment = "Will come back again!", SentimentScore = 0.8, DishId = caesar!.Id, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 4 },
                    // Anonymous reviews
                    new Review { CustomerName = "Anonymous", Comment = "Not my taste.", SentimentScore = 0.3, DishId = pizza!.Id, CustomerId = null, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 2 },
                    new Review { CustomerName = "Guest", Comment = "Loved the ambiance!", SentimentScore = 0.85, DishId = caesar!.Id, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 5 }
                );
            }

            // Seed AdminUsers
            if (!dbContext.AdminUsers.Any())
            {
                var admin = new AdminUser {
                    Username = "admin1",
                    PasswordHash = "adminhash1", // Replace with real hash in production
                    IsActive = true,
                    Role = "Admin",
                    SalesThreshold = 40,
                    SentimentThreshold = 0.7
                };
                var manager = new AdminUser {
                    Username = "manager1",
                    PasswordHash = "managerhash1",
                    IsActive = true,
                    Role = "Manager",
                    SalesThreshold = 30,
                    SentimentThreshold = 0.6
                };
                dbContext.AdminUsers.AddRange(admin, manager);
                dbContext.SaveChanges();
            }

            Console.WriteLine("✅ Done seeding database.");
        }
    }
}
