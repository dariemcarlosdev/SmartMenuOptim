using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartMenuOptim.Shared.Data.Context;
using SmartMenuOptim.Shared.Data.Entities;
using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;
using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;

namespace SmartMenuOptim.API.Data
{
    public static class DbSeeder
    {
        public static void Seed(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext   >();

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
                    Email = "admin@smartmenuoptim.com",
                    PasswordHash = "adminhash1",  // In production, use proper password hashing
                    Role = AdminRole.SystemAdmin,
                    PhoneNumber = "+1 (555) 111-0000",
                    IsActive = true,
                    LastLoginAt = DateTime.UtcNow,
                    // Analytics and threshold settings
                    SalesThreshold = 30,
                    SentimentThreshold = 0.6,
                    ReviewCountThreshold = 5,
                    WellSoldThreshold = 20,
                    RegularCustomerReviewCountThreshold = 3,
                    PremiumCustomerReviewCountThreshold = 10,
                    // Full access permissions for admin
                    Permissions = AdminPermission.All
                };

                var manager = new AdminUser
                {
                    Username = "manager1",
                    Email = "manager@smartmenuoptim.com",
                    PasswordHash = "managerhash1",  // In production, use proper password hashing
                    Role = AdminRole.Manager,
                    PhoneNumber = "+1 (555) 111-0001",
                    IsActive = true,
                    LastLoginAt = DateTime.UtcNow,
                    // Analytics and threshold settings (more conservative)
                    SalesThreshold = 40,
                    SentimentThreshold = 0.7,
                    ReviewCountThreshold = 8,
                    WellSoldThreshold = 25,
                    RegularCustomerReviewCountThreshold = 5,
                    PremiumCustomerReviewCountThreshold = 15,
                    // Get default permissions for manager role
                    Permissions = AdminUser.GetDefaultPermissionsForRole(AdminRole.Manager)
                };

                dbContext.AdminUsers.AddRange(admin, manager);
                dbContext.SaveChanges();

                Console.WriteLine("✅ AdminUsers seeded successfully");
            }

            // Seed BusinessRules for historical tracking
            if (!dbContext.BusinessRules.Any())
            {
                var admin = dbContext.AdminUsers.FirstOrDefault(u => u.Username == "admin1");
                var manager = dbContext.AdminUsers.FirstOrDefault(u => u.Username == "manager1");
                if (admin == null || manager == null)
                    throw new Exception("❌ Required admin users could not be loaded for business rules.");

                // Historical rules for admin
                var adminRules = new[]
                {
                    new BusinessRule
                    {
                        Name = "Initial Sales Threshold",
                        Description = "Initial setup of sales threshold for popular dishes. Base threshold for considering a dish popular based on sales volume.",
                        Value = 30,
                        RuleType = BusinessRuleType.SalesThreshold,
                        AdminUserId = admin.Id,
                        AdminUser = admin,
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        IsActive = true,
                        Version = 1,
                        Notes = "Initial configuration for sales threshold"
                    },
                    new BusinessRule
                    {
                        Name = "Initial Sentiment Threshold",
                        Description = "Initial setup of sentiment score threshold. Base threshold for determining positive customer sentiment in reviews.",
                        Value = 0.6,
                        RuleType = BusinessRuleType.SentimentThreshold,
                        AdminUserId = admin.Id,
                        AdminUser = admin,
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        IsActive = true,
                        Version = 1,
                        Notes = "Initial configuration for sentiment analysis"
                    },
                    new BusinessRule
                    {
                        Name = "Initial Review Count Threshold",
                        Description = "Initial minimum reviews required for well-reviewed status. Minimum number of reviews needed to consider feedback statistically significant.",
                        Value = 5,
                        RuleType = BusinessRuleType.ReviewCountThreshold,
                        AdminUserId = admin.Id,
                        AdminUser = admin,
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        IsActive = true,
                        Version = 1,
                        Notes = "Initial configuration for review count requirements"
                    },
                    new BusinessRule
                    {
                        Name = "Initial Well-Sold Threshold",
                        Description = "Initial threshold for well-sold dishes. Base number of sales required to consider a dish well-performing.",
                        Value = 20,
                        RuleType = BusinessRuleType.WellSoldThreshold,
                        AdminUserId = admin.Id,
                        AdminUser = admin,
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        IsActive = true,
                        Version = 1,
                        Notes = "Initial configuration for sales performance metrics"
                    },
                    new BusinessRule
                    {
                        Name = "Regular Customer Review Threshold",
                        Description = "Minimum reviews needed for regular customer status. Threshold for identifying engaged customers.",
                        Value = 3,
                        RuleType = BusinessRuleType.RegularCustomerReviewCountThreshold,
                        AdminUserId = admin.Id,
                        AdminUser = admin,
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        IsActive = true,
                        Version = 1,
                        Notes = "Initial configuration for regular customer classification"
                    }
                };

                // Historical rules for manager (more conservative settings)
                var managerRules = new[]
                {
                    new BusinessRule
                    {
                        Name = "Manager Sales Threshold",
                        Description = "Manager's custom sales threshold setting. Higher threshold for more stringent popularity classification.",
                        Value = 40,
                        RuleType = BusinessRuleType.SalesThreshold,
                        AdminUserId = manager.Id,
                        AdminUser = manager,
                        CreatedAt = DateTime.UtcNow.AddDays(-15),
                        IsActive = true,
                        Version = 1,
                        Notes = "Manager adjusted sales threshold for better accuracy"
                    },
                    new BusinessRule
                    {
                        Name = "Manager Sentiment Threshold",
                        Description = "Manager's custom sentiment threshold. Higher requirement for positive sentiment classification.",
                        Value = 0.7,
                        RuleType = BusinessRuleType.SentimentThreshold,
                        AdminUserId = manager.Id,
                        AdminUser = manager,
                        CreatedAt = DateTime.UtcNow.AddDays(-15),
                        IsActive = true,
                        Version = 1,
                        Notes = "Manager adjusted sentiment threshold for higher quality standards"
                    },
                    new BusinessRule
                    {
                        Name = "Manager Review Count Setting",
                        Description = "Manager's custom review count requirement. Increased minimum reviews for statistical significance.",
                        Value = 8,
                        RuleType = BusinessRuleType.ReviewCountThreshold,
                        AdminUserId = manager.Id,
                        AdminUser = manager,
                        CreatedAt = DateTime.UtcNow.AddDays(-15),
                        IsActive = true,
                        Version = 1,
                        Notes = "Manager adjusted review count for better data reliability"
                    }
                };

                dbContext.BusinessRules.AddRange(adminRules);
                dbContext.BusinessRules.AddRange(managerRules);
                dbContext.SaveChanges();

                Console.WriteLine("✅ Business Rules seeded successfully");
            }

            // Seed Restaurants
            if (!dbContext.Restaurants.Any())
            {
                var admin = dbContext.AdminUsers.FirstOrDefault(u => u.Username == "admin1");
                var manager = dbContext.AdminUsers.FirstOrDefault(u => u.Username == "manager1");
                if (admin == null || manager == null)
                    throw new Exception("❌ Required admin users could not be loaded for restaurants.");

                // Restaurant validation:
                // - Name is required and <= 200 chars
                // - Email is required and a valid email address
                // - PhoneNumber is required and a valid phone format
                // - TimeZoneId is required and <= 100 chars
                // Indexes for restaurants (e.g., by OwnerId) are defined centrally in AppDbContext

                var urbanBistro = new Restaurant
                {
                    Name = "Urban Bistro",
                    OwnerId = admin.Id,
                    Email = "contact@urbanbistro.com",
                    PhoneNumber = "+1 (555) 123-4567",
                    Address = "123 City Center, Downtown",
                    Description = "A modern bistro offering contemporary fusion cuisine in an elegant setting. " +
                                "Open daily for breakfast, lunch, and dinner.",
                    TimeZoneId = "America/New_York",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                var greenLeaf = new Restaurant
                {
                    Name = "Green Leaf",
                    OwnerId = manager.Id,
                    Email = "info@greenleafrestaurant.com",
                    PhoneNumber = "+1 (555) 987-6543",
                    Address = "456 Garden Avenue, Midtown",
                    Description = "Eco-friendly restaurant specializing in fresh, organic, and plant-based cuisine. " +
                                "Supporting local farmers and sustainable practices.",
                    TimeZoneId = "America/New_York",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                dbContext.Restaurants.AddRange(urbanBistro, greenLeaf);
                dbContext.SaveChanges();
            }

            var Bistro = dbContext.Restaurants.FirstOrDefault(r => r.Name == "Urban Bistro");
            var GreenLeaf = dbContext.Restaurants.FirstOrDefault(r => r.Name == "Green Leaf");
            if (Bistro == null || GreenLeaf == null)
                throw new Exception("❌ Required restaurants could not be loaded.");

            // Seed Staff Members
            if (!dbContext.StaffMembers.Any())
            {
                if (Bistro == null || GreenLeaf == null)
                    throw new Exception("❌ Required restaurants could not be loaded for staff members.");

                var staffMembers = new[]
                {
                    // Urban Bistro Staff
                    new StaffMember { 
                        Name = "John Smith",
                        Role = StaffRole.Waiter,
                        IsActive = true,
                        RestaurantId = Bistro.Id,
                        Email = "john.smith@bistro.com",
                        Username = "john.smith",
                        PasswordHash = "hash", // In production, use proper password hashing
                        HireDate = DateTime.UtcNow.AddMonths(-6),
                        PhoneNumber = "+1 (555) 123-4567",
                        PhoneNumberConfirmed = true,
                        EmergencyContactName = "Mary Smith",
                        EmergencyContactPhone = "+1 (555) 987-6543",
                        EmploymentStatus = EmploymentStatus.FullTime,
                        Notes = "Experienced waiter, excellent customer service skills"
                    },
                    new StaffMember { 
                        Name = "Gordon Chef",
                        Role = StaffRole.Chef,
                        IsActive = true,
                        RestaurantId = Bistro.Id,
                        Email = "gordon.chef@bistro.com",
                        Username = "gordon.chef",
                        PasswordHash = "hash",
                        HireDate = DateTime.UtcNow.AddYears(-2),
                        PhoneNumber = "+1 (555) 234-5678",
                        PhoneNumberConfirmed = true,
                        EmergencyContactName = "Sarah Chef",
                        EmergencyContactPhone = "+1 (555) 876-5432",
                        EmploymentStatus = EmploymentStatus.FullTime,
                        Notes = "Head chef, specializes in Italian cuisine"
                    },
                    new StaffMember { 
                        Name = "Julia Cook",
                        Role = StaffRole.Manager,
                        IsActive = true,
                        RestaurantId = Bistro.Id,
                        Email = "julia.cook@bistro.com",
                        Username = "julia.cook",
                        PasswordHash = "hash",
                        HireDate = DateTime.UtcNow.AddYears(-1),
                        PhoneNumber = "+1 (555) 345-6789",
                        PhoneNumberConfirmed = true,
                        EmergencyContactName = "Robert Cook",
                        EmergencyContactPhone = "+1 (555) 765-4321",
                        EmploymentStatus = EmploymentStatus.FullTime,
                        Notes = "Restaurant manager, handles staff scheduling"
                    },

                    // Green Leaf Staff
                    new StaffMember { 
                        Name = "Mary Johnson",
                        Role = StaffRole.Waiter,
                        IsActive = true,
                        RestaurantId = GreenLeaf.Id,
                        Email = "mary.j@greenleaf.com",
                        Username = "mary.j",
                        PasswordHash = "hash",
                        HireDate = DateTime.UtcNow.AddMonths(-3),
                        PhoneNumber = "+1 (555) 456-7890",
                        PhoneNumberConfirmed = true,
                        EmergencyContactName = "John Johnson",
                        EmergencyContactPhone = "+1 (555) 654-3210",
                        EmploymentStatus = EmploymentStatus.PartTime,
                        Notes = "Part-time waiter, student"
                    },
                    new StaffMember { 
                        Name = "Sam Bartender",
                        Role = StaffRole.Bartender,
                        IsActive = true,
                        RestaurantId = GreenLeaf.Id,
                        Email = "sam.b@greenleaf.com",
                        Username = "sam.b",
                        PasswordHash = "hash",
                        HireDate = DateTime.UtcNow.AddMonths(-8),
                        PhoneNumber = "+1 (555) 567-8901",
                        PhoneNumberConfirmed = true,
                        EmergencyContactName = "Lisa Bartender",
                        EmergencyContactPhone = "+1 (555) 543-2109",
                        EmploymentStatus = EmploymentStatus.FullTime,
                        Notes = "Experienced mixologist, specializes in craft cocktails"
                    }
                };
                dbContext.StaffMembers.AddRange(staffMembers);
                dbContext.SaveChanges();

                Console.WriteLine("✅ Staff Members seeded successfully");
            }

            // Seed Staff Schedules
            if (!dbContext.StaffSchedules.Any())
            {
                var johnSmith = dbContext.StaffMembers.FirstOrDefault(s => s.Name == "John Smith");
                var gordonChef = dbContext.StaffMembers.FirstOrDefault(s => s.Name == "Gordon Chef");
                var juliaCook = dbContext.StaffMembers.FirstOrDefault(s => s.Name == "Julia Cook"); // Manager

                if (johnSmith == null || gordonChef == null || juliaCook == null)
                    throw new Exception("❌ Required staff members could not be loaded for schedule seeding.");

                // Use an AdminUser as the creator/modifier for seeded schedules so audit fields align with new model
                var adminUser = dbContext.AdminUsers.FirstOrDefault(u => u.Username == "admin1")
                                 ?? dbContext.AdminUsers.FirstOrDefault();
                if (adminUser == null)
                    throw new Exception("❌ Required admin user could not be loaded for schedule seeding.");

                // Notes:
                // - StaffSchedule validation enforces: ShiftEnd > ShiftStart, duration <= 24 hours, recurring schedules must set RecurringDay.
                // - Indexes for schedule queries are centralized in AppDbContext (IX_StaffSchedules_Restaurant_Staff_ShiftStart, IX_StaffSchedules_Restaurant_ShiftRange).
                // - CreatedByAdminUserId will reference an AdminUser for seeded schedules to reflect management by owners/managers.

                var now = DateTime.UtcNow;

                var schedules = new[]
                {
                    // John Smith's schedule at Urban Bistro (valid single-day shift, 8 hours)
                    new StaffSchedule
                    {
                        StaffMemberId = johnSmith.Id,
                        RestaurantId = johnSmith.RestaurantId,
                        ShiftStart = now.Date.AddDays(1).AddHours(9), // 9 AM tomorrow (UTC)
                        ShiftEnd = now.Date.AddDays(1).AddHours(17),   // 5 PM tomorrow (UTC)
                        IsRecurring = false,
                        RecurringDay = null,
                        Status = ScheduleStatus.Approved,
                        CreatedByAdminUserId = adminUser.Id,
                        Notes = "Morning shift",
                        LastModified = now,
                        LastModifiedByAdminUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now,
                        IsDeleted = false
                    },
                    // Gordon Chef's schedule at Urban Bistro (valid single-day shift, 8 hours)
                    new StaffSchedule
                    {
                        StaffMemberId = gordonChef.Id,
                        RestaurantId = gordonChef.RestaurantId,
                        ShiftStart = now.Date.AddDays(2).AddHours(14), // 2 PM in 2 days (UTC)
                        ShiftEnd = now.Date.AddDays(2).AddHours(22),   // 10 PM in 2 days (UTC)
                        IsRecurring = false,
                        RecurringDay = null,
                        Status = ScheduleStatus.Approved,
                        CreatedByAdminUserId = adminUser.Id,
                        Notes = "Evening shift, kitchen prep",
                        LastModified = now,
                        LastModifiedByAdminUserId = adminUser.Id,
                        CreatedAt = now,
                        UpdatedAt = now,
                        IsDeleted = false
                    }
                };
                dbContext.StaffSchedules.AddRange(schedules);
                dbContext.SaveChanges();
            }

            // Seed Menu Types
            if (!dbContext.MenuTypes.Any())
            {
                var restaurants = dbContext.Restaurants.ToList();
                if (!restaurants.Any())
                    throw new Exception("❌ Restaurants must be seeded before menu types.");

                foreach (var restaurant in restaurants)
                {
                    // Create menu types with trimmed names to satisfy unique index and validation rules
                    var breakfast = new MenuType
                    {
                        Name = "Breakfast".Trim(),
                        Description = "Morning menu, served 7-11 AM",
                        DefaultStartTime = new TimeSpan(7, 0, 0),  // 7:00 AM
                        DefaultEndTime = new TimeSpan(11, 0, 0),   // 11:00 AM
                        DisplayOrder = 1,
                        IsActive = true,
                        RestaurantId = restaurant.Id // Assign to the current restaurant
                    };
                    var lunch = new MenuType
                    {
                        Name = "Lunch".Trim(),
                        Description = "Midday menu, served 11 AM-4 PM",
                        DefaultStartTime = new TimeSpan(11, 0, 0), // 11:00 AM
                        DefaultEndTime = new TimeSpan(16, 0, 0),   // 4:00 PM
                        DisplayOrder = 2,
                        IsActive = true,
                        RestaurantId = restaurant.Id // Assign to the current restaurant
                    };
                    var dinner = new MenuType
                    {
                        Name = "Dinner".Trim(),
                        Description = "Evening menu, served 4-10 PM",
                        DefaultStartTime = new TimeSpan(16, 0, 0), // 4:00 PM
                        DefaultEndTime = new TimeSpan(22, 0, 0),   // 10:00 PM
                        DisplayOrder = 3,
                        IsActive = true,
                        RestaurantId = restaurant.Id // Assign to the current restaurant
                    };
                    var drinks = new MenuType
                    {
                        Name = "Drinks".Trim(),
                        Description = "Beverages menu, available all day",
                        DefaultStartTime = new TimeSpan(7, 0, 0),  // 7:00 AM
                        DefaultEndTime = new TimeSpan(22, 0, 0),   // 10:00 PM
                        DisplayOrder = 4,
                        IsActive = true,
                        RestaurantId = restaurant.Id // Assign to the current restaurant
                    };

                    // Add them while guarding against validation exceptions from EF
                    dbContext.MenuTypes.AddRange(breakfast, lunch, dinner, drinks);
                }

                try
                {
                    dbContext.SaveChanges();
                }
                catch (DbUpdateException dbex)
                {
                    // Log and rethrow with context - conflicts may occur if unique index violated
                    Console.WriteLine($"⚠️ MenuTypes seeding encountered an error: {dbex.Message}");
                    throw;
                }
            }

            // Seed Menus
            if (!dbContext.Menus.Any())
            {
                var restaurants = dbContext.Restaurants.Include(r => r.MenuTypes).ToList();
                foreach (var restaurant in restaurants)
                {
                    foreach (var menuType in restaurant.MenuTypes)
                    {
                        var menu = new Menu
                        {
                            Name = $"{restaurant.Name} - {menuType.Name} Menu",
                            Description = $"{menuType.Description} at {restaurant.Name}",
                            MenuTypeId = menuType.Id,
                            RestaurantId = restaurant.Id,
                            // Use the MenuType's default times for menu availability
                            AvailableFrom = menuType.DefaultStartTime,
                            AvailableTo = menuType.DefaultEndTime,
                            IsActive = true // All menus start as active by default
                        };
                        dbContext.Menus.Add(menu);
                    }
                }
                dbContext.SaveChanges();
            }

            // Seed Categories (per restaurant)
            if (Bistro == null || GreenLeaf == null)
                throw new Exception("❌ Required restaurants could not be loaded for categories.");

            if (!dbContext.Categories.Any())
            {
                var bistroCategories = new[]
                {
                    new Category 
                    { 
                        Name = "Italian", 
                        Description = "Traditional Italian cuisine including pasta, pizza, and authentic dishes",
                        DisplayOrder = 1,
                        IsActive = true,
                        RestaurantId = Bistro.Id 
                    },
                    new Category 
                    { 
                        Name = "Grill", 
                        Description = "Grilled specialties including steaks, burgers, and grilled vegetables",
                        DisplayOrder = 2,
                        IsActive = true,
                        RestaurantId = Bistro.Id 
                    },
                    new Category 
                    { 
                        Name = "Dessert", 
                        Description = "Sweet treats and desserts including cakes, ice cream, and pastries",
                        DisplayOrder = 3,
                        IsActive = true,
                        RestaurantId = Bistro.Id 
                    }
                };

                var greenLeafCategories = new[]
                {
                    new Category 
                    { 
                        Name = "Salad", 
                        Description = "Fresh and healthy salads with organic ingredients",
                        DisplayOrder = 1,
                        IsActive = true,
                        RestaurantId = GreenLeaf.Id 
                    },
                    new Category 
                    { 
                        Name = "Vegan", 
                        Description = "Plant-based dishes and vegan alternatives",
                        DisplayOrder = 2,
                        IsActive = true,
                        RestaurantId = GreenLeaf.Id 
                    },
                    new Category 
                    { 
                        Name = "Drinks", 
                        Description = "Refreshing beverages, smoothies, and fresh juices",
                        DisplayOrder = 3,
                        IsActive = true,
                        RestaurantId = GreenLeaf.Id 
                    }
                };

                dbContext.Categories.AddRange(bistroCategories);
                dbContext.Categories.AddRange(greenLeafCategories);
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
                    Name = "Alice Johnson",
                    Email = "alice@example.com",
                    Username = "aliceuser",
                    PasswordHash = "hashedpassword1",
                    IsActive = true,
                    DateRegistered = DateTime.UtcNow.AddDays(-10),
                    LastActivityDate = DateTime.UtcNow.AddHours(-2),
                    PreferredLanguage = "en",
                    TimeZoneId = "America/New_York",
                    AcceptsMarketing = true,
                    PhoneNumber = "+1 (555) 123-4567",
                    PhoneNumberConfirmed = true,
                    Notes = "Regular customer, prefers vegetarian options"
                };

                var bob = new Customer
                {
                    Name = "Bob Smith",
                    Email = "bob@example.com",
                    Username = "bobuser",
                    PasswordHash = "hashedpassword2",
                    IsActive = true,
                    DateRegistered = DateTime.UtcNow.AddDays(-8),
                    LastActivityDate = DateTime.UtcNow.AddDays(-1),
                    PreferredLanguage = "en",
                    TimeZoneId = "America/New_York",
                    AcceptsMarketing = false,
                    PhoneNumber = "+1 (555) 234-5678",
                    PhoneNumberConfirmed = true
                };

                var charlie = new Customer
                {
                    Name = "Charlie Brown",
                    Email = "charlie@example.com",
                    Username = "charlieuser",
                    PasswordHash = "hashedpassword3",
                    IsActive = false,
                    DateRegistered = DateTime.UtcNow.AddDays(-5),
                    LastActivityDate = DateTime.UtcNow.AddDays(-4),
                    PreferredLanguage = "es",
                    TimeZoneId = "America/Chicago",
                    AcceptsMarketing = true,
                    PhoneNumber = "+1 (555) 345-6789",
                    PhoneNumberConfirmed = false
                };

                var diana = new Customer
                {
                    Name = "Diana Miller",
                    Email = "diana@example.com",
                    Username = "dianauser",
                    PasswordHash = "hashedpassword4",
                    IsActive = true,
                    DateRegistered = DateTime.UtcNow.AddDays(-3),
                    LastActivityDate = DateTime.UtcNow.AddHours(-1),
                    PreferredLanguage = "en",
                    TimeZoneId = "America/Los_Angeles",
                    AcceptsMarketing = true,
                    PhoneNumber = "+1 (555) 456-7890",
                    PhoneNumberConfirmed = true,
                    Notes = "Premium customer, interested in wine pairing events"
                };

                var eric = new Customer
                {
                    Name = "Eric Davis",
                    Email = "eric@example.com",
                    Username = "ericuser",
                    PasswordHash = "hashedpassword5",
                    IsActive = true,
                    DateRegistered = DateTime.UtcNow.AddDays(-2),
                    LastActivityDate = DateTime.UtcNow.AddHours(-4),
                    PreferredLanguage = "fr",
                    TimeZoneId = "America/New_York",
                    AcceptsMarketing = false,
                    PhoneNumber = "+1 (555) 567-8901",
                    PhoneNumberConfirmed = false
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

                // SaleRecord validation:
                // - QuantitySold must be >= 0
                // - SaleDate should not be in the future
                // Index for sales analysis centralized in AppDbContext (IX_SaleRecords_Restaurant_Dish_Date)

                dbContext.SaleRecords.AddRange(
                    // Pizza Margherita
                    new SaleRecord { DishId = pizza.Id, RestaurantId = pizza.RestaurantId, QuantitySold = 50, SaleDate = DateTime.UtcNow.Date.AddDays(-1) },
                    new SaleRecord { DishId = pizza.Id, RestaurantId = pizza.RestaurantId, QuantitySold = 30, SaleDate = DateTime.UtcNow.Date.AddDays(-2) },
                    new SaleRecord { DishId = pizza.Id, RestaurantId = pizza.RestaurantId, QuantitySold = 40, SaleDate = DateTime.UtcNow.Date.AddDays(-3) },
                    // Spaghetti Carbonara
                    new SaleRecord { DishId = spaghetti.Id, RestaurantId = spaghetti.RestaurantId, QuantitySold = 25, SaleDate = DateTime.UtcNow.Date.AddDays(-1) },
                    new SaleRecord { DishId = spaghetti.Id, RestaurantId = spaghetti.RestaurantId, QuantitySold = 20, SaleDate = DateTime.UtcNow.Date.AddDays(-2) },
                    // Lasagna
                    new SaleRecord { DishId = lasagna.Id, RestaurantId = lasagna.RestaurantId, QuantitySold = 15, SaleDate = DateTime.UtcNow.Date.AddDays(-1) },
                    // Grilled Steak
                    new SaleRecord { DishId = steak.Id, RestaurantId = steak.RestaurantId, QuantitySold = 18, SaleDate = DateTime.UtcNow.Date.AddDays(-2) },
                    // Classic Burger
                    new SaleRecord { DishId = burger.Id, RestaurantId = burger.RestaurantId, QuantitySold = 22, SaleDate = DateTime.UtcNow.Date.AddDays(-3) },
                    // Tiramisu
                    new SaleRecord { DishId = tiramisu.Id, RestaurantId = tiramisu.RestaurantId, QuantitySold = 12, SaleDate = DateTime.UtcNow.Date.AddDays(-1) },
                    // Cheesecake
                    new SaleRecord { DishId = cheesecake.Id, RestaurantId = cheesecake.RestaurantId, QuantitySold = 10, SaleDate = DateTime.UtcNow.Date.AddDays(-2) },
                    // Caesar Salad
                    new SaleRecord { DishId = caesar.Id, RestaurantId = caesar.RestaurantId, QuantitySold = 20, SaleDate = DateTime.UtcNow.Date.AddDays(-3) },
                    new SaleRecord { DishId = caesar.Id, RestaurantId = caesar.RestaurantId, QuantitySold = 15, SaleDate = DateTime.UtcNow.Date.AddDays(-4) },
                    // Greek Salad
                    new SaleRecord { DishId = greek.Id, RestaurantId = greek.RestaurantId, QuantitySold = 13, SaleDate = DateTime.UtcNow.Date.AddDays(-2) },
                    // Tofu Stir Fry
                    new SaleRecord { DishId = tofu.Id, RestaurantId = tofu.RestaurantId, QuantitySold = 9, SaleDate = DateTime.UtcNow.Date.AddDays(-1) },
                    // Vegan Power Bowl
                    new SaleRecord { DishId = veganBowl.Id, RestaurantId = veganBowl.RestaurantId, QuantitySold = 8, SaleDate = DateTime.UtcNow.Date.AddDays(-2) },
                    // Fresh Lemonade
                    new SaleRecord { DishId = lemonade.Id, RestaurantId = lemonade.RestaurantId, QuantitySold = 17, SaleDate = DateTime.UtcNow.Date.AddDays(-1) },
                    // Iced Coffee
                    new SaleRecord { DishId = coffee.Id, RestaurantId = coffee.RestaurantId, QuantitySold = 14, SaleDate = DateTime.UtcNow.Date.AddDays(-2) }
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
                var alice = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Alice Johnson"));
                var bob = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Bob Smith"));
                var charlie = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Charlie Brown"));
                var diana = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Diana Miller")); 
                var eric = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Eric Davis"));
                if (pizza == null || spaghetti == null || lasagna == null || steak == null || burger == null || tiramisu == null || cheesecake == null || caesar == null || greek == null || tofu == null || veganBowl == null || lemonade == null || coffee == null)
                    throw new Exception("❌ Required dishes could not be loaded for reviews.");
                if (alice == null || bob == null || charlie == null || diana == null || eric == null)
                    throw new Exception("❌ Required customers could not be loaded for reviews.");

                // Ensure seeded reviews conform to validation rules: Rating 1-5, SentimentScore 0.0-1.0, lengths
                dbContext.Reviews.AddRange(
                    // Pizza Margherita
                    new Review {
                        CustomerId = alice.Id,
                        Customer = alice,
                        CustomerName = alice.Name,
                        Comment = "Great food!",
                        SentimentScore = 0.9,
                        DishId = pizza.Id,
                        RestaurantId = pizza.RestaurantId,
                        DateCreated = DateTime.UtcNow.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        UpdatedAt = DateTime.UtcNow.AddDays(-3),
                        Rating = 5,
                        IsDeleted = false
                    },
                    new Review {
                        CustomerId = bob.Id,
                        Customer = bob,
                        CustomerName = bob.Name,
                        Comment = "Crust was a bit hard.",
                        SentimentScore = 0.6,
                        DishId = pizza.Id,
                        RestaurantId = pizza.RestaurantId,
                        DateCreated = DateTime.UtcNow.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        UpdatedAt = DateTime.UtcNow.AddDays(-2),
                        Rating = 3,
                        IsDeleted = false
                    },
                    new Review {
                        CustomerId = diana.Id,
                        Customer = diana,
                        CustomerName = diana.Name,
                        Comment = "Loved the cheese!",
                        SentimentScore = 0.8,
                        DishId = pizza.Id,
                        RestaurantId = pizza.RestaurantId,
                        DateCreated = DateTime.UtcNow.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1),
                        Rating = 4,
                        IsDeleted = false
                    },
                    new Review {
                        CustomerId = null,
                        CustomerName = "Anonymous",
                        Comment = "Too cheesy for me.",
                        SentimentScore = 0.4,
                        DishId = pizza.Id,
                        RestaurantId = pizza.RestaurantId,
                        DateCreated = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Rating = 2,
                        IsDeleted = false
                    },
                    new Review {
                        CustomerId = null,
                        CustomerName = "Guest",
                        Comment = "Best pizza in town!",
                        SentimentScore = 0.95,
                        DishId = pizza.Id,
                        RestaurantId = pizza.RestaurantId,
                        DateCreated = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Rating = 5,
                        IsDeleted = false
                    },

                    // Spaghetti Carbonara
                    new Review { CustomerId = charlie.Id, Customer = charlie, CustomerName = charlie.Name, Comment = "Loved the sauce!", SentimentScore = 0.85, DishId = spaghetti.Id, RestaurantId = spaghetti.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-4), Rating = 5 },
                    new Review { CustomerId = eric.Id, Customer = eric, CustomerName = eric.Name, Comment = "Could use more bacon.", SentimentScore = 0.7, DishId = spaghetti.Id, RestaurantId = spaghetti.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-3), Rating = 4 },
                    new Review { CustomerId = null, CustomerName = "Anonymous", Comment = "Too salty.", SentimentScore = 0.3, DishId = spaghetti.Id, RestaurantId = spaghetti.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 2 },
                    new Review { CustomerId = bob.Id, Customer = bob, CustomerName = bob.Name, Comment = "Good but not great.", SentimentScore = 0.6, DishId = spaghetti.Id, RestaurantId = spaghetti.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 3 },

                    // Lasagna
                    new Review { CustomerId = alice.Id, Customer = alice, CustomerName = alice.Name, Comment = "Delicious layers!", SentimentScore = 0.92, DishId = lasagna.Id, RestaurantId = lasagna.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-5), Rating = 5 },
                    new Review { CustomerId = diana.Id, Customer = diana, CustomerName = diana.Name, Comment = "Tasty and filling!", SentimentScore = 0.85, DishId = lasagna.Id, RestaurantId = lasagna.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 4 },
                    new Review { 
                        CustomerId = null, 
                        CustomerName = "Guest", 
                        Comment = "Portion was small.", 
                        SentimentScore = 0.5, 
                        DishId = lasagna.Id, 
                        RestaurantId = lasagna.RestaurantId, 
                        DateCreated = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Rating = 3,
                        IsDeleted = false
                    },

                    // Grilled Steak
                    new Review { CustomerId = bob.Id, Customer = bob, CustomerName = bob.Name, Comment = "Perfectly cooked!", SentimentScore = 0.95, DishId = steak.Id, RestaurantId = steak.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 5 },
                    new Review { CustomerId = eric.Id, Customer = eric, CustomerName = eric.Name, Comment = "A bit overdone.", SentimentScore = 0.5, DishId = steak.Id, RestaurantId = steak.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 3 },
                    new Review { 
                        CustomerId = null, 
                        CustomerName = "Anonymous", 
                        Comment = "A bit tough.", 
                        SentimentScore = 0.4, 
                        DishId = steak.Id, 
                        RestaurantId = steak.RestaurantId, 
                        DateCreated = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Rating = 2,
                        IsDeleted = false
                    },

                    // Classic Burger
                    new Review { CustomerId = charlie.Id, Customer = charlie, CustomerName = charlie.Name, Comment = "Juicy and tasty!", SentimentScore = 0.88, DishId = burger.Id, RestaurantId = burger.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-3), Rating = 4 },
                    new Review { CustomerId = diana.Id, Customer = diana, CustomerName = diana.Name, Comment = "Loved the fries!", SentimentScore = 0.9, DishId = burger.Id, RestaurantId = burger.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 5 },
                    new Review { 
                        CustomerId = null, 
                        CustomerName = "Guest", 
                        Comment = "Bun was stale.", 
                        SentimentScore = 0.2, 
                        DishId = burger.Id, 
                        RestaurantId = burger.RestaurantId, 
                        DateCreated = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Rating = 1,
                        IsDeleted = false
                    },

                    // Tiramisu
                    new Review { CustomerId = alice.Id, Customer = alice, CustomerName = alice.Name, Comment = "Best tiramisu ever!", SentimentScore = 0.95, DishId = tiramisu.Id, RestaurantId = tiramisu.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-3), Rating = 5 },
                    new Review { CustomerId = bob.Id, Customer = bob, CustomerName = bob.Name, Comment = "Too sweet for my taste.", SentimentScore = 0.4, DishId = tiramisu.Id, RestaurantId = tiramisu.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 2 },
                    new Review { CustomerId = diana.Id, Customer = diana, CustomerName = diana.Name, Comment = "Loved the chocolate flavor!", SentimentScore = 0.8, DishId = tiramisu.Id, RestaurantId = tiramisu.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-1), Rating = 4 },

                    // Cheesecake
                    new Review { CustomerId = alice.Id, Customer = alice, CustomerName = alice.Name, Comment = "Creamy and delicious!", SentimentScore = 0.9, DishId = cheesecake.Id, RestaurantId = cheesecake.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-4), Rating = 5 },
                    new Review { CustomerId = bob.Id, Customer = bob, CustomerName = bob.Name, Comment = "Crust was too hard.", SentimentScore = 0.5, DishId = cheesecake.Id, RestaurantId = cheesecake.RestaurantId, DateCreated = DateTime.UtcNow.AddDays(-2), Rating = 3 },
                    new Review { CustomerId = null, CustomerName = "Guest", Comment = "Best dessert ever!", SentimentScore = 0.95, DishId = cheesecake.Id, RestaurantId = cheesecake.RestaurantId, DateCreated = DateTime.UtcNow, Rating = 5 }
                );
                dbContext.SaveChanges();
            }

            // Seed Tables
            if (!dbContext.Tables.Any())
            {
                if (Bistro == null || GreenLeaf == null)
                    throw new Exception("❌ Required restaurants could not be loaded for tables.");

                // Ensure seeded tables conform to validation rules:
                // - TableNumber is required and max length 20
                // - Capacity must be between 1 and 100
                // Index for table availability is centralized in AppDbContext (IX_Tables_Restaurant_Availability_Capacity)

                var nowTables = DateTime.UtcNow;

                var bistroTables = new[]
                {
                    new Table 
                    { 
                        TableNumber = "1", 
                        Capacity = 2, 
                        RestaurantId = Bistro.Id,
                        IsAvailable = true, // Ready for immediate seating
                        CreatedAt = nowTables,
                        UpdatedAt = nowTables,
                        IsDeleted = false,
                        IsActive = true
                    },
                    new Table 
                    { 
                        TableNumber = "2", 
                        Capacity = 4, 
                        RestaurantId = Bistro.Id,
                        IsAvailable = true, // Ready for immediate seating
                        CreatedAt = nowTables,
                        UpdatedAt = nowTables,
                        IsDeleted = false,
                        IsActive = true
                    },
                    new Table 
                    { 
                        TableNumber = "3", 
                        Capacity = 6, 
                        RestaurantId = Bistro.Id,
                        IsAvailable = false, // Under maintenance
                        CreatedAt = nowTables,
                        UpdatedAt = nowTables,
                        IsDeleted = false,
                        IsActive = true
                    }
                };

                var greenLeafTables = new[]
                {
                    new Table 
                    { 
                        TableNumber = "A1", 
                        Capacity = 4, 
                        RestaurantId = GreenLeaf.Id,
                        IsAvailable = true, // Ready for immediate seating
                        CreatedAt = nowTables,
                        UpdatedAt = nowTables,
                        IsDeleted = false,
                        IsActive = true
                    },
                    new Table 
                    { 
                        TableNumber = "A2", 
                        Capacity = 4, 
                        RestaurantId = GreenLeaf.Id,
                        IsAvailable = true, // Ready for immediate seating
                        CreatedAt = nowTables,
                        UpdatedAt = nowTables,
                        IsDeleted = false,
                        IsActive = true
                    },
                    new Table 
                    { 
                        TableNumber = "B1", 
                        Capacity = 8, 
                        RestaurantId = GreenLeaf.Id,
                        IsAvailable = false, // Being cleaned
                        CreatedAt = nowTables,
                        UpdatedAt = nowTables,
                        IsDeleted = false,
                        IsActive = true
                    }
                };

                dbContext.Tables.AddRange(bistroTables);
                dbContext.Tables.AddRange(greenLeafTables);
                dbContext.SaveChanges();
            }

            // Seed Promotions
            if (!dbContext.Promotions.Any())
            {
                if (Bistro == null || GreenLeaf == null)
                    throw new Exception("❌ Required restaurants could not be loaded for promotions.");

                // Promotion validation:
                // - Name required and <= 150 chars
                // - DiscountAmount must be >= 0
                // - ValidFrom must be <= ValidTo

                var bistroPromoActive = new Promotion
                {
                    Name = "Weekend Special",
                    DiscountAmount = 5.00m,
                    ValidFrom = DateTime.UtcNow.Date,
                    ValidTo = DateTime.UtcNow.Date.AddDays(7),
                    RestaurantId = Bistro.Id,
                    IsActive = true // Currently running promotion
                };

                var bistroPromoInactive = new Promotion
                {
                    Name = "Early Bird Special",
                    DiscountAmount = 10.00m,
                    ValidFrom = DateTime.UtcNow.Date.AddDays(-30),
                    ValidTo = DateTime.UtcNow.Date.AddDays(30),
                    RestaurantId = Bistro.Id,
                    IsActive = false // Temporarily paused promotion
                };

                var greenLeafPromoActive = new Promotion
                {
                    Name = "Lunch Deal",
                    DiscountAmount = 15.0m,
                    ValidFrom = DateTime.UtcNow.Date,
                    ValidTo = DateTime.UtcNow.Date.AddMonths(1),
                    RestaurantId = GreenLeaf.Id,
                    IsActive = true // Active promotion
                };

                var greenLeafPromoFuture = new Promotion
                {
                    Name = "Summer Special",
                    DiscountAmount = 20.0m,
                    ValidFrom = DateTime.UtcNow.Date.AddMonths(1),
                    ValidTo = DateTime.UtcNow.Date.AddMonths(3),
                    RestaurantId = GreenLeaf.Id,
                    IsActive = true // Ready to start when date comes
                };

                dbContext.Promotions.AddRange(bistroPromoActive, bistroPromoInactive, greenLeafPromoActive, greenLeafPromoFuture);
                dbContext.SaveChanges();
            }

            // Seed Order Statuses
            if (!dbContext.Set<OrderStatus>().Any())
            {
                var restaurants = dbContext.Restaurants.ToList();
                if (!restaurants.Any())
                    throw new Exception("❌ Restaurants must be seeded before order statuses.");

                foreach (var restaurant in restaurants)
                {
                    // OrderStatus validation rules (Name required, max 50 chars; ColorCode must be '#RRGGBB' if provided)
                    var statuses = new[]
                    {
                        new OrderStatus { Name = "Pending", Description = "Order received, waiting for confirmation.", DisplayOrder = 1, IsTerminal = false, ColorCode = "#FFA500", RestaurantId = restaurant.Id },
                        new OrderStatus { Name = "Preparing", Description = "The kitchen is preparing the order.", DisplayOrder = 2, IsTerminal = false, ColorCode = "#1E90FF", RestaurantId = restaurant.Id },
                        new OrderStatus { Name = "Ready", Description = "The order is ready for pickup or delivery.", DisplayOrder = 3, IsTerminal = false, ColorCode = "#32CD32", RestaurantId = restaurant.Id },
                        new OrderStatus { Name = "Completed", Description = "The order has been delivered/picked up.", DisplayOrder = 4, IsTerminal = true, ColorCode = "#008000", RestaurantId = restaurant.Id },
                        new OrderStatus { Name = "Cancelled", Description = "The order has been cancelled.", DisplayOrder = 5, IsTerminal = true, ColorCode = "#FF0000", RestaurantId = restaurant.Id }
                    };
                    dbContext.Set<OrderStatus>().AddRange(statuses);
                }
                dbContext.SaveChanges();
            }

            // Seed Orders and OrderItems
            if (!dbContext.Orders.Any())
            {
                var customers = dbContext.Customers.Where(c => c.IsActive).ToList();
                var restaurantsWithDishes = dbContext.Restaurants.Include(r => r.Dishes).Include(r => r.OrderStatuses).ToList();

                if (!customers.Any() || !restaurantsWithDishes.Any())
                    throw new Exception("❌ Required data for seeding orders is missing.");

                var bistro = restaurantsWithDishes.FirstOrDefault(r => r.Name == "Urban Bistro");
                var greenLeaf = restaurantsWithDishes.FirstOrDefault(r => r.Name == "Green Leaf");
                // Replace exact-name lookups with substring Contains to tolerate full-name vs first-name mismatches
                var alice = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Alice"));
                var bob = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Bob"));

                if (bistro == null || greenLeaf == null || alice == null || bob == null)
                    throw new Exception("❌ Specific entities for order seeding could not be loaded.");

                var pizza = bistro.Dishes.FirstOrDefault(d => d.Name == "Pizza Margherita");
                var burger = bistro.Dishes.FirstOrDefault(d => d.Name == "Classic Burger");
                var caesarSalad = greenLeaf.Dishes.FirstOrDefault(d => d.Name == "Caesar Salad");
    
                if (pizza == null || burger == null || caesarSalad == null)
                    throw new Exception("❌ Specific dishes for order seeding could not be loaded.");

                // choose staff member to assign as handler if available
                var handler = dbContext.StaffMembers.FirstOrDefault(s => s.RestaurantId == bistro.Id && s.IsActive);

                // NOTE: OrderItem validation rules (Quantity >= 1, UnitPrice >= 0) are enforced by data annotations
                // on `OrderItem`. Indexes for common OrderItem queries (by OrderId, DishId, RestaurantId) are
                // centralized in `AppDbContext.OnModelCreating` (see IX_OrderItems_* indexes). Keep seeded values
                // within valid ranges to avoid validation exceptions.

                var order1Items = new List<OrderItem>
                {
                    new OrderItem { DishId = pizza.Id, Quantity = 1, UnitPrice = pizza.DishPrice, RestaurantId = bistro.Id },
                    new OrderItem { DishId = burger.Id, Quantity = 2, UnitPrice = burger.DishPrice, RestaurantId = bistro.Id }
                };
                var order1 = new Order
                {
                    CustomerId = alice.Id,
                    RestaurantId = bistro.Id,
                    OrderStatusId = bistro.OrderStatuses.First(s => s.Name == "Completed").Id,
                    OrderDate = DateTime.UtcNow.AddDays(-2),
                    SpecialInstructions = "Please provide extra napkins.",
                    OrderItems = order1Items,
                    TotalAmount = order1Items.Sum(oi => oi.Quantity * oi.UnitPrice),
                    HandledByStaffId = handler?.Id
                };

                var order2Items = new List<OrderItem>
                {
                    new OrderItem { DishId = caesarSalad.Id, Quantity = 2, UnitPrice = caesarSalad.DishPrice, RestaurantId = greenLeaf.Id }
                };
                var order2 = new Order
                {
                    CustomerId = bob.Id,
                    RestaurantId = greenLeaf.Id,
                    OrderStatusId = greenLeaf.OrderStatuses.First(s => s.Name == "Pending").Id,
                    OrderDate = DateTime.UtcNow,
                    OrderItems = order2Items,
                    TotalAmount = order2Items.Sum(oi => oi.Quantity * oi.UnitPrice),
                    HandledByStaffId = null
                };

                dbContext.Orders.AddRange(order1, order2);
                dbContext.SaveChanges();
            }

            // Seed Customer Loyalty
            if (!dbContext.CustomerLoyalties.Any())
            {
                var alice = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Alice"));
                var bob = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Bob"));
                var diana = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Diana"));

                if (alice == null || bob == null || diana == null || Bistro == null || GreenLeaf == null)
                    throw new Exception("❌ Required customers or restaurants could not be loaded for loyalty seeding.");

                var aliceAtBistro = new CustomerLoyalty
                {
                    CustomerId = alice.Id,
                    RestaurantId = Bistro.Id,
                    Points = 150,
                    LifetimePoints = 200,
                    Tier = LoyaltyTier.Silver,
                    LastActivity = DateTime.UtcNow.AddDays(-2)
                };

                var bobAtBistro = new CustomerLoyalty
                {
                    CustomerId = bob.Id,
                    RestaurantId = Bistro.Id,
                    Points = 40,
                    LifetimePoints = 40,
                    Tier = LoyaltyTier.Bronze,
                    LastActivity = DateTime.UtcNow.AddDays(-5)
                };

                var dianaAtGreenLeaf = new CustomerLoyalty
                {
                    CustomerId = diana.Id,
                    RestaurantId = GreenLeaf.Id,
                    Points = 550,
                    LifetimePoints = 600,
                    Tier = LoyaltyTier.Gold,
                    LastActivity = DateTime.UtcNow.AddDays(-1)
                };

                dbContext.CustomerLoyalties.AddRange(aliceAtBistro, bobAtBistro, dianaAtGreenLeaf);
                dbContext.SaveChanges(); // Save to get IDs for transactions

                // Seed Loyalty Transactions
                if (!dbContext.LoyaltyTransactions.Any())
                {
                    dbContext.LoyaltyTransactions.AddRange(
                        // Alice at Urban Bistro
                        new LoyaltyTransaction { CustomerLoyaltyId = aliceAtBistro.Id, CustomerId = alice.Id, RestaurantId = Bistro.Id, PointsChange = 100, Type = LoyaltyTransactionType.OrderEarning, Description = "Dinner purchase", BalanceAfter = 100, CreatedAt = DateTime.UtcNow.AddDays(-10) },
                        new LoyaltyTransaction { CustomerLoyaltyId = aliceAtBistro.Id, CustomerId = alice.Id, RestaurantId = Bistro.Id, PointsChange = 100, Type = LoyaltyTransactionType.OrderEarning, Description = "Lunch special", BalanceAfter = 200, CreatedAt = DateTime.UtcNow.AddDays(-5) },
                        new LoyaltyTransaction { CustomerLoyaltyId = aliceAtBistro.Id, CustomerId = alice.Id, RestaurantId = Bistro.Id, PointsChange = -50, Type = LoyaltyTransactionType.RewardRedemption, Description = "Redeemed for discount", BalanceAfter = 150, CreatedAt = DateTime.UtcNow.AddDays(-2) },

                        // Bob at Urban Bistro
                        new LoyaltyTransaction { CustomerLoyaltyId = bobAtBistro.Id, CustomerId = bob.Id, RestaurantId = Bistro.Id, PointsChange = 40, Type = LoyaltyTransactionType.Bonus, Description = "First visit bonus", BalanceAfter = 40, CreatedAt = DateTime.UtcNow.AddDays(-5) },

                        // Diana at Green Leaf
                        new LoyaltyTransaction { CustomerLoyaltyId = dianaAtGreenLeaf.Id, CustomerId = diana.Id, RestaurantId = GreenLeaf.Id, PointsChange = 200, Type = LoyaltyTransactionType.OrderEarning, Description = "Catering order", BalanceAfter = 200, CreatedAt = DateTime.UtcNow.AddDays(-20) },
                        new LoyaltyTransaction { CustomerLoyaltyId = dianaAtGreenLeaf.Id, CustomerId = diana.Id, RestaurantId = GreenLeaf.Id, PointsChange = 400, Type = LoyaltyTransactionType.OrderEarning, Description = "Weekend dining", BalanceAfter = 600, CreatedAt = DateTime.UtcNow.AddDays(-10) },
                        new LoyaltyTransaction { CustomerLoyaltyId = dianaAtGreenLeaf.Id, CustomerId = diana.Id, RestaurantId = GreenLeaf.Id, PointsChange = -50, Type = LoyaltyTransactionType.RewardRedemption, Description = "Redeemed for free drink", BalanceAfter = 550, CreatedAt = DateTime.UtcNow.AddDays(-1) }
                    );
                    dbContext.SaveChanges();
                }
            }

            // Seed Reservations
            // Note: Reservation validation is enforced on the model:
            // - TableId must be a positive integer
            // - ReservationTime must be within a reasonable window (not older than 1 day, not more than 1 year in the future)
            // - CustomerId is optional to allow anonymous reservations
            // Indexes for reservation queries are centralized in `AppDbContext.OnModelCreating` (see IX_Reservations_Restaurant_Time and IX_Reservations_Table_Time)
            if (!dbContext.Reservations.Any())
            {
                var alice = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Alice"));
                var bob = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Bob"));
                var diana = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Diana"));
                var eric = dbContext.Customers.FirstOrDefault(c => c.Name.Contains("Eric"));

                // Get tables from both restaurants
                var bistroTables = dbContext.Tables.Where(t => t.RestaurantId == Bistro.Id).ToList();
                var greenLeafTables = dbContext.Tables.Where(t => t.RestaurantId == GreenLeaf.Id).ToList();

                if (alice == null || bob == null || diana == null || eric == null || 
                    !bistroTables.Any() || !greenLeafTables.Any())
                    throw new Exception("❌ Required entities for reservation seeding could not be loaded.");

                // Use DateTime.UtcNow to ensure times are stored in UTC and fall within the validation window
                var nowDate = DateTime.UtcNow.Date;

                var reservations = new List<Reservation>
                {
                    // Today's reservations at Urban Bistro
                    new Reservation
                    {
                        CustomerId = alice.Id,
                        TableId = bistroTables.First(t => t.Capacity == 4).Id,  // 4-seater table
                        ReservationTime = nowDate.AddHours(19),    // Today 7 PM UTC
                        RestaurantId = Bistro.Id
                    },
                    new Reservation
                    {
                        CustomerId = bob.Id,
                        TableId = bistroTables.First(t => t.Capacity == 2).Id,  // 2-seater table
                        ReservationTime = nowDate.AddHours(20),    // Today 8 PM UTC
                        RestaurantId = Bistro.Id
                    },

                    // Tomorrow's reservations at Urban Bistro
                    new Reservation
                    {
                        CustomerId = diana.Id,
                        TableId = bistroTables.First(t => t.Capacity == 6).Id,  // 6-seater table
                        ReservationTime = nowDate.AddDays(1).AddHours(18), // Tomorrow 6 PM UTC
                        RestaurantId = Bistro.Id
                    },

                    // Future reservations at Urban Bistro (within one year)
                    new Reservation
                    {
                        CustomerId = alice.Id,
                        TableId = bistroTables.First(t => t.Capacity == 4).Id,  // 4-seater table
                        ReservationTime = nowDate.AddDays(5).AddHours(19), // In 5 days, 7 PM UTC
                        RestaurantId = Bistro.Id
                    },

                    // Today's reservations at Green Leaf
                    new Reservation
                    {
                        CustomerId = eric.Id,
                        TableId = greenLeafTables.First(t => t.Capacity == 4).Id, // 4-seater table
                        ReservationTime = nowDate.AddHours(12),      // Today 12 PM (Lunch) UTC
                        RestaurantId = GreenLeaf.Id
                    },
                    new Reservation
                    {
                        CustomerId = diana.Id,
                        TableId = greenLeafTables.First(t => t.TableNumber == "A2").Id,
                        ReservationTime = nowDate.AddHours(13),      // Today 1 PM (Lunch) UTC
                        RestaurantId = GreenLeaf.Id
                    },

                    // Tomorrow's reservations at Green Leaf
                    new Reservation
                    {
                        CustomerId = bob.Id,
                        TableId = greenLeafTables.First(t => t.Capacity == 8).Id, // 8-seater table
                        ReservationTime = nowDate.AddDays(1).AddHours(19), // Tomorrow 7 PM UTC
                        RestaurantId = GreenLeaf.Id
                    },

                    // Weekend reservations at Green Leaf (within one year)
                    new Reservation
                    {
                        CustomerId = alice.Id,
                        TableId = greenLeafTables.First(t => t.TableNumber == "A1").Id,
                        ReservationTime = nowDate.AddDays(7).AddHours(18), // Next week 6 PM UTC
                        RestaurantId = GreenLeaf.Id
                    }
                };

                dbContext.Reservations.AddRange(reservations);
                dbContext.SaveChanges();
            }
        }

        private static void ClearTables(AppDbContext dbContext)
        {
            // Disable constraints and delete data in specific order

            // PostgreSQL command to defer foreign key constraint checks until transaction commit.
            // This allows deleting data from related tables in any order within the transaction.
            // Without this, we would need to carefully order deletions to respect foreign key relationships.
            dbContext.Database.ExecuteSqlRaw("SET CONSTRAINTS ALL DEFERRED;");

            // Delete in reverse order of dependencies
            dbContext.StaffSchedules.RemoveRange(dbContext.StaffSchedules);
            dbContext.Reservations.RemoveRange(dbContext.Reservations);
            dbContext.Tables.RemoveRange(dbContext.Tables);
            dbContext.OrderItems.RemoveRange(dbContext.OrderItems);
            dbContext.Orders.RemoveRange(dbContext.Orders);
            dbContext.OrderStatuses.RemoveRange(dbContext.OrderStatuses);
            dbContext.Promotions.RemoveRange(dbContext.Promotions);
            dbContext.LoyaltyTransactions.RemoveRange(dbContext.LoyaltyTransactions);
            dbContext.CustomerLoyalties.RemoveRange(dbContext.CustomerLoyalties);
            dbContext.Reviews.RemoveRange(dbContext.Reviews);
            dbContext.SaleRecords.RemoveRange(dbContext.SaleRecords);
            dbContext.Dishes.RemoveRange(dbContext.Dishes);
            dbContext.Categories.RemoveRange(dbContext.Categories);
            dbContext.Menus.RemoveRange(dbContext.Menus);
            dbContext.MenuTypes.RemoveRange(dbContext.MenuTypes);
            dbContext.Restaurants.RemoveRange(dbContext.Restaurants);
            dbContext.StaffMembers.RemoveRange(dbContext.StaffMembers);
            dbContext.AdminUsers.RemoveRange(dbContext.AdminUsers);
            dbContext.BusinessRules.RemoveRange(dbContext.BusinessRules);

            dbContext.SaveChanges();

            // Re-enable constraints
            dbContext.Database.ExecuteSqlRaw("SET CONSTRAINTS ALL IMMEDIATE;");
        }
    }
}
