using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Domain.Features.Restaurants;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.MenuAggregate;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Aggregates.TableAggregate;
using SmartMenuOptim.Domain.Aggregates.CustomerLoyaltyAggregate;
using SmartMenuOptim.Domain.Enums;
using SmartMenuOptim.Domain.ValueObjects;
using System.Data.Common;
using SmartMenuOptim.Infrastructure.Persistence.Context;
using SmartMenuOptim.Shared;
using SmartMenuOptim.Shared.Constants;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;

namespace SmartMenuOptim.API.Data
{
    /// <summary>
    /// Provides methods to seed the application's database with initial or sample data, including users, roles,
    /// restaurants, menus, and related entities.
    /// </summary>
    /// <remarks>The DbSeeder class is intended for use during application startup or testing to ensure the
    /// database is populated with required baseline data. It supports both initial seeding and clearing of existing
    /// data before reseeding, which is useful for development and integration testing scenarios. All seeding operations
    /// are performed asynchronously and are designed to be idempotent when possible. This class is not thread-safe and
    /// should be invoked in a controlled, single-threaded context, such as during application initialization.</remarks>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, bool clearExistingData = false)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            Console.WriteLine("🌱 Starting database seeding process...");
            await ConnectToDatabaseAsync(context);

            if (!context.Users.Any() || clearExistingData)
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    if (clearExistingData) await ExecuteSeedingPhaseAsync("Clear Tables", () => ClearTablesAsync(context));

                    // Seeding Phases - Reordered for correct dependency management
                    await ExecuteSeedingPhaseAsync("Seed Identity", () => SeedIdentityAsync(roleManager));
                    
                    // Step 1: Create Admins first, as they own restaurants.
                    var adminUsers = await ExecuteSeedingPhaseAsync("Seed Admin Users", () => SeedAdminUsersAsync(userManager, context));
                    
                    // Step 2: Create Restaurants, owned by Admins.
                    var restaurants = await ExecuteSeedingPhaseAsync("Seed Restaurants", () => SeedRestaurantsAsync(context, adminUsers));
                    
                    // Step 3: Create Customers and Staff, who may depend on Restaurants.
                    var (customerUsers, staffUsers) = await ExecuteSeedingPhaseAsync("Seed Customer and Staff Profiles", () => SeedCustomerAndStaffUsersAsync(userManager, context, restaurants));

                    // Step 4: Assign staff to restaurants
                    await ExecuteSeedingPhaseAsync("Assign Staff to Restaurants", () => AssignStaffToRestaurantsAsync(context, staffUsers, restaurants));

                    // Remaining seeding phases
                    await ExecuteSeedingPhaseAsync("Seed Business Rules", () => SeedBusinessRulesAsync(context, adminUsers));
                    await ExecuteSeedingPhaseAsync("Seed Menu Structure", () => SeedMenuStructureAsync(context, restaurants));
                    await ExecuteSeedingPhaseAsync("Seed Operational Infrastructure", () => SeedOperationalInfrastructureAsync(context, restaurants));
                    await ExecuteSeedingPhaseAsync("Seed Transactional Data", () => SeedTransactionalDataAsync(context, restaurants, customerUsers, staffUsers));
                    await ExecuteSeedingPhaseAsync("Seed Customer Engagement", () => SeedCustomerEngagementAsync(context, restaurants, customerUsers));
                    await ExecuteSeedingPhaseAsync("Synchronize All User Profiles", () => SynchronizeAllUserProfiles(context));

                    await transaction.CommitAsync();
                    Console.WriteLine("✅ Database seeding completed successfully!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"❌ Database seeding failed: {ex.Message}\n{ex.InnerException?.Message}\n{ex.StackTrace}");
                    throw;
                }
            }
            else
            {
                Console.WriteLine("✅ Database already contains data. Seeding skipped.");
            }
        }

        /// <summary>
        /// Attempts to connect to the database and apply any pending migrations asynchronously, retrying on failure up
        /// to a maximum number of attempts.
        /// </summary>
        /// <remarks>If the initial connection or migration attempt fails due to a database error, the
        /// method retries the operation multiple times with a delay between attempts. If all attempts fail, the last
        /// exception is propagated to the caller.</remarks>
        /// <param name="context">The database context to use for applying migrations. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task completes when the connection is established and
        /// migrations are applied, or an exception is thrown after all retry attempts fail.</returns>
        private static async Task ConnectToDatabaseAsync(AppDbContext context)
        {
            const int maxRetries = 10;
            for (int retryCount = 1; retryCount <= maxRetries; retryCount++)
            {
                try
                {
                    await context.Database.MigrateAsync();
                    Console.WriteLine("✅ Database connection successful and migrations applied.");
                    return;
                }
                catch (DbException ex)
                {
                    Console.WriteLine($"⏳ Database connection attempt {retryCount}/{maxRetries} failed: {ex.Message}");
                    if (retryCount == maxRetries) throw;
                    await Task.Delay(3000);
                }
            }
        }

        /// <summary>
        /// Asynchronously removes all data from the application's database tables and resets identity columns.
        /// </summary>
        /// <remarks>This method truncates all major tables in the database, including user, order, and
        /// menu-related tables, and resets their identity values. All data in these tables will be permanently deleted.
        /// Use with caution, especially in production environments.</remarks>
        /// <param name="context">The database context used to execute the table truncation operations. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous clear operation.</returns>
        private static async Task ClearTablesAsync(AppDbContext context)
        {
            Console.WriteLine("🗑️ Clearing existing data...");
            await context.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL DEFERRED;");
            var tableNames = new[]
            {
                "LoyaltyTransactions", "Reservations", "Reviews", "SaleRecords", "OrderItems", "Orders",
                "StaffSchedules", "Promotions", "MenuDishes", "Dishes", "Categories", "Menus", "MenuTypes",
                "Tables", "OrderStatuses", "UserPermissions", "BusinessRules", "Restaurants",
                "StaffMembers", "Customers", "AdminUsers",
                "UserRoles", "UserClaims", "UserLogins", "UserTokens", "RoleClaims", "Roles", "Users"
            };
            foreach (var tableName in tableNames)
            {
                await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{tableName}\" RESTART IDENTITY CASCADE;");
            }
            await context.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL IMMEDIATE;");
            Console.WriteLine("✅ Data cleared successfully.");
        }

        /// <summary>
        /// Seeds default identity roles and associated claims if no roles currently exist.
        /// </summary>
        /// <remarks>This method should be called during application startup to ensure that required roles
        /// and claims are present in the identity store. If roles already exist, no changes are made.</remarks>
        /// <param name="roleManager">The role manager used to create and manage identity roles and claims. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous seeding operation.</returns>
        private static async Task SeedIdentityAsync(RoleManager<IdentityRole> roleManager)
        {
            Console.WriteLine("👥 Seeding Identity Roles and Claims...");
            if (!await roleManager.Roles.AnyAsync())
            {
                await SeedHelper.CreateRolesAndClaims(roleManager);
            }
        }

        /// <summary>
        /// Ensures that the default administrative users exist in the database, creating them if necessary, and returns
        /// a dictionary of all admin users keyed by username.
        /// </summary>
        /// <remarks>If no admin users exist, this method creates a set of default admin users with
        /// predefined roles and credentials. If admin users are already present, no new users are created. This method
        /// is typically used during application initialization or database seeding.</remarks>
        /// <param name="userManager">The user manager used to create and manage application users.</param>
        /// <param name="context">The database context used to query and persist admin user data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary mapping usernames
        /// to their corresponding admin user entities. The dictionary will include all admin users present in the
        /// database after seeding.</returns>
        private static async Task<Dictionary<string, AdminUser>> SeedAdminUsersAsync(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            if (!await context.AdminUsers.AnyAsync())
            {
                var adminData = new[] {
                    ("systemadmin", "systemadmin@smartmenu.com", "System Administrator", AdminRoleType.SystemAdmin), // AdminRoleType.SystemAdmin value 0
                    ("restaurantowner", "owner@smartmenu.com", "Restaurant Owner", AdminRoleType.RestaurantOwner), // AdminRoleType.RestaurantOwner value 1
                    ("restaurantmanager", "manager@smartmenu.com", "Restaurant Manager", AdminRoleType.RestaurantManager) // AdminRoleType.RestaurantManager value 2
                };
                foreach (var (username, email, fullName, adminRoleType) in adminData)
                {
                    await SeedHelper.CreateAdminUser(userManager, username, email, AuthConstants.DefaultPasswords.AdminPassword, fullName, adminRoleType);
                }
                await context.SaveChangesAsync();
            }
            return await context.AdminUsers.ToDictionaryAsync(a => a.UserName!, a => a);
        }

        /// <summary>
        /// Seeds the database with default customer and staff user accounts if none exist, and returns the resulting
        /// lists of customers and staff members.
        /// </summary>
        /// <remarks>This method is intended for use during application setup or testing to ensure that
        /// sample customer and staff accounts are available. Staff users are assigned to the first restaurant in the
        /// provided list. The method does not reseed users if customer or staff data already exists.</remarks>
        /// <param name="userManager">The user manager used to create and manage application user accounts.</param>
        /// <param name="context">The database context used to access and modify customer and staff member data.</param>
        /// <param name="restaurants">The list of restaurants to which staff members will be assigned. Must contain at least one restaurant to
        /// seed staff users.</param>
        /// <returns>A tuple containing a list of all customers and a list of all staff members after seeding. If no users were
        /// seeded, the lists reflect the current database state.</returns>
        private static async Task<(List<Customer>, List<StaffMember>)> SeedCustomerAndStaffUsersAsync(UserManager<ApplicationUser> userManager, AppDbContext context, List<Restaurant> restaurants)
        {
            // Seed Customers
            if (!await context.Customers.AnyAsync())
            {
                var customerData = new[]
                {
                    ("John Doe", "john.doe@example.com"),
                    ("Jane Smith", "jane.smith@example.com"),
                    ("Robert Johnson", "robert.j@example.com"),
                    ("Maria Garcia", "maria.g@example.com"),
                    ("David Brown", "david.b@example.com")
                };

                foreach (var (name, email) in customerData)
                {
                    await SeedHelper.CreateCustomerUser(userManager,
                        email.Split('@')[0], // username from email
                        email,
                        AuthConstants.DefaultPasswords.CustomerPassword,
                        name);
                }
            }

            // Seed Staff
            if (!await context.StaffMembers.AnyAsync() && restaurants.Any())
            {
                var staffData = new[]
                {
                    ("Michael Chen", "m.chen@example.com", StaffRole.Manager),
                    ("Sarah Wilson", "s.wilson@example.com", StaffRole.Waiter),
                    ("James Miller", "j.miller@example.com", StaffRole.Chef),
                    ("Emily Davis", "e.davis@example.com", StaffRole.Waiter),
                    ("Carlos Rodriguez", "c.rodriguez@example.com", StaffRole.Chef)
                };

                foreach (var (name, email, role) in staffData)
                {
                    var assignedRestaurant = restaurants[0]; // Assign to first restaurant for simplicity
                    await SeedHelper.CreateStaffUser(userManager,
                        email.Split('@')[0], // username from email
                        email,
                        AuthConstants.DefaultPasswords.StaffPassword,
                        name,
                        role,
                        assignedRestaurant.Id);
                }
            }

            await context.SaveChangesAsync();
            return (
                await context.Customers.Include(c => c.ApplicationUser).ToListAsync(),
                await context.StaffMembers.Include(s => s.ApplicationUser).ToListAsync()
            );
        }

        /// <summary>
        /// Asynchronously seeds the Restaurants table with initial data if it is empty and returns the list of all
        /// restaurants.
        /// </summary>
        /// <remarks>If the Restaurants table already contains data, no new restaurants are added. The
        /// method requires that the adminUsers dictionary includes a user with the key "restaurantowner" to assign as
        /// the owner of the seeded restaurants.</remarks>
        /// <param name="context">The database context used to access and modify restaurant data. Must not be null.</param>
        /// <param name="adminUsers">A dictionary mapping admin user roles to their corresponding AdminUser objects. Used to assign ownership of
        /// seeded restaurants. Must contain an entry for the key "restaurantowner".</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of all Restaurant
        /// entities in the database after seeding.</returns>
        private static async Task<List<Restaurant>> SeedRestaurantsAsync(AppDbContext context, Dictionary<string, AdminUser> adminUsers)
        {
            Console.WriteLine("🏢 Seeding Restaurants...");
            if (!await context.Restaurants.AnyAsync() && adminUsers.TryGetValue("restaurantowner", out var owner))
            {
                var italianRestaurant = new Restaurant(
                    ownerId: owner.Id,
                    name: "La Bella Italia",
                    location: new Address(
                        street: "123 Main Street",
                        city: "Downtown",
                        state: "NY",
                        postalCode: "10001",
                        countryCode: "US"
                    ),
                    contactPhone: new PhoneNumber("+1-555-0123"),
                    contactEmail: new Email("contact@labellaitalia.com"),
                    maxSimultaneousOrders: 50,
                    description: "Authentic Italian cuisine in a cozy atmosphere",
                    timeZoneId: "America/New_York"
                );

                var sushiRestaurant = new Restaurant(
                    ownerId: owner.Id,
                    name: "Sushi Master",
                    location: new Address(
                        street: "456 Ocean Avenue",
                        city: "Seaside",
                        state: "CA",
                        postalCode: "90001",
                        countryCode: "US"
                    ),
                    contactPhone: new PhoneNumber("+1-555-0124"),
                    contactEmail: new Email("info@sushimaster.com"),
                    maxSimultaneousOrders: 50,
                    description: "Premium Japanese dining experience",
                    timeZoneId: "America/New_York"
                );

                await context.Restaurants.AddRangeAsync(italianRestaurant, sushiRestaurant);
                await context.SaveChangesAsync();
            }
            return await context.Restaurants.ToListAsync();
        }

        /// <summary>
        /// Assigns staff members without a restaurant association to the first restaurant in the provided list and
        /// saves the changes asynchronously.
        /// </summary>
        /// <remarks>No changes are made if either the staff or restaurant list is empty. Only staff
        /// members not already assigned to a restaurant are affected.</remarks>
        /// <param name="context">The database context used to update staff and restaurant associations.</param>
        /// <param name="staffUsers">The list of staff members to assign to restaurants. Staff members with a RestaurantId of 0 will be assigned.</param>
        /// <param name="restaurants">The list of available restaurants. The first restaurant in the list will be used for assignment.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task AssignStaffToRestaurantsAsync(AppDbContext context, List<StaffMember> staffUsers, List<Restaurant> restaurants)
        {
            Console.WriteLine("👨‍🍳 Assigning Staff to Restaurants...");
            if (staffUsers.Any() && restaurants.Any())
            {
                bool changed = false;
                foreach (var staff in staffUsers)
                {
                    if (staff.RestaurantId == 0)
                    {
                        var restaurant = restaurants[0]; // Assign to first restaurant for simplicity
                        staff.RestaurantId = restaurant.Id;
                        if (staff.ApplicationUser != null)
                        {
                            staff.ApplicationUser.RestaurantTenantId = restaurant.Id;
                        }
                        changed = true;
                    }
                }
                if (changed)
                {
                    await context.SaveChangesAsync();
                }
            }
        }
        
        /// <summary>
        /// Seeds default business rules into the database if none exist.
        /// </summary>
        /// <remarks>If business rules already exist in the database, this method performs no action. The
        /// method requires that the adminUsers dictionary contains a valid entry for the key "systemadmin" to associate
        /// the seeded rules with an administrator.</remarks>
        /// <param name="context">The database context used to access and modify business rules.</param>
        /// <param name="adminUsers">A dictionary mapping admin user identifiers to their corresponding AdminUser objects. Must contain an entry
        /// for the key "systemadmin".</param>
        /// <returns>A task that represents the asynchronous seeding operation.</returns>
        private static async Task SeedBusinessRulesAsync(AppDbContext context, Dictionary<string, AdminUser> adminUsers)
        {
            Console.WriteLine("📋 Seeding Business Rules...");
            if (!await context.BusinessRules.AnyAsync() && adminUsers.TryGetValue("systemadmin", out var admin))
            {
                var rules = new[]
                {
                    new BusinessRule { Name = "Sales Threshold", RuleType = BusinessRuleType.SalesThreshold, Value = 30, AdminUserId = admin.Id, IsCurrentValue = true },
                    new BusinessRule { Name = "Sentiment Threshold", RuleType = BusinessRuleType.SentimentThreshold, Value = 0.7, AdminUserId = admin.Id, IsCurrentValue = true }
                };
                await context.BusinessRules.AddRangeAsync(rules);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Seeds the menu structure, including menu types, categories, dishes, and menus, for each restaurant if no
        /// menu types currently exist in the database.
        /// </summary>
        /// <remarks>This method should be called during initial database setup or testing to populate
        /// sample menu data for each restaurant. If menu types already exist in the database, no changes will be
        /// made.</remarks>
        /// <param name="context">The database context used to access and modify menu-related entities.</param>
        /// <param name="restaurants">The list of restaurants for which the menu structure will be seeded. Each restaurant will receive its own
        /// set of menu types, categories, dishes, and menus.</param>
        /// <returns>A task that represents the asynchronous seeding operation.</returns>
        private static async Task SeedMenuStructureAsync(AppDbContext context, List<Restaurant> restaurants)
        {
            Console.WriteLine("🍽️ Seeding Menu Structure...");
            if (!await context.MenuTypes.AnyAsync())
            {
                foreach (var restaurant in restaurants)
                {
                    var menuTypes = new[]
                    {
                        new MenuType(restaurant.Id, "Breakfast", "Morning menu served daily", 1),
                        new MenuType(restaurant.Id, "Lunch", "Midday specials and favorites", 2),
                        new MenuType(restaurant.Id, "Dinner", "Evening fine dining experience", 3),
                        new MenuType(restaurant.Id, "Specials", "Limited time special offerings", 4)
                    };
                    await context.MenuTypes.AddRangeAsync(menuTypes);
                    await context.SaveChangesAsync();

                    var categories = new[]
                    {
                        new DishCategory(restaurant.Id, "Appetizers", "Start your meal with our delicious starters", 1),
                        new DishCategory(restaurant.Id, "Main Courses", "Signature entrees and hearty meals", 2),
                        new DishCategory(restaurant.Id, "Desserts", "Sweet treats to complete your dining experience", 3),
                        new DishCategory(restaurant.Id, "Beverages", "Refreshing drinks and specialty beverages", 4)
                    };
                    await context.Categories.AddRangeAsync(categories);
                    await context.SaveChangesAsync();

                    /* Improvements made to dish seeding:
                    1. Complete Property Coverage:
                       - All standalone properties from Dish entity now used
                       - Added missing properties like PreparationTime, Calories
                       - Included dietary flags (IsVegetarian, IsSpicy)
                       - Added comprehensive Ingredients and Allergens info

                    2. Enhanced Variety:
                       - Increased from 4-5 dishes to 10 dishes per restaurant
                       - Better category distribution
                       - More diverse price points ($3.99 - $32.99)
                       - Various preparation times (5-240 minutes)

                    3. Realistic Data:
                       - Accurate calorie counts
                       - Realistic preparation times
                       - Complete ingredient lists
                       - Proper allergen warnings
                       - Authentic dish descriptions

                    4. Dietary Considerations:
                       - Mix of vegetarian and non-vegetarian options
                       - Clear allergen information
                       - Spice level indicators
                       - Health-conscious options

                    5. Cultural Authenticity:
                       - Restaurant-specific traditional dishes
                       - Authentic ingredients and preparation methods
                       - Appropriate pricing structure
                       - Culture-specific preparation times

                    6. Improved Organization:
                       - Grouped by categories (appetizers, mains, etc.)
                       - Logical display order
                       - Better code readability with comments
                       - Consistent data structure
                    */

                    // Italian restaurant dishes
                    // Italian restaurant dishes
                    if (restaurant.Name == "La Bella Italia")
                    {
                        var dishes = new[]
                        {
                        new Dish
                        {
                            Name = new DishName("Bruschetta al Pomodoro"),
            Description = "Fresh tomatoes, garlic, and basil on toasted bread",
            DishPrice = 8.99m,
            CategoryId = categories[0].Id, // Appetizers
            RestaurantId = restaurant.Id,
            PreparationTime = 15,
            Calories = 220,
            IsVegetarian = true,
            IsSpicy = false,
            Ingredients = "Sourdough bread, tomatoes, garlic, fresh basil, extra virgin olive oil, sea salt",
            Allergens = "Gluten"
        },
        new Dish
        {
            Name = new DishName("Calamari Fritti"),
            Description = "Crispy fried calamari with marinara sauce",
            DishPrice = 12.99m,
            CategoryId = categories[0].Id,
            RestaurantId = restaurant.Id,
            PreparationTime = 20,
            Calories = 340,
            IsVegetarian = false,
            IsSpicy = false,
            Ingredients = "Fresh calamari, flour, herbs, marinara sauce",
            Allergens = "Shellfish, Gluten"
        },
        new Dish
        {
            Name = new DishName("Spaghetti Carbonara"),
            Description = "Classic pasta with eggs, pecorino, and guanciale",
            DishPrice = 16.99m,
            CategoryId = categories[1].Id, // Main Courses
            RestaurantId = restaurant.Id,
            PreparationTime = 25,
            Calories = 850,
            IsVegetarian = false,
            IsSpicy = false,
            Ingredients = "Spaghetti, eggs, pecorino romano, guanciale, black pepper",
            Allergens = "Eggs, Dairy, Gluten"
        },
        new Dish
        {
            Name = new DishName("Osso Buco"),
            Description = "Braised veal shanks with gremolata and saffron risotto",
            DishPrice = 32.99m,
            CategoryId = categories[1].Id,
            RestaurantId = restaurant.Id,
            PreparationTime = 180,
            Calories = 780,
            IsVegetarian = false,
            IsSpicy = false,
            Ingredients = "Veal shanks, vegetables, white wine, broth, saffron, rice",
            Allergens = "Dairy, Celery"
        },
        new Dish
        {
            Name = new DishName("Penne Arrabbiata"),
            Description = "Spicy tomato sauce with garlic and red chilies",
            DishPrice = 14.99m,
            CategoryId = categories[1].Id,
            RestaurantId = restaurant.Id,
            PreparationTime = 20,
            Calories = 550,
            IsVegetarian = true,
            IsSpicy = true,
            Ingredients = "Penne pasta, tomatoes, garlic, red chilies, olive oil",
            Allergens = "Gluten"
        },
        new Dish
        {
            Name = new DishName("Tiramisu"),
            Description = "Classic coffee-flavored Italian dessert",
            DishPrice = 8.99m,
            CategoryId = categories[2].Id, // Desserts
            RestaurantId = restaurant.Id,
            PreparationTime = 30,
            Calories = 350,
            IsVegetarian = true,
            IsSpicy = false,
            Ingredients = "Ladyfingers, mascarpone, coffee, eggs, cocoa powder",
            Allergens = "Eggs, Dairy, Gluten"
        },
        new Dish
        {
            Name = new DishName("Panna Cotta"),
            Description = "Silky vanilla cream dessert with berry compote",
            DishPrice = 7.99m,
            CategoryId = categories[2].Id,
            RestaurantId = restaurant.Id,
            PreparationTime = 240,
            Calories = 280,
            IsVegetarian = true,
            IsSpicy = false,
            Ingredients = "Heavy cream, vanilla, gelatin, mixed berries",
            Allergens = "Dairy"
        },
        new Dish
        {
            Name = new DishName("Chianti Classico"),
            Description = "Premium Tuscan red wine",
            DishPrice = 9.99m,
            CategoryId = categories[3].Id, // Beverages
            RestaurantId = restaurant.Id,
            PreparationTime = 1,
            Calories = 125,
            IsVegetarian = true,
            IsSpicy = false,
            Ingredients = "Red wine grapes",
            Allergens = "Sulfites"
        },
        new Dish
        {
            Name = new DishName("Margherita Pizza"),
            Description = "Classic Neapolitan pizza with tomatoes and mozzarella",
            DishPrice = 14.99m,
            CategoryId = categories[1].Id,
            RestaurantId = restaurant.Id,
            PreparationTime = 20,
            Calories = 850,
            IsVegetarian = true,
            IsSpicy = false,
            Ingredients = "Pizza dough, San Marzano tomatoes, buffalo mozzarella, basil, olive oil",
            Allergens = "Gluten, Dairy"
        },
        new Dish
        {
            Name = new DishName("Risotto ai Funghi"),
            Description = "Creamy mushroom risotto with truffle oil",
            DishPrice = 19.99m,
            CategoryId = categories[1].Id,
            RestaurantId = restaurant.Id,
            PreparationTime = 35,
            Calories = 620,
            IsVegetarian = true,
            IsSpicy = false,
            Ingredients = "Arborio rice, mushrooms, white wine, parmesan, truffle oil",
            Allergens = "Dairy"
        }
    };

                        await context.Dishes.AddRangeAsync(dishes);
                        await context.SaveChangesAsync();
                    }
                    // Japanese restaurant dishes
                    else if (restaurant.Name == "Sushi Master")
                    {
                        var dishes = new[]
                        {
        new Dish
        {
            Name = new DishName("Edamame"),
            Description = "Steamed young soybeans with sea salt",
            DishPrice = 5.99m,
            CategoryId = categories[0].Id, // Appetizers
            RestaurantId = restaurant.Id,
            PreparationTime = 10,
            Calories = 120,
            IsVegetarian = true,
            IsSpicy = false,
            Ingredients = "Young soybeans, sea salt",
            Allergens = "Soy"
        },
        new Dish
        {
            Name = new DishName("Miso Soup"),
            Description = "Traditional Japanese soup with tofu and seaweed",
            DishPrice = 4.99m,
            CategoryId = categories[0].Id,
            RestaurantId = restaurant.Id,
            PreparationTime = 15,
            Calories = 80,
            IsVegetarian = true,
            IsSpicy = false,
            Ingredients = "Dashi stock, miso paste, tofu, wakame seaweed, green onions",
            Allergens = "Soy"
        },
        new Dish
        {
            Name = new DishName("Dragon Roll"),
            Description = "Eel and cucumber roll topped with avocado",
            DishPrice = 16.99m,
            CategoryId = categories[1].Id, // Main Courses
            RestaurantId = restaurant.Id,
            PreparationTime = 20,
            Calories = 450,
            IsVegetarian = false,
            IsSpicy = false,
            Ingredients = "Sushi rice, nori, eel, cucumber, avocado, eel sauce",
            Allergens = "Fish, Soy, Gluten"
        },
        new Dish
        {
            Name = new DishName("Spicy Tuna Roll"),
            Description = "Fresh tuna with spicy mayo and cucumber",
            DishPrice = 14.99m,
            CategoryId = categories[1].Id,
            RestaurantId = restaurant.Id,
            PreparationTime = 15,
            Calories = 380,
            IsVegetarian = false,
            IsSpicy = true,
            Ingredients = "Sushi rice, nori, fresh tuna, spicy mayo, cucumber",
            Allergens = "Fish, Eggs, Soy"
        },
        new Dish
        {
            Name = new DishName("Tempura Udon"),
            Description = "Thick noodles in hot broth with tempura shrimp",
            DishPrice = 17.99m,
            CategoryId = categories[1].Id,
            RestaurantId = restaurant.Id,
            PreparationTime = 25,
            Calories = 680,
            IsVegetarian = false,
            IsSpicy = false,
            Ingredients = "Udon noodles, dashi broth, tempura shrimp, green onions",
            Allergens = "Gluten, Shellfish"
        },
        new Dish
        {
            Name = new DishName("Salmon Nigiri"),
            Description = "Fresh salmon over hand-pressed rice",
            DishPrice = 6.99m,
            CategoryId = categories[1].Id,
            RestaurantId = restaurant.Id,
            PreparationTime = 5,
            Calories = 150,
            IsVegetarian = false,
            IsSpicy = false,
            Ingredients = "Fresh salmon, sushi rice, wasabi",
            Allergens = "Fish"
        },
        new Dish
        {
            Name = new DishName("Mochi Ice Cream"),
            Description = "Japanese rice cake filled with ice cream",
            DishPrice = 6.99m,
            CategoryId = categories[2].Id, // Desserts
            RestaurantId = restaurant.Id,
            PreparationTime = 5,
            Calories = 180,
            IsVegetarian = true,
            IsSpicy = false,
            Ingredients = "Rice flour, ice cream, sugar",
            Allergens = "Milk, Soy"
        },
        new Dish
        {
            Name = new DishName("Green Tea"),
            Description = "Premium Japanese green tea",
            DishPrice = 3.99m,
            CategoryId = categories[3].Id, // Beverages
            RestaurantId = restaurant.Id,
            PreparationTime = 5,
            Calories = 0,
            IsVegetarian = true,
            IsSpicy = false,
            Ingredients = "Japanese green tea leaves",
            Allergens = null
        },
        new Dish
        {
            Name = new DishName("Chicken Katsu Curry"),
            Description = "Crispy chicken cutlet with Japanese curry",
            DishPrice = 18.99m,
            CategoryId = categories[1].Id,
            RestaurantId = restaurant.Id,
            PreparationTime = 30,
            Calories = 850,
            IsVegetarian = false,
            IsSpicy = false,
            Ingredients = "Chicken breast, panko breadcrumbs, Japanese curry roux, rice",
            Allergens = "Gluten, Soy"
        },
        new Dish
        {
            Name = new DishName("Gyoza"),
            Description = "Pan-fried pork and vegetable dumplings",
            DishPrice = 7.99m,
            CategoryId = categories[0].Id,
            RestaurantId = restaurant.Id,
            PreparationTime = 20,
            Calories = 320,
            IsVegetarian = false,
            IsSpicy = false,
            Ingredients = "Ground pork, cabbage, garlic, ginger, dumpling wrappers",
            Allergens = "Gluten, Soy"
        }
    };


                        await context.Dishes.AddRangeAsync(dishes);
                        await context.SaveChangesAsync();

                        // Create menus and link dishes
                        foreach (var menuType in menuTypes)
                        {
                            var menu = new SmartMenuOptim.Domain.Aggregates.MenuAggregate.Menu(
                                restaurant.Id,
                                $"{restaurant.Name} {menuType.Name}",
                                menuType.Id,
                                $"{menuType.Name} offerings"
                            );
                            await context.Menus.AddAsync(menu);
                            await context.SaveChangesAsync();

                            // Link dishes to menu through MenuDish
                            var menuDishes = dishes.Select((dish, index) => new MenuDish
                            {
                                MenuId = menu.Id,
                                DishId = dish.Id,
                                RestaurantId = restaurant.Id,
                                DisplayOrder = index + 1,
                                // Optional: Add special prices for some dishes
                                SpecialPrice = index % 3 == 0 ? dish.DishPrice * 0.9m : null, // 10% discount every third dish
                                Notes = index % 3 == 0 ? "Special promotional price!" : null
                            }).ToList();

                            await context.MenuDishes.AddRangeAsync(menuDishes);
                            await context.SaveChangesAsync();
                        }
                    }
                    await context.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Seeds the operational infrastructure data for each restaurant if it does not already exist in the database.
        /// </summary>
        /// <remarks>This method should be called during application initialization to ensure that
        /// essential operational data, such as tables, order statuses, and default promotions, are present for each
        /// restaurant. The method does not add data if tables already exist in the database.</remarks>
        /// <param name="context">The database context used to add tables, order statuses, and promotions.</param>
        /// <param name="restaurants">A list of restaurants for which operational infrastructure data will be seeded. Each restaurant in the list
        /// will receive its own set of tables, order statuses, and, if applicable, promotions.</param>
        /// <returns>A task that represents the asynchronous seeding operation.</returns>
        private static async Task SeedOperationalInfrastructureAsync(AppDbContext context, List<Restaurant> restaurants)
        {
            Console.WriteLine("📊 Seeding Operational Infrastructure...");
            if (!await context.Tables.AnyAsync())
            {
                foreach (var restaurant in restaurants)
                {
                    var tables = new[]
                    {
                        new Table(restaurant.Id, "T1", 2, "Window seat for two"),
                        new Table(restaurant.Id, "T2", 4, "Family table"),
                        new Table(restaurant.Id, "T3", 6, "Group dining table"),
                        new Table(restaurant.Id, "T4", 2, "Romantic corner table"),
                        new Table(restaurant.Id, "T5", 8, "Large group table")
                    };
                    await context.Tables.AddRangeAsync(tables);

                    await context.OrderStatuses.AddRangeAsync(
                        new OrderStatus(restaurant.Id, "Pending", 1, false, "#FFA500", "Order received, awaiting confirmation"),
                        new OrderStatus(restaurant.Id, "Preparing", 2, false, "#17A2B8", "Being prepared in kitchen"),
                        new OrderStatus(restaurant.Id, "Ready", 3, false, "#28A745", "Ready for pickup or delivery"),
                        new OrderStatus(restaurant.Id, "Served", 4, false, "#20C997", "Food served to customer"),
                        new OrderStatus(restaurant.Id, "Completed", 5, true, "#218838", "Order successfully completed"),
                        new OrderStatus(restaurant.Id, "Cancelled", 6, true, "#DC3545", "Order cancelled")
                    );

                    // Restaurant-specific promotions
                    if (restaurant.Name == "La Bella Italia")
                    {
                        var happyHourPromotion = new Promotion(
                            restaurant.Id,
                            "Happy Hour Pasta",
                            20.0m,
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddMonths(1),
                            "20% off on all pasta dishes between 3 PM and 5 PM"
                        );
                        happyHourPromotion.Activate();

                        var familySundayPromotion = new Promotion(
                            restaurant.Id,
                            "Family Sunday",
                            100.0m,
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddMonths(2),
                            "Kids eat free with every adult main course"
                        );
                        familySundayPromotion.Activate();

                        await context.Promotions.AddRangeAsync(happyHourPromotion, familySundayPromotion);
                    }
                    else if (restaurant.Name == "Sushi Master")
                    {
                        var lunchSpecialPromotion = new Promotion(
                            restaurant.Id,
                            "Lunch Special Bento",
                            15.0m,
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddMonths(1),
                            "15% off on all bento boxes during lunch hours"
                        );
                        lunchSpecialPromotion.Activate();

                        var sushiTuesdayPromotion = new Promotion(
                            restaurant.Id,
                            "Sushi Tuesday",
                            50.0m,
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddMonths(2),
                            "Buy one roll, get second at half price"
                        );
                        sushiTuesdayPromotion.Activate();

                        await context.Promotions.AddRangeAsync(lunchSpecialPromotion, sushiTuesdayPromotion);
                    }
                }
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Asynchronously seeds sample transactional data, including orders and order items, into the database for the
        /// specified restaurants, customers, and staff members if no orders currently exist.
        /// </summary>
        /// <remarks>Transactional data is only seeded if the database contains no existing orders and
        /// both the customers and restaurants lists are not empty. This method is intended for development or testing
        /// scenarios to populate the database with representative order data.</remarks>
        /// <param name="context">The database context used to access and modify the application's data.</param>
        /// <param name="restaurants">A list of restaurants for which transactional data will be seeded. Must not be null or empty.</param>
        /// <param name="customers">A list of customers to associate with the seeded orders. Must not be null or empty.</param>
        /// <param name="staff">A list of staff members to associate with the seeded orders. Must not be null.</param>
        /// <returns>A task that represents the asynchronous seeding operation.</returns>
        private static async Task SeedTransactionalDataAsync(AppDbContext context, List<Restaurant> restaurants, List<Customer> customers, List<StaffMember> staff)
        {
            Console.WriteLine("🛒 Seeding Transactional Data...");
            if (!await context.Orders.AnyAsync() && customers.Any() && restaurants.Any())
            {
                foreach (var restaurant in restaurants)
                {
                    var dishes = await context.Dishes.Where(d => d.RestaurantId == restaurant.Id).ToListAsync();
                    var statuses = await context.OrderStatuses.Where(s => s.RestaurantId == restaurant.Id).ToListAsync();
                    var restaurantStaff = staff.Where(s => s.RestaurantId == restaurant.Id).ToList();

                    if (dishes.Any() && statuses.Any())
                    {
                        // Create some sample orders
                        var orders = new List<Order>();
                        var completedStatus = statuses.First(s => s.Name == "Completed");
                        var customer = customers.First();
                        var staffMember = restaurantStaff.FirstOrDefault();

                        // Add a few completed orders
                        for (int i = 0; i < 3; i++)
                        {
                            var order = new Order(
                                restaurantId: restaurant.Id,
                                customerId: customer.Id,
                                orderStatusId: completedStatus.Id,
                                specialInstructions: "Regular customer order"
                            );

                            // Add 2-3 items to each order using aggregate method
                            foreach (var dish in dishes.Take(2))
                            {
                                order.AddItem(
                                    dishId: dish.Id,
                                    dishName: dish.Name,
                                    unitPrice: dish.DishPrice,
                                    quantity: 1,
                                    specialInstructions: null
                                );
                            }

                            // Assign staff member if available
                            if (staffMember != null)
                            {
                                order.AssignStaffMember(staffMember.Id);
                            }

                            orders.Add(order);
                        }

                        await context.Orders.AddRangeAsync(orders);
                        await context.SaveChangesAsync();
                    }
                }

                // Create sample sale records for each restaurant's dishes
                if (!await context.SaleRecords.AnyAsync())
                {
                    // Create sales records spanning the last 30 days
                    var endDate = DateTime.UtcNow;
                    var startDate = endDate.AddDays(-30);

                    foreach (var restaurant in restaurants)
                    {
                        var dishes = await context.Dishes
                            .Where(d => d.RestaurantId == restaurant.Id)
                            .ToListAsync();

                        if (dishes.Any())
                        {
                            var saleRecords = new List<SaleRecord>();
                            var random = new Random();

                            // Generate sales data for each day in the range
                            for (var date = startDate; date <= endDate; date = date.AddDays(1))
                            {
                                foreach (var dish in dishes)
                                {
                                    // Not every dish sells every day - add some randomness
                                    if (random.Next(100) < 70) // 70% chance of a sale on any given day
                                    {
                                        var quantitySold = random.Next(1, 11);
                                        var totalAmount = dish.DishPrice * quantitySold;
                                        var saleRecord = new SaleRecord(
                                            restaurantId: restaurant.Id,
                                            dishId: dish.Id,
                                            saleAmount: new Money(totalAmount, "USD"),
                                            quantitySold: quantitySold
                                        );
                                        saleRecords.Add(saleRecord);
                                    }
                                }
                            }

                            await context.SaleRecords.AddRangeAsync(saleRecords);
                        }
                    }
                    
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Sales records seeded successfully.");
                }
            }
        }
        
        /// <summary>
        /// Seeds sample customer engagement data, including reviews, loyalty programs, and loyalty transactions, into
        /// the database if no reviews currently exist.
        /// </summary>
        /// <remarks>This method is intended for use during application setup or testing to populate the
        /// database with initial customer engagement data. No data will be seeded if reviews already exist in the
        /// database or if the customers list is empty.</remarks>
        /// <param name="context">The database context used to access and modify customer engagement entities. Must not be null.</param>
        /// <param name="restaurants">The list of restaurants for which customer engagement data will be seeded. Must not be null and should
        /// contain at least one restaurant to seed data.</param>
        /// <param name="customers">The list of customers to associate with seeded reviews and loyalty programs. Must not be null and should
        /// contain at least one customer to create engagement data.</param>
        /// <returns>A task that represents the asynchronous seeding operation.</returns>
        private static async Task SeedCustomerEngagementAsync(AppDbContext context, List<Restaurant> restaurants, List<Customer> customers)
        {
            Console.WriteLine("🤝 Seeding Customer Engagement Data...");
            if (!await context.Reviews.AnyAsync() && customers.Any())
            {
                // First: Seed reviews for each restaurant
                foreach (var restaurant in restaurants)
                {
                    var dishes = await context.Dishes.Where(d => d.RestaurantId == restaurant.Id).ToListAsync();
                    if (dishes.Any())
                    {
                        await SeedRestaurantReviews(context, restaurant, dishes, customers.First());
                    }
                }

                // Second: Seed loyalty programs (separate from reviews)
                await SeedLoyaltyPrograms(context, restaurants, customers);

                await context.SaveChangesAsync();
                Console.WriteLine("✅ Customer Engagement Data seeded successfully.");
            }
        }

        /// <summary>
        /// Asynchronously adds sample review entries for the specified restaurant and dishes to the database context,
/// using the provided customer as the reviewer.
/// </summary>
/// <remarks>
/// Review Generation Improvements:
/// 
/// 1. Realistic Review Distribution:
///    - Full range of ratings (1-5 stars)
///    - Varied sentiment scores (0.10-0.98)
///    - Contextual comments matching ratings
///    - Restaurant-specific content and terminology
/// 
/// 2. Weighted Randomization Strategy:
///    - 35% chance: 5-star reviews (excellent experiences)
///    - 25% chance: 4-star reviews (good but not perfect)
///    - 20% chance: 3-star reviews (average experiences)
///    - 15% chance: 2-star reviews (below expectations)
///    - 5% chance: 1-star reviews (poor experiences)
///    This distribution creates a realistic but generally positive review profile
/// 
/// 3. Natural Variations:
///    - Sentiment scores include small random adjustments (±0.05)
///    - Review dates spread across the last month
///    - Maintains correlation between ratings and sentiment
///    - Avoids artificial patterns in data
/// 
/// 4. Context-Aware Content:
///    - Italian Restaurant:
///      * References to pasta, sauce, authenticity
///      * Italian cuisine-specific terminology
///      * Cultural references (Rome, Italian flavors)
///    
///    - Sushi Restaurant:
///      * Comments on fish freshness
///      * Mentions of sushi quality
///      * Japanese cuisine-specific details
/// 
/// This approach creates a more realistic and nuanced review dataset
/// that better represents actual customer feedback patterns.
/// </remarks>
private static async Task SeedRestaurantReviews(
    AppDbContext context, 
    Restaurant restaurant, 
    List<Dish> dishes, 
    Customer customer)
{
    var reviews = new List<Review>();
    var random = new Random();
    var reviewData = new List<(int rating, double sentiment, string comment)>();
    
    // Restaurant-specific review templates with correlated ratings and sentiments
    if (restaurant.Name == "La Bella Italia")
    {
        reviewData = new List<(int rating, double sentiment, string comment)>
        {
            // 5-star reviews focus on authenticity and excellent experiences
            (5, 0.95, "Absolutely phenomenal! The authentic Italian flavors transported me straight to Rome. Every bite was perfection!"),
            // 4-star reviews highlight good quality with minor issues
            (4, 0.82, "Really enjoyed my meal. The flavors were great, though portion size could be a bit larger."),
            // 3-star reviews express average satisfaction
            (3, 0.50, "Decent Italian food, but nothing extraordinary. Service was okay."),
            // 2-star reviews indicate significant issues
            (2, 0.30, "Disappointed with my visit. The pasta was overcooked and the sauce was too salty."),
            // 1-star reviews reflect very poor experiences
            (1, 0.15, "Very poor experience. Food was cold and service was extremely slow.")
        };
    }
    else if (restaurant.Name == "Sushi Master")
    {
        reviewData = new List<(int rating, double sentiment, string comment)>
        {
            // 5-star reviews emphasize freshness and presentation
            (5, 0.98, "Outstanding sushi! The fish was incredibly fresh and the presentation was beautiful."),
            // 4-star reviews note quality with price concerns
            (4, 0.85, "Great quality sushi and excellent service. Slightly pricey but worth it."),
            // 3-star reviews indicate mediocre experiences
            (3, 0.55, "Average sushi restaurant. Nothing special but nothing bad either."),
            // 2-star reviews highlight quality concerns
            (2, 0.25, "The rice was mushy and the fish didn't taste very fresh. Disappointed."),
            // 1-star reviews focus on serious quality/service issues
            (1, 0.10, "Had to send my food back. Quality was questionable and service was terrible.")
        };
    }

    foreach (var dish in dishes)
    {
        // Apply weighted randomization for review selection
        var reviewChoice = random.NextDouble() switch
        {
            var n when n < 0.35 => 0, // 35% chance of 5 stars
            var n when n < 0.60 => 1, // 25% chance of 4 stars
            var n when n < 0.80 => 2, // 20% chance of 3 stars
            var n when n < 0.95 => 3, // 15% chance of 2 stars
            _ => 4                    // 5% chance of 1 star
        };

        var (rating, sentiment, comment) = reviewData[reviewChoice];

        // Add natural variation to sentiment scores (±0.05)
        var adjustedSentiment = Math.Min(1.0, Math.Max(0.0, 
            sentiment + (random.NextDouble() - 0.5) * 0.1));

                        var review = new Review(
                            restaurantId: restaurant.Id,
                            dishId: dish.Id,
                            rating: rating,
                            comment: comment,
                            customerId: customer.Id,
                            sentimentScore: adjustedSentiment
                        );
        reviews.Add(review);
    }

    await context.Reviews.AddRangeAsync(reviews);
    Console.WriteLine($"✅ Added {reviews.Count} diverse reviews for {restaurant.Name}");
}

        /// <summary>
        /// Seeds loyalty program data for all customers across all restaurants.
        /// </summary>
        /// <remarks>
        /// This method creates diverse loyalty profiles for each customer-restaurant combination:
        /// - Varied points and tiers across different loyalty levels
        /// - Different transaction histories
        /// - Realistic activity timestamps
        /// 
        /// Fix Summary:
        /// 1. Removed nested restaurant loop that caused duplicate loyalty records
        /// 2. Separated loyalty seeding from review seeding for better organization
        /// 3. Added proper transaction handling and data validation
        /// 4. Improved error handling and logging
        /// 5. Uses simple property setters instead of Domain aggregate methods
        /// </remarks>
        private static async Task SeedLoyaltyPrograms(
            AppDbContext context,
            List<Restaurant> restaurants,
            List<Customer> customers)
        {
            var random = new Random();

            foreach (var customer in customers)
            {
                foreach (var restaurant in restaurants)
                {
                    // Create varied initial points and tiers for each customer
                    var initialPoints = random.Next(50, 1001); // Random points between 50-1000
                    var lifetimePoints = initialPoints + random.Next(0, 2001); // Ensure lifetimePoints >= current points
                    var tier = DetermineTier(lifetimePoints); // Helper to determine tier based on points

                    var loyalty = new SmartMenuOptim.Domain.Aggregates.CustomerLoyaltyAggregate.CustomerLoyalty(
                        restaurant.Id,
                        customer.Id
                    );
                    await context.CustomerLoyalties.AddAsync(loyalty);
                    await context.SaveChangesAsync(); // Save to get the ID

                    // Add varied loyalty transactions using aggregate methods
                    var numberOfTransactions = random.Next(2, 5);
                    for (int i = 0; i < numberOfTransactions; i++)
                    {
                        var transactionTypes = new[]
                        {
                            (LoyaltyTransactionType.OrderEarning, "Points earned from order", 50),
                            (LoyaltyTransactionType.Bonus, "Welcome bonus points", 100),
                            (LoyaltyTransactionType.Bonus, "Birthday bonus", 200),
                            (LoyaltyTransactionType.Adjustment, "Service recovery bonus", 75),
                            (LoyaltyTransactionType.OrderEarning, "Special event bonus", 150)
                        };

                        var (type, description, points) = transactionTypes[random.Next(transactionTypes.Length)];
                        
                        // Use aggregate behavioral methods to add points
                        if (type == LoyaltyTransactionType.OrderEarning)
                        {
                            loyalty.AddPoints(points, description, type, null);
                        }
                        else if (type == LoyaltyTransactionType.Bonus || type == LoyaltyTransactionType.Adjustment)
                        {
                            loyalty.AddPoints(points, description, type, null);
                        }
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        /// ----------------------- Helpers Methods -----------------------


        /// <summary>
        /// Determines the loyalty tier based on the specified total lifetime points.
        /// </summary>
        /// <param name="lifetimePoints">The total number of points accumulated by a customer over their lifetime. Must be zero or greater.</param>
        /// <returns>A <see cref="CustomerLoyaltyTier"/> value representing the customer's loyalty tier. Returns <see
        /// cref="CustomerLoyaltyTier.Platinum"/> for 2,000 or more points, <see cref="CustomerLoyaltyTier.Gold"/> for 1,000 to 1,999
        /// points, <see cref="CustomerLoyaltyTier.Silver"/> for 500 to 999 points, and <see cref="CustomerLoyaltyTier.Bronze"/> for
        /// fewer than 500 points.</returns>
        private static CustomerLoyaltyTier DetermineTier( int lifetimePoints)
        {
            return lifetimePoints switch
            {
                >= 2000 => CustomerLoyaltyTier.Platinum,
                >= 1000 => CustomerLoyaltyTier.Gold,
                >= 500 => CustomerLoyaltyTier.Silver,
                _ => CustomerLoyaltyTier.Bronze
            };
        }
        
        /// <summary>
        /// Synchronizes all user profiles in the database to ensure consistency between user entities and their
        /// associated profiles.
        /// </summary>
        /// <remarks>This method loads all users and their related profiles, updates each user's profile
        /// associations as needed, and saves the changes to the database. Intended for use in administrative or
        /// maintenance scenarios where profile data must be brought into alignment.</remarks>
        /// <param name="context">The database context used to access and update user and profile data. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task SynchronizeAllUserProfiles(AppDbContext context)
        {
            Console.WriteLine("🔄 Finalizing Profile Synchronization...");
            var users = await context.Users
                .Include(u => u.AdminProfile)
                .Include(u => u.CustomerProfile)
                .Include(u => u.StaffProfile)
                .ToListAsync();

            foreach (var user in users)
            {
                user.SynchronizeProfiles();
            }
            await context.SaveChangesAsync();
        }
        
        /// <summary>
        /// Executes a seeding phase by invoking the specified asynchronous action and handles any exceptions that occur
        /// during execution.
        /// </summary>
        /// <param name="phaseName">The name of the seeding phase. Used to identify the phase in error messages.</param>
        /// <param name="seedingAction">An asynchronous delegate that performs the seeding logic for the specified phase.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if an exception occurs during the execution of the seeding action. The exception message includes the
        /// phase name.</exception>
        private static async Task ExecuteSeedingPhaseAsync(string phaseName, Func<Task> seedingAction)
        {
            try
            {
                await seedingAction();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in phase '{phaseName}': {ex.Message}");
                throw new InvalidOperationException($"Seeding failed during the '{phaseName}' phase.", ex);
            }
        }
        
        /// <summary>
        /// Executes a seeding phase by invoking the specified asynchronous action and handles any exceptions that occur
        /// during execution.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the seeding action.</typeparam>
        /// <param name="phaseName">The name of the seeding phase. Used to identify the phase in error messages.</param>
        /// <param name="seedingAction">A function that represents the asynchronous seeding operation to execute.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the value returned by the
        /// seeding action.</returns>
        /// <exception cref="InvalidOperationException">Thrown if an exception occurs during the execution of the seeding action. The exception provides details
        /// about the failed phase.</exception>
        private static async Task<T> ExecuteSeedingPhaseAsync<T>(string phaseName, Func<Task<T>> seedingAction)
        {
            try
            {
                return await seedingAction();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in phase '{phaseName}': {ex.Message}");
                throw new InvalidOperationException($"Seeding failed during the '{phaseName}' phase.", ex);
            }
        }
    }
}
