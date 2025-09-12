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

            // Seed AdminUsers (owners)
            if (!dbContext.AdminUsers.Any())
            {
                var admin = new AdminUser
                {
                    Username = "admin1",
                    PasswordHash = "adminhash1",
                    IsActive = true,
                    Role = "Admin",
                    SalesThreshold = 30,
                    SentimentThreshold = 0.6
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
                dbContext.SaveChanges();
            }

            // Seed Restaurants
            if (!dbContext.Restaurants.Any())
            {
                var admin = dbContext.AdminUsers.FirstOrDefault(u => u.Username == "admin1");
                var manager = dbContext.AdminUsers.FirstOrDefault(u => u.Username == "manager1");
                if (admin == null || manager == null)
                    throw new Exception("❌ Required admin users could not be loaded for restaurants.");

                var r1 = new Restaurant { Name = "Urban Bistro", OwnerId = admin.Id };
                var r2 = new Restaurant { Name = "Green Leaf", OwnerId = manager.Id };
                dbContext.Restaurants.AddRange(r1, r2);
                dbContext.SaveChanges();
            }

            // Seed Categories (per restaurant)
            var Bistro = dbContext.Restaurants.FirstOrDefault(r => r.Name.Contains("Urban Bistro")); // Assuming "Urban Bistro" is the name of the first restaurant
            var GreenLeaf = dbContext.Restaurants.FirstOrDefault(r => r.Name.Contains("Green Leaf")); // Assuming "Green Leaf" is the name of the second restaurant
            if (Bistro == null || GreenLeaf == null)
                throw new Exception("❌ Required restaurants could not be loaded for categories.");

            if (!dbContext.Categories.Any())
            {
                var italianCategory = new Category { Name = "Italian", RestaurantId = Bistro.Id };
                var grillCategory = new Category { Name = "Grill", RestaurantId = Bistro.Id };
                var dessertCategory = new Category { Name = "Dessert", RestaurantId = Bistro.Id };
                var saladCategory = new Category { Name = "Salad", RestaurantId = GreenLeaf.Id };
                var veganCategory = new Category { Name = "Vegan", RestaurantId = GreenLeaf.Id };
                var drinksCategory = new Category { Name = "Drinks", RestaurantId = GreenLeaf.Id };
                dbContext.Categories.AddRange(italianCategory, grillCategory, dessertCategory, saladCategory, veganCategory, drinksCategory);
                dbContext.SaveChanges();
            }

            // Reload categories to ensure IDs are set
            var italianCat = dbContext.Categories.FirstOrDefault(c => c.Name == "Italian" && c.RestaurantId == Bistro.Id);
            var grillCat = dbContext.Categories.FirstOrDefault(c => c.Name == "Grill" && c.RestaurantId == Bistro.Id);
            var dessertCat = dbContext.Categories.FirstOrDefault(c => c.Name == "Dessert" && c.RestaurantId == Bistro.Id);
            var saladCat = dbContext.Categories.FirstOrDefault(c => c.Name == "Salad" && c.RestaurantId == GreenLeaf.Id);
            var veganCat = dbContext.Categories.FirstOrDefault(c => c.Name == "Vegan" && c.RestaurantId == GreenLeaf.Id);
            var drinksCat = dbContext.Categories.FirstOrDefault(c => c.Name == "Drinks" && c.RestaurantId == GreenLeaf.Id);
            if (italianCat == null || grillCat == null || dessertCat == null || saladCat == null || veganCat == null || drinksCat == null)
                throw new Exception("❌ Required categories could not be loaded after save.");

            // Seed Dishes (per restaurant)
            if (!dbContext.Dishes.Any())
            {
                // Italian
                var pizza = new Dish { Name = "Pizza Margherita", CategoryId = italianCat.Id, RestaurantId = Bistro.Id, DishPrice = 10.99m };
                var spaghetti = new Dish { Name = "Spaghetti Carbonara", CategoryId = italianCat.Id, RestaurantId = Bistro.Id, DishPrice = 12.49m };
                var lasagna = new Dish { Name = "Lasagna", CategoryId = italianCat.Id, RestaurantId = Bistro.Id, DishPrice = 13.99m };
                // Grill
                var steak = new Dish { Name = "Grilled Steak", CategoryId = grillCat.Id, RestaurantId = Bistro.Id, DishPrice = 19.99m };
                var burger = new Dish { Name = "Classic Burger", CategoryId = grillCat.Id, RestaurantId = Bistro.Id, DishPrice = 11.49m };
                // Dessert
                var tiramisu = new Dish { Name = "Tiramisu", CategoryId = dessertCat.Id, RestaurantId = Bistro.Id, DishPrice = 6.99m };
                var cheesecake = new Dish { Name = "Cheesecake", CategoryId = dessertCat.Id, RestaurantId = Bistro.Id, DishPrice = 7.49m };
                // Salad
                var caesar = new Dish { Name = "Caesar Salad", CategoryId = saladCat.Id, RestaurantId = GreenLeaf.Id, DishPrice = 8.99m };
                var greek = new Dish { Name = "Greek Salad", CategoryId = saladCat.Id, RestaurantId = GreenLeaf.Id, DishPrice = 9.49m };
                // Vegan
                var tofu = new Dish { Name = "Tofu Stir Fry", CategoryId = veganCat.Id, RestaurantId = GreenLeaf.Id, DishPrice = 10.49m };
                var veganBowl = new Dish { Name = "Vegan Power Bowl", CategoryId = veganCat.Id, RestaurantId = GreenLeaf.Id, DishPrice = 11.99m };
                // Drinks
                var lemonade = new Dish { Name = "Fresh Lemonade", CategoryId = drinksCat.Id, RestaurantId = GreenLeaf.Id, DishPrice = 3.99m };
                var coffee = new Dish { Name = "Iced Coffee", CategoryId = drinksCat.Id, RestaurantId = GreenLeaf.Id, DishPrice = 4.49m };
                dbContext.Dishes.AddRange(pizza, spaghetti, lasagna, steak, burger, tiramisu, cheesecake, caesar, greek, tofu, veganBowl, lemonade, coffee);
                dbContext.SaveChanges();
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
                var diana = new Customer
                {
                    Name = "Diana",
                    Email = "diana@example.com",
                    Username = "dianauser",
                    PasswordHash = "hashedpassword4",
                    IsActive = true,
                    DateRegistered = DateTime.UtcNow.AddDays(-3),
                    Role = "User"
                };
                var eric = new Customer
                {
                    Name = "Eric",
                    Email = "eric@example.com",
                    Username = "ericuser",
                    PasswordHash = "hashedpassword5",
                    IsActive = true,
                    DateRegistered = DateTime.UtcNow.AddDays(-2),
                    Role = "User"
                };
                dbContext.Customers.AddRange(alice, bob, charlie, diana, eric);
                dbContext.SaveChanges();
            }

            // Seed SaleRecords (per dish)
            if (!dbContext.SaleRecords.Any())
            {
                var pizza = dbContext.Dishes.FirstOrDefault(d => d.Name == "Pizza Margherita");
                var spaghetti = dbContext.Dishes.FirstOrDefault(d => d.Name == "Spaghetti Carbonara");
                var lasagna = dbContext.Dishes.FirstOrDefault(d => d.Name == "Lasagna");
                var steak = dbContext.Dishes.FirstOrDefault(d => d.Name == "Grilled Steak");
                var burger = dbContext.Dishes.FirstOrDefault(d => d.Name == "Classic Burger");
                var tiramisu = dbContext.Dishes.FirstOrDefault(d => d.Name == "Tiramisu");
                var cheesecake = dbContext.Dishes.FirstOrDefault(d => d.Name == "Cheesecake");
                var caesar = dbContext.Dishes.FirstOrDefault(d => d.Name == "Caesar Salad");
                var greek = dbContext.Dishes.FirstOrDefault(d => d.Name == "Greek Salad");
                var tofu = dbContext.Dishes.FirstOrDefault(d => d.Name == "Tofu Stir Fry");
                var veganBowl = dbContext.Dishes.FirstOrDefault(d => d.Name == "Vegan Power Bowl");
                var lemonade = dbContext.Dishes.FirstOrDefault(d => d.Name == "Fresh Lemonade");
                var coffee = dbContext.Dishes.FirstOrDefault(d => d.Name == "Iced Coffee");
                if (pizza == null || spaghetti == null || lasagna == null || steak == null || burger == null || tiramisu == null || cheesecake == null || caesar == null || greek == null || tofu == null || veganBowl == null || lemonade == null || coffee == null)
                    throw new Exception("❌ Required dishes could not be loaded for sale records.");

                dbContext.SaleRecords.AddRange(
                    // Pizza Margherita
                    new SaleRecord { DishId = pizza.Id, QuantitySold = 50, SaleDate = DateTime.Today.AddDays(-1).ToUniversalTime() },
                    new SaleRecord { DishId = pizza.Id, QuantitySold = 30, SaleDate = DateTime.Today.AddDays(-2).ToUniversalTime() },
                    new SaleRecord { DishId = pizza.Id, QuantitySold = 40, SaleDate = DateTime.Today.AddDays(-3).ToUniversalTime() },
                    // Spaghetti Carbonara
                    new SaleRecord { DishId = spaghetti.Id, QuantitySold = 25, SaleDate = DateTime.Today.AddDays(-1).ToUniversalTime() },
                    new SaleRecord { DishId = spaghetti.Id, QuantitySold = 20, SaleDate = DateTime.Today.AddDays(-2).ToUniversalTime() },
                    // Lasagna
                    new SaleRecord { DishId = lasagna.Id, QuantitySold = 15, SaleDate = DateTime.Today.AddDays(-1).ToUniversalTime() },
                    // Grilled Steak
                    new SaleRecord { DishId = steak.Id, QuantitySold = 18, SaleDate = DateTime.Today.AddDays(-2).ToUniversalTime() },
                    // Classic Burger
                    new SaleRecord { DishId = burger.Id, QuantitySold = 22, SaleDate = DateTime.Today.AddDays(-3).ToUniversalTime() },
                    // Tiramisu
                    new SaleRecord { DishId = tiramisu.Id, QuantitySold = 12, SaleDate = DateTime.Today.AddDays(-1).ToUniversalTime() },
                    // Cheesecake
                    new SaleRecord { DishId = cheesecake.Id, QuantitySold = 10, SaleDate = DateTime.Today.AddDays(-2).ToUniversalTime() },
                    // Caesar Salad
                    new SaleRecord { DishId = caesar.Id, QuantitySold = 20, SaleDate = DateTime.Today.AddDays(-3).ToUniversalTime() },
                    new SaleRecord { DishId = caesar.Id, QuantitySold = 15, SaleDate = DateTime.Today.AddDays(-4).ToUniversalTime() },
                    // Greek Salad
                    new SaleRecord { DishId = greek.Id, QuantitySold = 13, SaleDate = DateTime.Today.AddDays(-2).ToUniversalTime() },
                    // Tofu Stir Fry
                    new SaleRecord { DishId = tofu.Id, QuantitySold = 9, SaleDate = DateTime.Today.AddDays(-1).ToUniversalTime() },
                    // Vegan Power Bowl
                    new SaleRecord { DishId = veganBowl.Id, QuantitySold = 8, SaleDate = DateTime.Today.AddDays(-2).ToUniversalTime() },
                    // Fresh Lemonade
                    new SaleRecord { DishId = lemonade.Id, QuantitySold = 17, SaleDate = DateTime.Today.AddDays(-1).ToUniversalTime() },
                    // Iced Coffee
                    new SaleRecord { DishId = coffee.Id, QuantitySold = 14, SaleDate = DateTime.Today.AddDays(-2).ToUniversalTime() }
                );
                dbContext.SaveChanges();
            }

            // Seed Reviews (per dish and restaurant)
            if (!dbContext.Reviews.Any())
            {
                var pizza = dbContext.Dishes.FirstOrDefault(d => d.Name == "Pizza Margherita");
                var spaghetti = dbContext.Dishes.FirstOrDefault(d => d.Name == "Spaghetti Carbonara");
                var lasagna = dbContext.Dishes.FirstOrDefault(d => d.Name == "Lasagna");
                var steak = dbContext.Dishes.FirstOrDefault(d => d.Name == "Grilled Steak");
                var burger = dbContext.Dishes.FirstOrDefault(d => d.Name == "Classic Burger");
                var tiramisu = dbContext.Dishes.FirstOrDefault(d => d.Name == "Tiramisu");
                var cheesecake = dbContext.Dishes.FirstOrDefault(d => d.Name == "Cheesecake");
                var caesar = dbContext.Dishes.FirstOrDefault(d => d.Name == "Caesar Salad");
                var greek = dbContext.Dishes.FirstOrDefault(d => d.Name == "Greek Salad");
                var tofu = dbContext.Dishes.FirstOrDefault(d => d.Name == "Tofu Stir Fry");
                var veganBowl = dbContext.Dishes.FirstOrDefault(d => d.Name == "Vegan Power Bowl");
                var lemonade = dbContext.Dishes.FirstOrDefault(d => d.Name == "Fresh Lemonade");
                var coffee = dbContext.Dishes.FirstOrDefault(d => d.Name == "Iced Coffee");
                var alice = dbContext.Customers.FirstOrDefault(c => c.Name == "Alice");
                var bob = dbContext.Customers.FirstOrDefault(c => c.Name == "Bob");
                var charlie = dbContext.Customers.FirstOrDefault(c => c.Name == "Charlie");
                var diana = dbContext.Customers.FirstOrDefault(c => c.Name == "Diana");
                var eric = dbContext.Customers.FirstOrDefault(c => c.Name == "Eric");
                if (pizza == null || spaghetti == null || lasagna == null || steak == null || burger == null || tiramisu == null || cheesecake == null || caesar == null || greek == null || tofu == null || veganBowl == null || lemonade == null || coffee == null)
                    throw new Exception("❌ Required dishes could not be loaded for reviews.");
                if (alice == null || bob == null || charlie == null || diana == null || eric == null)
                    throw new Exception("❌ Required customers could not be loaded for reviews.");

                dbContext.Reviews.AddRange(
                    // Pizza Margherita
                    new Review { CustomerId = alice.Id, Customer = alice, CustomerName = alice.Name, Comment = "Great food!", SentimentScore = 0.9, DishId = pizza.Id, RestaurantId = pizza.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-3), Rating = 5 },
                    new Review { CustomerId = bob.Id, Customer = bob, CustomerName = bob.Name, Comment = "Crust was a bit hard.", SentimentScore = 0.6, DishId = pizza.Id, RestaurantId = pizza.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 3 },
                    new Review { CustomerId = diana.Id, Customer = diana, CustomerName = diana.Name, Comment = "Loved the cheese!", SentimentScore = 0.8, DishId = pizza.Id, RestaurantId = pizza.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 4 },
                    new Review { CustomerName = "Anonymous", Comment = "Too cheesy for me.", SentimentScore = 0.4, DishId = pizza.Id, RestaurantId = pizza.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 2 },
                    new Review { CustomerName = "Guest", Comment = "Best pizza in town!", SentimentScore = 0.95, DishId = pizza.Id, RestaurantId = pizza.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 5 },

                    // Spaghetti Carbonara
                    new Review { CustomerId = charlie.Id, Customer = charlie, CustomerName = charlie.Name, Comment = "Loved the sauce!", SentimentScore = 0.85, DishId = spaghetti.Id, RestaurantId = spaghetti.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-4), Rating = 5 },
                    new Review { CustomerId = eric.Id, Customer = eric, CustomerName = eric.Name, Comment = "Could use more bacon.", SentimentScore = 0.7, DishId = spaghetti.Id, RestaurantId = spaghetti.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-3), Rating = 4 },
                    new Review { CustomerName = "Anonymous", Comment = "Too salty.", SentimentScore = 0.3, DishId = spaghetti.Id, RestaurantId = spaghetti.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 2 },
                    new Review { CustomerId = bob.Id, Customer = bob, CustomerName = bob.Name, Comment = "Good but not great.", SentimentScore = 0.6, DishId = spaghetti.Id, RestaurantId = spaghetti.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 3 },

                    // Lasagna
                    new Review { CustomerId = alice.Id, Customer = alice, CustomerName = alice.Name, Comment = "Delicious layers!", SentimentScore = 0.92, DishId = lasagna.Id, RestaurantId = lasagna.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-5), Rating = 5 },
                    new Review { CustomerId = diana.Id, Customer = diana, CustomerName = diana.Name, Comment = "Tasty and filling!", SentimentScore = 0.85, DishId = lasagna.Id, RestaurantId = lasagna.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 4 },
                    new Review { CustomerName = "Guest", Comment = "Portion was small.", SentimentScore = 0.5, DishId = lasagna.Id, RestaurantId = lasagna.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 3 },

                    // Grilled Steak
                    new Review { CustomerId = bob.Id, Customer = bob, CustomerName = bob.Name, Comment = "Perfectly cooked!", SentimentScore = 0.95, DishId = steak.Id, RestaurantId = steak.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 5 },
                    new Review { CustomerId = eric.Id, Customer = eric, CustomerName = eric.Name, Comment = "A bit overdone.", SentimentScore = 0.5, DishId = steak.Id, RestaurantId = steak.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 3 },
                    new Review { CustomerName = "Anonymous", Comment = "A bit tough.", SentimentScore = 0.4, DishId = steak.Id, RestaurantId = steak.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 2 },

                    // Classic Burger
                    new Review { CustomerId = charlie.Id, Customer = charlie, CustomerName = charlie.Name, Comment = "Juicy and tasty!", SentimentScore = 0.88, DishId = burger.Id, RestaurantId = burger.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-3), Rating = 4 },
                    new Review { CustomerId = diana.Id, Customer = diana, CustomerName = diana.Name, Comment = "Loved the fries!", SentimentScore = 0.9, DishId = burger.Id, RestaurantId = burger.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 5 },
                    new Review { CustomerName = "Guest", Comment = "Bun was stale.", SentimentScore = 0.2, DishId = burger.Id, RestaurantId = burger.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 1 },

                    // Tiramisu
                    new Review { CustomerId = alice.Id, Customer = alice, CustomerName = alice.Name, Comment = "Heavenly dessert!", SentimentScore = 0.97, DishId = tiramisu.Id, RestaurantId = tiramisu.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 5 },
                    new Review { CustomerId = eric.Id, Customer = eric, CustomerName = eric.Name, Comment = "Nice coffee flavor.", SentimentScore = 0.8, DishId = tiramisu.Id, RestaurantId = tiramisu.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 4 },
                    new Review { CustomerName = "Anonymous", Comment = "Too sweet.", SentimentScore = 0.5, DishId = tiramisu.Id, RestaurantId = tiramisu.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 3 },

                    // Cheesecake
                    new Review { CustomerId = bob.Id, Customer = bob, CustomerName = bob.Name, Comment = "Rich and creamy!", SentimentScore = 0.93, DishId = cheesecake.Id, RestaurantId = cheesecake.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 5 },
                    new Review { CustomerId = diana.Id, Customer = diana, CustomerName = diana.Name, Comment = "Perfect texture!", SentimentScore = 0.9, DishId = cheesecake.Id, RestaurantId = cheesecake.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 5 },
                    new Review { CustomerName = "Guest", Comment = "Not my favorite.", SentimentScore = 0.4, DishId = cheesecake.Id, RestaurantId = cheesecake.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 2 },

                    // Caesar Salad
                    new Review { CustomerId = charlie.Id, Customer = charlie, CustomerName = charlie.Name, Comment = "Fresh and crisp!", SentimentScore = 0.9, DishId = caesar.Id, RestaurantId = caesar.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-3), Rating = 5 },
                    new Review { CustomerId = eric.Id, Customer = eric, CustomerName = eric.Name, Comment = "Nice dressing.", SentimentScore = 0.8, DishId = caesar.Id, RestaurantId = caesar.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 4 },
                    new Review { CustomerName = "Anonymous", Comment = "Needed more dressing.", SentimentScore = 0.5, DishId = caesar.Id, RestaurantId = caesar.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 3 },
                    new Review { CustomerId = alice.Id, Customer = alice, CustomerName = alice.Name, Comment = "Loved the ambiance!", SentimentScore = 0.85, DishId = caesar.Id, RestaurantId = caesar.RestaurantId, DateCreated = DateTime.UtcNow, Rating = 5 },

                    // Greek Salad
                    new Review { CustomerId = bob.Id, Customer = bob, CustomerName = bob.Name, Comment = "Very authentic!", SentimentScore = 0.8, DishId = greek.Id, RestaurantId = greek.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 4 },
                    new Review { CustomerId = diana.Id, Customer = diana, CustomerName = diana.Name, Comment = "Loved the feta!", SentimentScore = 0.9, DishId = greek.Id, RestaurantId = greek.RestaurantId, DateCreated = DateTime.UtcNow, Rating = 5 },
                    new Review { CustomerName = "Guest", Comment = "Too many olives.", SentimentScore = 0.4, DishId = greek.Id, RestaurantId = greek.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 2 },

                    // Tofu Stir Fry
                    new Review { CustomerId = charlie.Id, Customer = charlie, CustomerName = charlie.Name, Comment = "Healthy and tasty!", SentimentScore = 0.9, DishId = tofu.Id, RestaurantId = tofu.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 5 },
                    new Review { CustomerId = eric.Id, Customer = eric, CustomerName = eric.Name, Comment = "Great vegan option!", SentimentScore = 0.85, DishId = tofu.Id, RestaurantId = tofu.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 4 },
                    new Review { CustomerName = "Anonymous", Comment = "Not enough flavor.", SentimentScore = 0.3, DishId = tofu.Id, RestaurantId = tofu.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 2 },

                    // Vegan Power Bowl
                    new Review { CustomerId = alice.Id, Customer = alice, CustomerName = alice.Name, Comment = "Super filling!", SentimentScore = 0.88, DishId = veganBowl.Id, RestaurantId = veganBowl.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 4 },
                    new Review { CustomerId = diana.Id, Customer = diana, CustomerName = diana.Name, Comment = "Loved the variety!", SentimentScore = 0.9, DishId = veganBowl.Id, RestaurantId = veganBowl.RestaurantId, DateCreated = DateTime.UtcNow, Rating = 5 },
                    new Review { CustomerName = "Guest", Comment = "Too many beans.", SentimentScore = 0.5, DishId = veganBowl.Id, RestaurantId = veganBowl.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 3 },

                    // Fresh Lemonade
                    new Review { CustomerId = bob.Id, Customer = bob, CustomerName = bob.Name, Comment = "Very refreshing!", SentimentScore = 0.95, DishId = lemonade.Id, RestaurantId = lemonade.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 5 },
                    new Review { CustomerId = eric.Id, Customer = eric, CustomerName = eric.Name, Comment = "Nice and cold!", SentimentScore = 0.9, DishId = lemonade.Id, RestaurantId = lemonade.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 4 },
                    new Review { CustomerName = "Anonymous", Comment = "Too sour.", SentimentScore = 0.4, DishId = lemonade.Id, RestaurantId = lemonade.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 2 },

                    // Iced Coffee
                    new Review { CustomerId = charlie.Id, Customer = charlie, CustomerName = charlie.Name, Comment = "Perfect for summer!", SentimentScore = 0.9, DishId = coffee.Id, RestaurantId = coffee.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 5 },
                    new Review { CustomerId = diana.Id, Customer = diana, CustomerName = diana.Name, Comment = "Great pick-me-up!", SentimentScore = 0.85, DishId = coffee.Id, RestaurantId = coffee.RestaurantId, DateCreated = DateTime.UtcNow, Rating = 4 },
                    new Review { CustomerName = "Guest", Comment = "Not strong enough.", SentimentScore = 0.5, DishId = coffee.Id, RestaurantId = coffee.RestaurantId, CustomerId = null, DateCreated = DateTime.UtcNow, Rating = 3 }
                );
                dbContext.SaveChanges();
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
            dbContext.Categories.RemoveRange(dbContext.Categories);
            dbContext.Restaurants.RemoveRange(dbContext.Restaurants);
            dbContext.Customers.RemoveRange(dbContext.Customers);
            dbContext.AdminUsers.RemoveRange(dbContext.AdminUsers);
            dbContext.SaveChanges();
        }
    }
}
