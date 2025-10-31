using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using SmartMenuOptim.Shared.Data.Entities;
using SmartMenuOptim.Shared.Data.Entities.GlobalEntities;
using SmartMenuOptim.Shared.Data.Entities.TenantSpecificEntities;

namespace SmartMenuOptim.Shared.Data.Context
{
    public class AppDbContext : DbContext
    {
        // Global Entities
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<StaffMember> StaffMembers { get; set; }
        public DbSet<BusinessRule> BusinessRules { get; set; }

        // Tenant Specific Entities
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<MenuType> MenuTypes { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<SaleRecord> SaleRecords { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<StaffSchedule> StaffSchedules { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<CustomerLoyalty> CustomerLoyalties { get; set; }
        public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
        public DbSet<OrderStatus> OrderStatuses { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Business Rule Relationships
            modelBuilder.Entity<BusinessRule>()
                .HasOne(br => br.AdminUser)
                .WithMany(au => au.BusinessRules)
                .HasForeignKey(br => br.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add index on AdminUserId as it's used in relationships
            modelBuilder.Entity<BusinessRule>()
                .HasIndex(br => br.AdminUserId)
                .HasDatabaseName("IX_BusinessRules_AdminUserId");

            // Add composite index on RuleType and CreatedAt for efficient historical queries
            modelBuilder.Entity<BusinessRule>()
                .HasIndex(br => new { br.RuleType, br.CreatedAt })
                .HasDatabaseName("IX_BusinessRules_RuleType_CreatedAt");

            // Restaurant Relationships
            modelBuilder.Entity<Restaurant>()
                .HasOne(r => r.Owner)
                .WithMany(a => a.OwnedRestaurants)
                .HasForeignKey(r => r.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add index on OwnerId for restaurant queries
            modelBuilder.Entity<Restaurant>()
                .HasIndex(r => r.OwnerId)
                .HasDatabaseName("IX_Restaurants_OwnerId");

            // Menu Relationships
            modelBuilder.Entity<Menu>()
                .HasOne(m => m.Restaurant)
                .WithMany(r => r.Menus)
                .HasForeignKey(m => m.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add composite index for menu availability queries
            modelBuilder.Entity<Menu>()
                .HasIndex(m => new { m.RestaurantId, m.AvailableFrom, m.AvailableTo })
                .HasDatabaseName("IX_Menus_Restaurant_Availability");

            modelBuilder.Entity<Menu>()
                .HasOne(m => m.MenuType)
                .WithMany(mt => mt.Menus)
                .HasForeignKey(m => m.MenuTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many-to-many: Menu <-> Dish
            // Many-to-many between menus and dishes is a real relationship: a dish can appear on multiple menus and menus contain many dishes.
            // Use explicit join table for clarity and to allow indexing for performance.
            modelBuilder.Entity<Menu>()
                .HasMany(m => m.Dishes)
                .WithMany(d => d.Menus)
                .UsingEntity<Dictionary<string, object>>(
                    "MenuDishes",
                    j => j
                        .HasOne<Dish>()
                        .WithMany()
                        .HasForeignKey("DishId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<Menu>()
                        .WithMany()
                        .HasForeignKey("MenuId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.ToTable("MenuDishes");

                        // Composite primary key for the join table
                        j.HasKey("MenuId", "DishId");

                        // Indexes to optimize common query patterns:
                        // - Query all dishes for a menu: index on MenuId,DishId (covered by PK)
                        // - Query all menus containing a dish: index on DishId,MenuId
                        j.HasIndex(new[] { "DishId", "MenuId" }).HasDatabaseName("IX_MenuDishes_Dish_Menu");

                        // Optional: include RestaurantId as a shadow property to support tenant-scoped queries
                        // without joining back to Menu/Dish table (helps leaderboard-like queries across menu items per restaurant).
                        j.Property<int?>("RestaurantId").HasColumnName("RestaurantId");
                        j.HasIndex(new[] { "RestaurantId", "MenuId", "DishId" }).HasDatabaseName("IX_MenuDishes_Restaurant_Menu_Dish");

                        // When inserting via navigations, ensure the shadow RestaurantId is populated in application code if needed.
                    });

            // Category Relationships
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Restaurant)
                .WithMany(r => r.Categories)
                .HasForeignKey(c => c.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add unique index for category names per restaurant to enforce uniqueness and speed lookups
            modelBuilder.Entity<Category>()
                .HasIndex(c => new { c.RestaurantId, c.Name })
                .IsUnique()
                .HasDatabaseName("IX_Categories_Restaurant_UniqueName");

            // Dish Relationships
            modelBuilder.Entity<Dish>()
                .HasOne(d => d.Category)
                .WithMany(c => c.Dishes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Dish>()
                .HasOne(d => d.Restaurant)
                .WithMany(r => r.Dishes)
                .HasForeignKey(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add unique index for dish names per restaurant (prevents duplicate dish names within a restaurant)
            modelBuilder.Entity<Dish>()
                .HasIndex(d => new { d.RestaurantId, d.Name })
                .IsUnique()
                .HasDatabaseName("IX_Dishes_Restaurant_UniqueName");

            modelBuilder.Entity<Dish>()
                .HasIndex(d => d.CategoryId)
                .HasDatabaseName("IX_Dishes_CategoryId");

            // Order Relationships
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Restaurant)
                .WithMany(r => r.Orders)
                .HasForeignKey(o => o.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add relationship configuration for HandledBy staff member
            modelBuilder.Entity<Order>()
                .HasOne(o => o.HandledBy)
                .WithMany(s => s.HandledOrders)
                .HasForeignKey(o => o.HandledByStaffId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent staff deletion if they have handled orders

            // Add index for orders handled by staff
            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.RestaurantId, o.HandledByStaffId, o.OrderDate })
                .HasDatabaseName("IX_Orders_Restaurant_Staff_Date");

            // Add indexes for order queries
            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.RestaurantId, o.CreatedAt })
                .HasDatabaseName("IX_Orders_Restaurant_CreatedAt");

            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.CustomerId, o.CreatedAt })
                .HasDatabaseName("IX_Orders_Customer_CreatedAt");

            // Add index for Order status tracking
            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.RestaurantId, o.OrderStatusId, o.CreatedAt })
                .HasDatabaseName("IX_Orders_Restaurant_Status_Created");

            // NOTE: removed backward-compatible index that referenced navigation property 'Status' because
            // indexing a navigation property (entity type) is not supported by the database provider and
            // prevents the DbContext from being created at design-time. Use the scalar FK 'OrderStatusId' instead.

            // OrderItem Relationships
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // When an order is deleted, automatically delete all its order items

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Dish)
                .WithMany(d => d.OrderItems)
                .HasForeignKey(oi => oi.DishId)
                .OnDelete(DeleteBehavior.Restrict); // Prevents deletion of Dish if it's referenced in orders. Must handle order items first.

            // Add indexes for OrderItem queries
            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => oi.OrderId)
                .HasDatabaseName("IX_OrderItems_OrderId");

            // Composite index for dish popularity analysis
            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => new { oi.DishId, oi.CreatedAt })
                .HasDatabaseName("IX_OrderItems_Dish_CreatedAt");

            // Composite index for restaurant-specific order item queries
            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => new { oi.RestaurantId, oi.OrderId })
                .HasDatabaseName("IX_OrderItems_Restaurant_Order");

            // Composite index for dish orders in a time period per restaurant
            modelBuilder.Entity<OrderItem>()
                .HasIndex(oi => new { oi.RestaurantId, oi.DishId, oi.CreatedAt })
                .HasDatabaseName("IX_OrderItems_Restaurant_Dish_CreatedAt");

            // Review Relationships
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Restaurant)
                .WithMany(r => r.Reviews)
                .HasForeignKey(r => r.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Dish)
                .WithMany(d => d.Reviews)
                .HasForeignKey(r => r.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for review queries
            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.RestaurantId, r.CreatedAt })
                .HasDatabaseName("IX_Reviews_Restaurant_CreatedAt");

            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.DishId, r.CreatedAt })
                .HasDatabaseName("IX_Reviews_Dish_CreatedAt");

            // Table and Reservation Relationships
            modelBuilder.Entity<Table>()
                .ToTable("RestaurantTables");

            modelBuilder.Entity<Table>()
                .HasOne(t => t.Restaurant)
                .WithMany(r => r.Tables)
                .HasForeignKey(t => t.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Table)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.TableId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Reservations)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Update index for reservation queries to use ReservationTime instead of non-existent ReservationDate
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.RestaurantId, r.ReservationTime })
                .HasDatabaseName("IX_Reservations_Restaurant_Time");

            // Add additional index for reservation queries by table
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.TableId, r.ReservationTime })
                .HasDatabaseName("IX_Reservations_Table_Time");

            // Staff Schedule Relationships
            modelBuilder.Entity<StaffSchedule>()
                .HasOne(ss => ss.Restaurant)
                .WithMany(r => r.StaffSchedules)
                .HasForeignKey(ss => ss.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StaffSchedule>()
                .HasOne(ss => ss.StaffMember)
                .WithMany(sm => sm.Schedules)
                .HasForeignKey(ss => ss.StaffMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // The following staff audit relationships and indexes are commented out because
            // the streamlined model only allows admin users to manage schedules. Staff audit properties
            // (CreatedBy, CreatedByStaffId) are no longer present in StaffSchedule.
            /*
            modelBuilder.Entity<StaffSchedule>()
                .HasOne(ss => ss.CreatedBy)
                .WithMany()
                .HasForeignKey(ss => ss.CreatedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StaffSchedule>()
                .HasIndex(ss => ss.CreatedByStaffId)
                .HasDatabaseName("IX_StaffSchedules_CreatedByStaffId");
            */

            // Update index for staff schedule queries to use ShiftStart instead of non-existent ScheduleDate
            modelBuilder.Entity<StaffSchedule>()
                .HasIndex(ss => new { ss.RestaurantId, ss.StaffMemberId, ss.ShiftStart })
                .HasDatabaseName("IX_StaffSchedules_Restaurant_Staff_ShiftStart");

            // Add additional index for staff schedule time range queries
            modelBuilder.Entity<StaffSchedule>()
                .HasIndex(ss => new { ss.RestaurantId, ss.ShiftStart, ss.ShiftEnd })
                .HasDatabaseName("IX_StaffSchedules_Restaurant_ShiftRange");

            // Optional: Indexes to support queries filtering schedules by creator (admin only in streamlined model)
            modelBuilder.Entity<StaffSchedule>()
                .HasIndex(ss => ss.CreatedByAdminUserId)
                .HasDatabaseName("IX_StaffSchedules_CreatedByAdminUserId");

            // Order Status Relationships
            modelBuilder.Entity<OrderStatus>()
                .HasOne(os => os.Restaurant)
                .WithMany(r => r.OrderStatuses)
                .HasForeignKey(os => os.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Customer Loyalty Relationships
            modelBuilder.Entity<CustomerLoyalty>()
                .HasOne(cl => cl.Restaurant)
                .WithMany(r => r.CustomerLoyalties)
                .HasForeignKey(cl => cl.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CustomerLoyalty>()
                .HasOne(cl => cl.Customer)
                .WithMany(c => c.CustomerLoyalties)
                .HasForeignKey(cl => cl.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add unique constraint to ensure one loyalty record per customer per restaurant
            modelBuilder.Entity<CustomerLoyalty>()
                .HasIndex(cl => new { cl.RestaurantId, cl.CustomerId })
                .IsUnique()
                .HasDatabaseName("IX_CustomerLoyalties_Restaurant_Customer_Unique");

            // Add index for loyalty queries (existing analytics index retained)
            modelBuilder.Entity<CustomerLoyalty>()
                .HasIndex(cl => new { cl.RestaurantId, cl.CustomerId, cl.Points })
                .HasDatabaseName("IX_CustomerLoyalty_Restaurant_Customer_Points");

            modelBuilder.Entity<LoyaltyTransaction>()
                .HasOne(lt => lt.CustomerLoyalty)
                .WithMany(cl => cl.Transactions)
                .HasForeignKey(lt => lt.CustomerLoyaltyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add index for loyalty transaction queries (include restaurant for tenant-scoped queries)
            modelBuilder.Entity<LoyaltyTransaction>()
                .HasIndex(lt => new { lt.RestaurantId, lt.CustomerLoyaltyId, lt.CreatedAt })
                .HasDatabaseName("IX_LoyaltyTransactions_Restaurant_CustomerLoyalty_Date");

            // Keep backward-compatible index for queries that filter by CustomerLoyaltyId and date
            modelBuilder.Entity<LoyaltyTransaction>()
                .HasIndex(lt => new { lt.CustomerLoyaltyId, lt.CreatedAt })
                .HasDatabaseName("IX_LoyaltyTransactions_CustomerLoyalty_CreatedAt");

            // Promotion Relationships
            modelBuilder.Entity<Promotion>()
                .HasOne(p => p.Restaurant)
                .WithMany(r => r.Promotions)
                .HasForeignKey(p => p.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Sale Record Relationships
            modelBuilder.Entity<SaleRecord>()
                .HasOne(sr => sr.Restaurant)
                .WithMany(r => r.SaleRecords)
                .HasForeignKey(sr => sr.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SaleRecord>()
                .HasOne(sr => sr.Dish)
                .WithMany(d => d.SaleRecords)
                .HasForeignKey(sr => sr.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add index for sales analysis
            modelBuilder.Entity<SaleRecord>()
                .HasIndex(sr => new { sr.RestaurantId, sr.DishId, sr.SaleDate })
                .HasDatabaseName("IX_SaleRecords_Restaurant_Dish_Date");

            // Add index for AdminUser email lookups (common for authentication)
            modelBuilder.Entity<AdminUser>()
                .HasIndex(au => au.Email)
                .HasDatabaseName("IX_AdminUsers_Email");

            // Centralized indexes for AdminUser (previously declared via attributes in AdminUser.cs)
            // Composite unique index on Email + Username
            modelBuilder.Entity<AdminUser>()
                .HasIndex(au => new { au.Email, au.Username })
                .IsUnique()
                .HasDatabaseName("IX_AdminUsers_Email_Username_Unique");

            // Composite index on Role and IsActive for role-based queries
            modelBuilder.Entity<AdminUser>()
                .HasIndex(au => new { au.Role, au.IsActive })
                .HasDatabaseName("IX_AdminUsers_Role_Status");

            // Composite index for fast lookups by Username and active status
            modelBuilder.Entity<AdminUser>()
                .HasIndex(au => new { au.Username, au.IsActive })
                .HasDatabaseName("IX_Customers_Username_Active");

            // Composite index on PhoneNumber and LastLoginAt to support contact lookups and recent activity
            modelBuilder.Entity<AdminUser>()
                .HasIndex(au => new { au.PhoneNumber, au.LastLoginAt })
                .HasDatabaseName("IX_AdminUsers_Phone_LastLogin");

            // Add index for Customer email lookups (common for authentication and customer search)
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Email)
                .HasDatabaseName("IX_Customers_Email");

            // Add index for Customer registration date (for analytics and reporting)
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.DateRegistered)
                .HasDatabaseName("IX_Customers_DateRegistered");

            // Add index for StaffMember lookups
            modelBuilder.Entity<StaffMember>()
                .HasIndex(sm => sm.Email)
                .HasDatabaseName("IX_StaffMembers_Email");

            // Add composite index for active MenuTypes ordering
            modelBuilder.Entity<MenuType>()
                .HasIndex(mt => new { mt.RestaurantId, mt.IsActive, mt.DisplayOrder })
                .HasDatabaseName("IX_MenuTypes_Restaurant_Active_Order");

            // Ensure menu type names are unique per restaurant to prevent duplicate types (e.g., two "Breakfast" entries)
            modelBuilder.Entity<MenuType>()
                .HasIndex(mt => new { mt.RestaurantId, mt.Name })
                .IsUnique()
                .HasDatabaseName("IX_MenuTypes_Restaurant_UniqueName");

            // Add composite index for Menu availability queries
            modelBuilder.Entity<Menu>()
                .HasIndex(m => new { m.RestaurantId, m.IsActive, m.AvailableFrom, m.AvailableTo })
                .HasDatabaseName("IX_Menus_Restaurant_Availability_Active");

            // Add composite index for CustomerLoyalty tier queries
            modelBuilder.Entity<CustomerLoyalty>()
                .HasIndex(cl => new { cl.RestaurantId, cl.Tier, cl.LastActivity })
                .HasDatabaseName("IX_CustomerLoyalty_Restaurant_Tier_Activity");

            // Add index for Order status tracking
            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.RestaurantId, o.OrderStatusId, o.CreatedAt })
                .HasDatabaseName("IX_Orders_Restaurant_Status_Created");

            // Add composite index for Table availability
            modelBuilder.Entity<Table>()
                .HasIndex(t => new { t.RestaurantId, t.IsAvailable, t.Capacity })
                .HasDatabaseName("IX_Tables_Restaurant_Availability_Capacity");

            // Add composite index for active Promotions
            modelBuilder.Entity<Promotion>()
                .HasIndex(p => new { p.RestaurantId, p.IsActive, p.ValidFrom, p.ValidTo })
                .HasDatabaseName("IX_Promotions_Restaurant_Active_Dates");

            // Add composite index for Dish search by price range
            modelBuilder.Entity<Dish>()
                .HasIndex(d => new { d.RestaurantId, d.CategoryId, d.DishPrice })
                .HasDatabaseName("IX_Dishes_Restaurant_Category_Price");

            // Add composite index for Review ratings
            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.RestaurantId, r.Rating, r.CreatedAt })
                .HasDatabaseName("IX_Reviews_Restaurant_Rating_Date");

            // Add composite index for OrderStatus name search
            modelBuilder.Entity<OrderStatus>()
                .HasIndex(os => new { os.RestaurantId, os.Name })
                .HasDatabaseName("IX_OrderStatuses_Restaurant_Name");

            // Add composite index for Customer lookups by username and status
            modelBuilder.Entity<Customer>()
                .HasIndex(c => new { c.Username, c.IsActive })
                .HasDatabaseName("IX_Customers_Username_Status");

            // Add composite index for Customer search by name
            modelBuilder.Entity<Customer>()
                .HasIndex(c => new { c.Name, c.IsActive })
                .HasDatabaseName("IX_Customers_Name_Status");

            // Add composite index for Customer activity tracking
            modelBuilder.Entity<Customer>()
                .HasIndex(c => new { c.LastActivityDate, c.IsActive })
                .HasDatabaseName("IX_Customers_Activity_Status");

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Overrides <c>SaveChangesAsync</c> to enforce standardized audit fields and soft-delete semantics
        /// before EF Core persists changes to the database. This summary documents the precise algorithm,
        /// ordering, and important caveats so future maintainers understand side effects and extension points.
        /// 
        /// High-level behavior
        /// - Collects all tracked entries derived from <c>EntityBase</c> that are in states Added, Modified
        ///   or Deleted and processes them in-memory before calling <c>base.SaveChangesAsync</c>.
        /// - Ensures consistent UTC timestamps by capturing <c>DateTime.UtcNow</c> once at method start and
        ///   applying the same value to all affected entries.
        /// - Implements soft-delete by converting <c>EntityState.Deleted</c> entries into <c>EntityState.Modified</c>
        ///   and setting <c>IsDeleted = true</c> rather than issuing hard deletes.
        /// - Protects immutable audit fields (e.g. <c>CreatedAt</c>) from accidental modification.
        /// 
        /// Detailed per-state actions (performed for each tracked <c>EntityBase</c> entry):
        /// 1. Added
        ///    - If <c>CreatedAt</c> was not explicitly provided by the caller (default value), set it to the captured
        ///      UTC timestamp. Otherwise preserve the caller-provided value.
        ///    - Set <c>UpdatedAt</c> to the captured UTC timestamp.
        ///    - Ensure <c>IsDeleted</c> is set to <c>false</c> for new entities.
        ///    - Note: any required tenant-assignment (e.g. setting <c>RestaurantId</c> on tenant entities) is not
        ///      performed by this method; that behavior can be layered in here if the project requires automatic
        ///      tenant assignment.
        /// 
        /// 2. Modified
        ///    - Prevent updates to <c>CreatedAt</c> by clearing its <c>IsModified</c> flag; the property remains in the
        ///      database as originally created.
        ///    - Set <c>UpdatedAt</c> to the captured UTC timestamp.
        ///    - The method intentionally does not inspect or revert unrelated property changes — validation and
        ///      domain rules should be applied at the service or entity level before saving.
        /// 
        /// 3. Deleted (soft-delete)
        ///    - Convert the entry state from <c>Deleted</c> to <c>Modified</c> so EF issues an UPDATE instead of DELETE.
        ///    - Set <c>IsDeleted = true</c> and <c>UpdatedAt</c> to the captured timestamp.
        ///    - Protect <c>CreatedAt</c> from modification by clearing its <c>IsModified</c> flag.
        ///    - Caveat: soft-deleting an entity does not automatically soft-delete related dependent entities. Depending
        ///      on cascade rules and foreign key constraints you may need to handle dependents explicitly to avoid
        ///      constraint violations.
        /// 
        /// Implementation notes and rationale
        /// - The method batches the tracked entries into a list and caches property accessors (via <c>entry.Property</c>)
        ///   to avoid repeated string-based lookups which reduces overhead during SaveChanges on large change sets.
        /// - Capturing the timestamp once improves consistency across multiple entries in the same SaveChanges call
        ///   and reduces calls to <c>DateTime.UtcNow</c>.
        /// - The method rethrows a wrapped <c>DbUpdateConcurrencyException</c> to provide a clearer error message while
        ///   preserving the original exception as inner detail; logging can be added where appropriate.
        /// - This approach favors explicit, centralized audit handling inside the DbContext so callers don't need to
        ///   remember to populate audit fields manually.
        /// 
        /// Testing and extension guidance
        /// - Because the implementation uses <c>DateTime.UtcNow</c> directly, unit tests that assert exact timestamps
        ///   should allow for small deltas or refactor to inject a time provider if deterministic timestamps are
        ///   required for tests.
        /// - If multi-tenant automatic assignment (e.g. setting <c>RestaurantId</c> for new tenant entities) is needed,
        ///   add that behavior here before the Added-case finishes. Prefer an injected tenant provider (per-scope) or a
        ///   public property on the context to avoid static access.
        /// - For global soft-delete filtering, add model-level query filters in <c>OnModelCreating</c> so ordinary queries
        ///   automatically exclude <c>IsDeleted == true</c> rows.
        /// 
        /// Performance considerations
        /// - This method is optimized for clarity and reasonable performance: processing only relevant states and
        ///   minimizing reflection/lookup overhead. If SaveChanges becomes a hotspot, consider profiling and
        ///   offloading heavier domain logic to a service layer executed before SaveChanges is called.
        /// 
        /// Safety and caveats
        /// - Because audit and soft-delete logic run for every SaveChanges invocation, ensure callers are aware that
        ///   Deleted entries will not be removed from the database unless a hard-delete path is intentionally implemented.
        /// - Be mindful of migrations and administrative operations that need to bypass tenant filters or soft-delete
        ///   behavior; provide a clear escape (e.g. an admin flag or a separate maintenance context) if necessary.
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // Gather all tracked EntityBase entries in relevant states
            var entries = ChangeTracker
                .Entries<EntityBase>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .ToList();

            // Process each entry according to its state
            foreach (var entry in entries)
            {
                // Cache property references to avoid repeated string-based lookups
                var createdAtProp = entry.Property(nameof(EntityBase.CreatedAt));
                var updatedAtProp = entry.Property(nameof(EntityBase.UpdatedAt));
                var isDeletedProp = entry.Property(nameof(EntityBase.IsDeleted));

                // Apply state-specific logic
                switch (entry.State)
                {
                    // 1. Added: This logic sets initial audit info
                    case EntityState.Added:
                        // Only set CreatedAt when not provided by the caller
                        if (!(createdAtProp.CurrentValue is DateTime created) || created == default)
                        {
                            createdAtProp.CurrentValue = now;
                        }

                        updatedAtProp.CurrentValue = now;
                        isDeletedProp.CurrentValue = false;
                        break;
                    // 2. Modified: This logic protects immutable fields and updates audit info
                    case EntityState.Modified:
                        // Protect CreatedAt from accidental updates
                        if (createdAtProp.IsModified)
                            createdAtProp.IsModified = false;

                        updatedAtProp.CurrentValue = now;
                        break;
                    // 3. Deleted: This logic implements soft-delete
                    case EntityState.Deleted:
                        // Soft-delete: convert delete into update and mark IsDeleted
                        entry.State = EntityState.Modified;
                        isDeletedProp.CurrentValue = true;

                        // Protect CreatedAt from accidental updates
                        if (createdAtProp.IsModified)
                            createdAtProp.IsModified = false;

                        updatedAtProp.CurrentValue = now;
                        break;
                }
            }

            try
            {
                return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {   
                
                throw new DbUpdateConcurrencyException("Concurrency conflict detected while saving changes to the database.", ex);
            }
        }
    }
}
