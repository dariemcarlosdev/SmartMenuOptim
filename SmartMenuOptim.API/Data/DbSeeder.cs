using Microsoft.EntityFrameworkCore;
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

            // WARNING: This will delete ALL data in these tables!. Just call it when you want to reset the database.
            ClearTables(dbContext);

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
                    Console.WriteLine($"⏳ Waiting for database connection... attempt {retryCount}/{maxRetries} - {ex.Message}");
                    Thread.Sleep(3000);
                }
            }
            if (!dbReady)
                throw new Exception("❌ Could not connect to DB or apply migrations after retries.");

            // Ensure required categories exist (idempotent)
            var italianCategory = dbContext.Categories.FirstOrDefault(c => c.Name == "Italian");
            if (italianCategory == null)
            {
                italianCategory = new Category { Name = "Italian" };
                dbContext.Categories.Add(italianCategory);
            }
            var saladCategory = dbContext.Categories.FirstOrDefault(c => c.Name == "Salad");
            if (saladCategory == null)
            {
                saladCategory = new Category { Name = "Salad" };
                dbContext.Categories.Add(saladCategory);
            }
            try
            {
                dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save categories: {ex.Message}");
                throw;
            }

            // Reload categories to ensure IDs are set
            italianCategory = dbContext.Categories.FirstOrDefault(c => c.Name == "Italian");
            saladCategory = dbContext.Categories.FirstOrDefault(c => c.Name == "Salad");
            if (italianCategory == null || saladCategory == null)
                throw new Exception("❌ Required categories could not be loaded after save.");

            // Seed Dishes
            if (!dbContext.Dishes.Any())
            {
                var pizza = new Dish { Name = "Pizza Margherita", CategoryId = italianCategory.Id };
                var spaghetti = new Dish { Name = "Spaghetti Carbonara", CategoryId = italianCategory.Id };
                var caesar = new Dish { Name = "Caesar Salad", CategoryId = saladCategory.Id };

                dbContext.Dishes.AddRange(pizza, spaghetti, caesar);
                try
                {
                    dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save dishes: {ex.Message}");
                    throw;
                }
            }

            // Seed Customers
            if (!dbContext.Customers.Any())
            {
                var alice = new Customer
                {
                    Name = "Alice",
                    Email = "alice@example.com",
                    Username = "aliceuser",
                    PasswordHash = "hashedpassword1",
                    IsActive = true,
                    DateRegistered = DateTime.UtcNow.AddDays(-10),
                    Role = "User"
                };
                var bob = new Customer
                {
                    Name = "Bob",
                    Email = "bob@example.com",
                    Username = "bobuser",
                    PasswordHash = "hashedpassword2",
                    IsActive = true,
                    DateRegistered = DateTime.UtcNow.AddDays(-8),
                    Role = "User"
                };
                var charlie = new Customer
                {
                    Name = "Charlie",
                    Email = "charlie@example.com",
                    Username = "charlieuser",
                    PasswordHash = "hashedpassword3",
                    IsActive = false,
                    DateRegistered = DateTime.UtcNow.AddDays(-5),
                    Role = "User"
                };
                dbContext.Customers.AddRange(alice, bob, charlie);
                try
                {
                    dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save customers: {ex.Message}");
                    throw;
                }
            }

            // Seed SaleRecords
            if (!dbContext.SaleRecords.Any())
            {
                var pizza = dbContext.Dishes.FirstOrDefault(d => d.Name == "Pizza Margherita");
                var spaghetti = dbContext.Dishes.FirstOrDefault(d => d.Name == "Spaghetti Carbonara");
                var caesar = dbContext.Dishes.FirstOrDefault(d => d.Name == "Caesar Salad");
                if (pizza == null || spaghetti == null || caesar == null)
                    throw new Exception("❌ Required dishes could not be loaded for sale records.");

                dbContext.SaleRecords.AddRange(
                    new SaleRecord { DishId = pizza.Id, QuantitySold = 50, SaleDate = DateTime.Today.AddDays(-1).ToUniversalTime() },
                    new SaleRecord { DishId = spaghetti.Id, QuantitySold = 30, SaleDate = DateTime.Today.AddDays(-2).ToUniversalTime() },
                    new SaleRecord { DishId = caesar.Id, QuantitySold = 20, SaleDate = DateTime.Today.AddDays(-3).ToUniversalTime() }
                );
                try
                {
                    dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save sale records: {ex.Message}");
                    throw;
                }
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

                if (pizza == null || spaghetti == null || caesar == null)
                    throw new Exception("❌ Required dishes could not be loaded for reviews.");
                if (alice == null || bob == null || charlie == null)
                    throw new Exception("❌ Required customers could not be loaded for reviews.");

                dbContext.Reviews.AddRange(
                    // Linked reviews (CustomerName is set to the customer's name)
                    new Review { CustomerId = alice.Id, Customer = alice, CustomerName = alice.Name, Comment = "Great food!", SentimentScore = 0.9, DishId = pizza.Id, DateCreated = DateTime.UtcNow.AddDays(-3), Rating = 5 },
                    new Review { CustomerId = bob.Id, Customer = bob, CustomerName = bob.Name, Comment = "Average service.", SentimentScore = 0.5, DishId = spaghetti.Id, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 3 },
                    new Review { CustomerId = charlie.Id, Customer = charlie, CustomerName = charlie.Name, Comment = "Will come back again!", SentimentScore = 0.8, DishId = caesar.Id, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 4 },
                    // Anonymous reviews
                    new Review { CustomerName = "Anonymous", Comment = "Not my taste.", SentimentScore = 0.3, DishId = pizza.Id, CustomerId = null, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 2 },
                    new Review { CustomerName = "Guest", Comment = "Loved the ambiance!", SentimentScore = 0.85, DishId = caesar.Id, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 5 }
                );
                try
                {
                    dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save reviews: {ex.Message}");
                    throw;
                }
            }

            // Seed AdminUsers
            if (!dbContext.AdminUsers.Any())
            {
                var admin = new AdminUser
                {
                    Username = "admin1",
                    PasswordHash = "adminhash1",
                    IsActive = true,
                    Role = "Admin",
                    SalesThreshold = 40,
                    SentimentThreshold = 0.7
                };
                var manager = new AdminUser
                {
                    Username = "manager1",
                    PasswordHash = "managerhash1",
                    IsActive = true,
                    Role = "Manager",
                    SalesThreshold = 30,
                    SentimentThreshold = 0.6
                };
                dbContext.AdminUsers.AddRange(admin, manager);

                try
                {
                    dbContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save admin users: {ex.Message}");
                    throw;
                }
            }

            Console.WriteLine("✅ Done seeding database.");

            // ------------------------------------------------------------------------------------
            // FUTURE SEEDING CONSIDERATIONS:
            // ------------------------------------------------------------------------------------
            // When expanding the database and seeding logic, consider the following entities
            // and relationships for a more robust and realistic dataset:
            //
            // 1. Additional Entities:
            //    - Ingredient: For many-to-many Dish-Ingredient relationships.
            //    - Order & OrderItem: To link Customers and Dishes for order history.
            //    - Table & Reservation: For restaurant table management and bookings.
            //    - Menu: For supporting seasonal or dynamic menus.
            //    - Payment & Transaction: For e-commerce/payment tracking.
            //    - Notification: For user/admin alerts and system messages.
            //
            // 2. Relationships:
            //    - Many-to-Many: Dishes ↔ Ingredients, Orders ↔ Dishes (via OrderItems).
            //    - One-to-Many: Customer → Orders, AdminUser → Notifications.
            //    - One-to-One: Customer → Profile (if separating profile details).
            //    - Optional/Nullable: Review → Customer (already present, for anonymous reviews) Reservation → Table (nullable if not assigned yet)
            //
            // 3. Seeding Best Practices:
            //    - Always seed in dependency order (e.g., categories → dishes → sale records).Always create parent entities before children.
            //    - Ensure foreign key constraints are respected.
            //    - Add realistic data for relationships (e.g., multiple reviews per dish).
            //    - Consider edge cases (e.g., dishes without reviews, customers without orders).
            //
            // 4. Example Expansion (pseudo-code):
            //    // Seed Ingredients and link to Dishes
            //    // if (!dbContext.Ingredients.Any()) { ... }
            //    // dbContext.DishIngredients.AddRange(...);
            //    // dbContext.SaveChanges();
            // ------------------------------------------------------------------------------------
            // Example: Seeding Ingredients and linking to Dishes
            // ------------------------------------------------------------------------------------
            // if (!dbContext.Ingredients.Any())
            // {
            //    var cheese = new Ingredient { Name = "Cheese" };
            //    var tomato = new Ingredient { Name = "Tomato" };
            //    dbContext.Ingredients.AddRange(cheese, tomato);
            //    dbContext.SaveChanges();
            //
            //    var pizza = dbContext.Dishes.FirstOrDefault(d => d.Name == "Pizza Margherita");
            //    if (pizza != null)
            //    {
            //        dbContext.DishIngredients.AddRange(
            //            new DishIngredient { DishId = pizza.Id, IngredientId = cheese.Id },
            //            new DishIngredient { DishId = pizza.Id, IngredientId = tomato.Id }
            //        );
            //        dbContext.SaveChanges();
            //    }
            // }
            //
            // ------------------------------------------------------------------------------------
        }

        private static void ClearTables(AppDbContext dbContext)
        {
            // Remove child tables first to respect FK constraints
            dbContext.Reviews.RemoveRange(dbContext.Reviews);
            dbContext.SaleRecords.RemoveRange(dbContext.SaleRecords);
            dbContext.Dishes.RemoveRange(dbContext.Dishes);
            dbContext.Customers.RemoveRange(dbContext.Customers);
            dbContext.AdminUsers.RemoveRange(dbContext.AdminUsers);
            dbContext.Categories.RemoveRange(dbContext.Categories);
            dbContext.SaveChanges();
        }
    }
}
