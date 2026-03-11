using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Application.Contracts;
using SmartMenuOptim.Domain.Aggregates.CustomerLoyaltyAggregate;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Aggregates.MenuAggregate;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Features.Restaurants;
using SmartMenuOptim.Domain.Aggregates.TableAggregate;
using SmartMenuOptim.Domain.Common;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Entities.ProfileEntities;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;
using SmartMenuOptim.Domain.Enums;
using SmartMenuOptim.Domain.ValueObjects;
using SmartMenuOptim.Infrastructure.Persistence.Context.Converters;

namespace SmartMenuOptim.Infrastructure.Persistence.Context
{
    /// <summary>
    /// The application's main database context, extending IdentityDbContext to integrate ASP.NET Core Identity.
    /// </summary>
    /// <remarks>
    /// Design Rationale for IdentityDbContext Integration:
    /// 
    /// 1. Authentication Infrastructure:
    ///    - ApplicationUser inherits from IdentityUser to leverage identity framework
    ///    - IdentityDbContext automatically provides required tables and schemas
    ///    - Enables automatic handling of identity-related features
    /// 
    /// 2. Profile Entity Integration:
    ///    - One-to-one relationships maintained between ApplicationUser and profile entities
    ///    - Clear separation between authentication and business data
    ///    - Facilitates proper profile-based authorization
    /// 
    /// 3. Database Schema Management:
    ///    - Auto-creates identity tables (AspNetUsers, AspNetRoles, AspNetUserClaims, etc.)
    ///    - Handles identity-specific migrations automatically
    ///    - Maintains proper foreign key relationships across the schema
    /// 
    /// 4. Security Features:
    ///    - Built-in password hashing mechanisms
    ///    - Integrated token management for email/phone verification
    ///    - Support for claims-based authorization
    /// 
    /// 5. Multi-Tenant Architecture:
    ///    - TenantId in ApplicationUser enables proper user-tenant scoping
    ///    - Profile entities (Customer, StaffMember) support tenant-specific operations
    ///    - AdminUser remains tenant-independent for cross-tenant management
    /// 
    /// 6. Identity Framework Features:
    ///    - Complete user management system (registration, login, etc.)
    ///    - Role-based authorization capabilities
    ///    - Claims-based permissions system
    ///    - Built-in token generation and validation
    /// 
    /// For a complete overview of entity relationships and their design patterns,
    /// see the detailed documentation in docs/EntityRelationships.md
    /// 
    /// This design ensures a robust authentication and authorization system while maintaining
    /// clean separation of concerns between identity management and business logic.
    /// </remarks>
    public partial class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        // Add ApplicationUsers DbSet
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        // Global Entities
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<StaffMember> StaffMembers { get; set; }
        public DbSet<BusinessRule> BusinessRules { get; set; }

        // Tenant Specific Entities - Using Domain Aggregates for DDD
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<BusinessHours> BusinessHours { get; set; } // Child entity of Restaurant aggregate
        public DbSet<DishCategory> Categories { get; set; }
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<MenuDish> MenuDishes { get; set; }
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
        // Add DbSet for UserPermissions
        public DbSet<UserPermission> UserPermissions { get; set; }
        
        // Domain event dispatcher for publishing events after persistence
        private readonly IDomainEventDispatcher? _domainEventDispatcher;

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        
        /// <summary>
        /// Creates a new AppDbContext with domain event dispatching support.
        /// </summary>
        /// <param name="options">The database context options.</param>
        /// <param name="domainEventDispatcher">The domain event dispatcher for publishing events after persistence.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options, IDomainEventDispatcher domainEventDispatcher)
            : base(options)
        {
            _domainEventDispatcher = domainEventDispatcher;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ═══════════════════════════════════════════════════════════════════════
            // APPLY CONFIGURATIONS FROM ASSEMBLY
            // ═══════════════════════════════════════════════════════════════════════
            // Auto-discover and apply all IEntityTypeConfiguration<T> implementations
            // from the Configurations folder. This enables modular configuration files.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            /// ------- Configure Identity table names and keys
            /// 
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");

            modelBuilder.Entity<IdentityRole>().ToTable("Roles");

            modelBuilder.Entity<IdentityUserRole<string>>()
                .ToTable("UserRoles")
                .HasKey(r => new { r.UserId, r.RoleId });

            modelBuilder.Entity<IdentityUserClaim<string>>()
                .ToTable("UserClaims")
                .HasKey(uc => uc.Id);

            modelBuilder.Entity<IdentityUserLogin<string>>()
                .ToTable("UserLogins")
                .HasKey(l => new { l.LoginProvider, l.ProviderKey });

            modelBuilder.Entity<IdentityUserToken<string>>()
                .ToTable("UserTokens")
                .HasKey(t => new { t.UserId, t.LoginProvider, t.Name });

            modelBuilder.Entity<IdentityRoleClaim<string>>()
                .ToTable("RoleClaims")
                .HasKey(rc => rc.Id);

            modelBuilder.Entity<IdentityUserRole<string>>()
                .ToTable("UserRoles")
                .HasKey(r => new { r.UserId, r.RoleId });

            modelBuilder.Entity<IdentityUserClaim<string>>()
                .ToTable("UserClaims")
                .HasKey(uc => uc.Id);

            modelBuilder.Entity<IdentityUserLogin<string>>()
                .ToTable("UserLogins")
                .HasKey(l => new { l.LoginProvider, l.ProviderKey });

            modelBuilder.Entity<IdentityUserToken<string>>()
                .ToTable("UserTokens")
                .HasKey(t => new { t.UserId, t.LoginProvider, t.Name });

            modelBuilder.Entity<IdentityRoleClaim<string>>()
                .ToTable("RoleClaims")
                .HasKey(rc => rc.Id);

            /// --------- Configure AdminUser entity relationships and keys

            modelBuilder.Entity<AdminUser>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Ensure ID is database-generated
                entity.Property(e => e.Id)
                    .UseIdentityAlwaysColumn() // For PostgreSQL
                    .ValueGeneratedOnAdd();

                // Configure one-to-one relationship with ApplicationUser
                entity.HasOne(e => e.ApplicationUser)
                    .WithOne(u => u.AdminProfile)
                    .HasForeignKey<AdminUser>(e => e.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);

                // Fix: Index only ApplicationUserId for AdminUser
                entity.HasIndex(a => a.ApplicationUserId)  // Only ApplicationUserId should be unique
                    .IsUnique()
                    .HasDatabaseName("IX_AdminUsers_ApplicationUser");

                // Add index for AdminUser email lookups (common for authentication)
                entity.HasIndex(au => au.Email)
                    .HasDatabaseName("IX_AdminUsers_Email");

                // Composite unique index for AdminUser email and username
                entity.HasIndex(au => new { au.Email, au.UserName })
                    .IsUnique()
                    .HasDatabaseName("IX_AdminUsers_Email_Username_Unique");

            });


            /// ---------- Configure ApplicationUser relationships with consistent cascade behavior
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                // Configure one-to-one relationship with Admin profile
                entity.HasOne(au => au.AdminProfile)
                .WithOne(a => a.ApplicationUser)
                .HasForeignKey<AdminUser>(a => a.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

                // Configure one-to-one relationship with Customer profile
                entity.HasOne(au => au.CustomerProfile)
                    .WithOne(c => c.ApplicationUser)
                    .HasForeignKey<Customer>(c => c.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure one-to-one relationship with StaffMember profile
                entity.HasOne(au => au.StaffProfile)
                    .WithOne(s => s.ApplicationUser)
                    .HasForeignKey<StaffMember>(s => s.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);

                // Add composite indexes for profile queries
                entity.HasIndex(au => new { au.ProfileType, au.ProfileId })
                    .HasDatabaseName("IX_ApplicationUsers_Profile");

                entity.HasIndex(au => new { au.ProfileType, au.RestaurantTenantId })
                    .HasDatabaseName("IX_ApplicationUsers_Profile_Tenant");

            });


            /// ---------- Configure UserPermission relationships and indexes
            // Detailed configuration for UserPermission entity. This includes:
            modelBuilder.Entity<UserPermission>(entity =>
            {
                // Primary key is inherited from EntityBase (Id)

                // Configure many-to-one relationship with ApplicationUser
                // Required foreign key to ApplicationUser
                entity.HasOne(up => up.ApplicationUser)
                      .WithMany(au => au.PermissionsAssigment)
                      .HasForeignKey(up => up.ApplicationUserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Configure many-to-one relationship with Restaurant
                // Required foreign key to Restaurant (tenant)
                entity.HasOne(up => up.Restaurant)
                      .WithMany(r => r.UserPermissions)
                      .HasForeignKey(up => up.RestaurantId)
                      .OnDelete(DeleteBehavior.Cascade);
                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);

                // Indexes for permission queries

                // Primary composite index for permission lookup
                entity.HasIndex(up => new { up.RestaurantId, up.ApplicationUserId, up.Name })
                      .HasDatabaseName("IX_UserPermissions_Restaurant_User_Permission");

                // Index for filtering by restaurant and access level
                entity.HasIndex(up => new { up.RestaurantId, up.AccessLevel })
                      .HasDatabaseName("IX_UserPermissions_Restaurant_AccessLevel");

                // Index for expiring permissions
                entity.HasIndex(up => new { up.ExpiresAt, up.IsActive })
                      .HasDatabaseName("IX_UserPermissions_Expiration_Active")
                      .HasFilter("\"ExpiresAt\" IS NOT NULL"); // PostgreSQL syntax for filtered index

                // Index for audit queries by who granted the permission
                entity.HasIndex(up => new { up.GrantedBy, up.GrantedAt })
                      .HasDatabaseName("IX_UserPermissions_GrantedBy_Date");

                // Validate required fields
                entity.Property(up => up.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(up => up.ApplicationUserId)
                      .IsRequired()
                      .HasMaxLength(450); // Matches Identity key length

                // Optional properties constraints
                entity.Property(up => up.Description)
                      .HasMaxLength(500);

                entity.Property(up => up.Area)
                      .HasMaxLength(100);

                entity.Property(up => up.GrantedBy)
                      .HasMaxLength(450); // Matches Identity key length
            });


            /// ---------- Business Rule Relationships and Indexes 
            modelBuilder.Entity<BusinessRule>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Ensure ID is database-generated

                entity.Property(e => e.Id)
                    .UseIdentityAlwaysColumn() // For PostgreSQL
                    .ValueGeneratedOnAdd();

                // Configure many-to-one relationship with AdminUser
                entity.HasOne(br => br.AdminUser)
                .WithMany(au => au.BusinessRules)
                .HasForeignKey(br => br.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);

                // Primary composite index for business rules
                // - Optimizes historical queries by rule type, admin, and date
                // - Supports efficient filtering for audit trails and reporting
                // - Enables quick access to rule history for specific admins
                entity.HasIndex(br => new { br.RuleType, br.AdminUserId, br.CreatedAt })
                    .HasDatabaseName("IX_BusinessRules_RuleType_AdminUser_Date");


                // Index to optimize queries filtering by AdminUserId
                // - Improves performance for accessing all rules for a specific admin
                // - Supports efficient joins between BusinessRules and AdminUser
                // - Helps with audit queries across an admin's rule changes
                entity.HasIndex(br => br.AdminUserId)
                    .HasDatabaseName("IX_BusinessRules_AdminUserId");

                // Unique constraint for active rules
                // - Ensures only one active rule per type per admin
                // - Prevents duplicate active rules that could cause conflicts
                // - Critical for maintaining data consistency in business rule application
                entity.HasIndex(br => new { br.RuleType, br.AdminUserId, br.IsCurrentValue })
                    .IsUnique()
                    .HasFilter("\"IsCurrentValue\" = true")  // PostgreSQL syntax
                    .HasDatabaseName("UX_BusinessRules_ActiveRule");

                // Additional index for historical analysis
                // - Supports queries analyzing rule changes over time
                // - Helps track rule evolution and audit patterns
                entity.HasIndex(br => new { br.Version, br.CreatedAt })
                    .HasDatabaseName("IX_BusinessRules_Version_CreatedAt");
            });


            /// ----------- Restaurant configurations and indexes

            modelBuilder.Entity<Restaurant>(entity =>
            {
                // Configure value object properties with explicit converters
                // This prevents EF Core from treating them as separate entities
                entity.Property(r => r.Location)
                    .HasConversion(new AddressValueConverter())
                    .HasColumnName("Address")  // ← Maps to DB column
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(r => r.ContactEmail)
                    .HasConversion(new EmailValueConverter())
                    .HasColumnName("Email")  // ← Maps to DB column
                    .HasMaxLength(254)
                    .IsRequired();

                entity.Property(r => r.ContactPhone)
                    .HasConversion(new PhoneNumberValueConverter())
                    .HasColumnName("PhoneNumber")  // ← Maps to DB column
                    .HasMaxLength(20)
                    .IsRequired();

                // Configure operational properties that are part of the domain model
                //Why persist these in the DB? They affect restaurant behavior
                
                // IsAcceptingOrders - critical for knowing if orders can be placed
                // Use cases:
                // 1. Restaurant temporarily closes for orders (e.g., holidays, maintenance)
                // 2. Limits order intake during peak times
                // 3. Enables dynamic control over order flow
                // 4. UI filtering of restaurants accepting orders - Show only those open for business

                entity.Property(r => r.IsAcceptingOrders)
                    .IsRequired()
                    .HasDefaultValue(false);

                // MaxSimultaneousOrders - important for managing order volume and business configurations
                // Use cases:
                // 1. Prevents overwhelming kitchen staff during busy periods
                // 2. Allows restaurants to set capacity limits based on resources
                // 3. Enables dynamic adjustment of order limits
                // 4. Key for operational efficiency and customer satisfaction
                // 5. Kitchen workflow optimization
                // 6. Dinamic adjustment during special events or promotions - Scale up or down based on expected demand and staff.
                // 7. Business rule enforcement - Integrate with business rules to automatically adjust limits based on time of day, day of week, or special occasions.
                entity.Property(r => r.MaxSimultaneousOrders)
                    .IsRequired()
                    .HasDefaultValue(50);

                entity.HasOne(r => r.Owner)
                   .WithMany(a => a.OwnedRestaurants)
                   .HasForeignKey(r => r.OwnerId)
                   .OnDelete(DeleteBehavior.Restrict);

                // Add index on OwnerId for restaurant queries
                entity.HasIndex(r => r.OwnerId)
                    .HasDatabaseName("IX_Restaurants_OwnerId");

                // Add index for filtering restaurants that are accepting orders
                entity.HasIndex(r => new { r.IsAcceptingOrders, r.IsActive })
                    .HasDatabaseName("IX_Restaurants_AcceptingOrders_Active");

                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });

            /// ----------- BusinessHours configuration (child entity of Restaurant aggregate)
            modelBuilder.Entity<BusinessHours>(entity =>
            {
                // Configure primary key
                entity.HasKey(bh => bh.Id);

                // Ensure ID is database-generated
                entity.Property(bh => bh.Id)
                    .UseIdentityAlwaysColumn() // For PostgreSQL
                    .ValueGeneratedOnAdd();

                // Configure required foreign key to Restaurant (parent aggregate root)
                entity.Property(bh => bh.RestaurantId)
                    .IsRequired();

                // Configure required properties
                entity.Property(bh => bh.DayOfWeek)
                    .IsRequired()
                    .HasConversion<int>(); // Store as int in database

                entity.Property(bh => bh.OpenTime)
                    .IsRequired();

                entity.Property(bh => bh.CloseTime)
                    .IsRequired();

                // Configure relationship to Restaurant (parent aggregate)
                // Note: Navigation property should exist on Restaurant entity as OperatingHours collection
                // This is a one-to-many relationship where Restaurant is the parent

                // Add unique constraint to prevent duplicate hours for same day per restaurant
                entity.HasIndex(bh => new { bh.RestaurantId, bh.DayOfWeek })
                    .IsUnique()
                    .HasDatabaseName("IX_BusinessHours_Restaurant_Day_Unique");

                // Add index for querying business hours by restaurant
                entity.HasIndex(bh => bh.RestaurantId)
                    .HasDatabaseName("IX_BusinessHours_RestaurantId");
            });


            /// ---------------------- Menu Relationships configuration and Indexes

            modelBuilder.Entity<Menu>(entity =>
            {
                // Added missing cascade delete behavior
                entity.HasOne(m => m.Restaurant)
                    .WithMany(r => r.Menus)
                    .HasForeignKey(m => m.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Fixed menu type relationship
                entity.HasOne(m => m.MenuType)
                    .WithMany(mt => mt.Menus)
                    .HasForeignKey(m => m.MenuTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Add composite index for menu availability queries
                entity.HasIndex(m => new { m.RestaurantId, m.AvailableFrom, m.AvailableTo })
                    .HasDatabaseName("IX_Menus_Restaurant_Availability");

                // Add composite index for active menu availability queries
                entity.HasIndex(m => new { m.RestaurantId, m.IsActive, m.AvailableFrom, m.AvailableTo })
                    .HasDatabaseName("IX_Menus_Restaurant_Availability_Active");
            });


            // Configure MenuDish (join entity). Set many-to-many relationship with additional properties
            // between Menu and Dish through MenuDish
            modelBuilder.Entity<MenuDish>(entity =>
            {
                // Configure primary key
                entity.HasKey(md => new { md.MenuId, md.DishId });

                // Configure relationships to avoid duplicate navigation properties
                // and prevent shadow property creation
                entity.HasOne(md => md.Menu)
                    .WithMany(m => m.MenuDishes)
                    .HasForeignKey(md => md.MenuId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(md => md.Dish)
                    .WithMany(d => d.MenuDishes)
                    .HasForeignKey(md => md.DishId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Restaurant tenant relationship (from TenantEntityBase)
                entity.HasOne(md => md.Restaurant)
                    .WithMany()
                    .HasForeignKey(md => md.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure required properties and constraints
                entity.Property(md => md.DisplayOrder)
                    .IsRequired();

                entity.Property(md => md.SpecialPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(md => md.Notes)
                    .HasMaxLength(500);

                // Configure indexes for performance optimization
                entity.HasIndex(new[] { "DishId", "MenuId" })
                    .HasDatabaseName("IX_MenuDishes_Dish_Menu");

                entity.HasIndex(new[] { "RestaurantId", "MenuId", "DishId" })
                    .HasDatabaseName("IX_MenuDishes_Restaurant_Menu_Dish");

                // Configure global query filter for soft-delete to maintain data consistency
                entity.HasQueryFilter(md => !md.IsDeleted);
            });


            /// ----------Category Relationships configuration and Indexes
            modelBuilder.Entity<DishCategory>(entity =>
            {
                entity.HasOne(c => c.Restaurant)
               .WithMany(r => r.Categories)
               .HasForeignKey(c => c.RestaurantId)
               .OnDelete(DeleteBehavior.Cascade);

                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);

                // Add unique index for category names per restaurant to enforce uniqueness and speed lookups
                entity.HasIndex(c => new { c.RestaurantId, c.Name })
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false") // PostgreSQL syntax for filtered index
                    .HasDatabaseName("IX_Categories_Restaurant_UniqueName");

            });


            /// ---------- StaffMember Relationships configuration and Indexes

            modelBuilder.Entity<StaffMember>(entity =>
            {
                // Primary key configuration
                entity.HasKey(e => e.Id);

                // Configure one-to-one relationship with ApplicationUser
                entity.HasOne(e => e.ApplicationUser)
                    .WithOne(au => au.StaffProfile)
                    .HasForeignKey<StaffMember>(e => e.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure many-to-one relationship with Restaurant (tenant)
                entity.HasOne(e => e.Restaurant)
                    .WithMany()  // Restaurant side is already configured
                    .HasForeignKey(e => e.RestaurantId)
                    .OnDelete(DeleteBehavior.Restrict);  // Prevent deletion of restaurant with active staff

                // Configure one-to-many relationship with Order (HandledOrders)
                entity.HasMany(e => e.HandledOrders)
                    .WithOne(o => o.HandledBy)
                    .HasForeignKey(o => o.HandledByStaffId)
                    .OnDelete(DeleteBehavior.Restrict);  // Prevent deletion of staff with handled orders

                // Configure one-to-many relationship with StaffSchedule
                entity.HasMany(e => e.Schedules)
                    .WithOne(ss => ss.StaffMember)
                    .HasForeignKey(ss => ss.StaffMemberId)
                    .OnDelete(DeleteBehavior.Cascade);  // Delete schedules when staff is deleted
                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
                // Indexes for common query patterns

                // Primary composite index for authentication and tenant scoping
                entity.HasIndex(e => new { e.Email, e.UserName })
                    .IsUnique()
                    .HasDatabaseName("IX_StaffMembers_Email_Username_Unique");

                // Index for staff management and scheduling
                entity.HasIndex(e => new { e.RestaurantId, e.Role, e.EmploymentStatus, e.IsActive })
                    .HasDatabaseName("IX_StaffMembers_Restaurant_Role_Status");

                // Index for contact information and verification
                entity.HasIndex(e => new { e.PhoneNumber, e.PhoneNumberConfirmed })
                    .HasDatabaseName("IX_StaffMembers_Phone_Verified");

                // Field validations
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(20);

                entity.Property(e => e.EmergencyContactName)
                    .HasMaxLength(100);

                entity.Property(e => e.EmergencyContactPhone)
                    .HasMaxLength(20);

                entity.Property(e => e.Notes)
                    .HasMaxLength(1000);
            });


            /// ---------- Dish Relationships configuration and Indexes

            modelBuilder.Entity<Dish>(entity =>
            {
                entity.HasOne(d => d.Category)
                 .WithMany(c => c.Dishes)
                 .HasForeignKey(d => d.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Restaurant)
                    .WithMany(r => r.Dishes)
                    .HasForeignKey(d => d.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Add unique index for dish names per restaurant (prevents duplicate dish names within a restaurant)
                entity.HasIndex(d => new { d.RestaurantId, d.Name })
                    .IsUnique()
                    .HasDatabaseName("IX_Dishes_Restaurant_UniqueName");

                entity.HasIndex(d => d.CategoryId)
                    .HasDatabaseName("IX_Dishes_CategoryId");

                // Add composite index for Dish search by price range
                entity.HasIndex(d => new { d.RestaurantId, d.CategoryId, d.DishPrice })
                    .HasDatabaseName("IX_Dishes_Restaurant_Category_Price");
                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });


            /// ---------- Order Relationships

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasOne(o => o.Restaurant)
                    .WithMany(r => r.Orders)
                    .HasForeignKey(o => o.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(o => o.Customer)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Add relationship configuration for HandledBy staff member
                entity.HasOne(o => o.HandledBy)
                    .WithMany(s => s.HandledOrders)
                    .HasForeignKey(o => o.HandledByStaffId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent staff deletion if they have handled orders

                // Add index for orders handled by staff
                entity.HasIndex(o => new { o.RestaurantId, o.HandledByStaffId, o.OrderDate })
                    .HasDatabaseName("IX_Orders_Restaurant_Staff_Date");

                // Add indexes for order queries
                entity.HasIndex(o => new { o.RestaurantId, o.CreatedAt })
                    .HasDatabaseName("IX_Orders_Restaurant_CreatedAt");

                entity.HasIndex(o => new { o.CustomerId, o.CreatedAt })
                    .HasDatabaseName("IX_Orders_Customer_CreatedAt");

                // Add index for Order status tracking
                entity.HasIndex(o => new { o.RestaurantId, o.OrderStatusId, o.CreatedAt })
                    .HasDatabaseName("IX_Orders_Restaurant_Status_Created");
                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });


            // NOTE: removed backward-compatible index that referenced navigation property 'Status' because
            // indexing a navigation property (entity type) is not supported by the database provider and
            // prevents the DbContext from being created at design-time. Use the scalar FK 'OrderStatusId' instead.

            /// ---------- OrderItem Relationships

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasOne(oi => oi.Order)
               .WithMany(o => o.OrderItems)
               .HasForeignKey(oi => oi.OrderId)
               .OnDelete(DeleteBehavior.Cascade); // When an order is deleted, automatically delete all its order items           

                entity.HasOne(oi => oi.Dish)
                    .WithMany(d => d.OrderItems)
                    .HasForeignKey(oi => oi.DishId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevents deletion of Dish if it's referenced in orders. Must handle order items first.

                // Add indexes for OrderItem queries
                entity.HasIndex(oi => oi.OrderId)
                    .HasDatabaseName("IX_OrderItems_OrderId");

                // Composite index for dish popularity analysis
                entity.HasIndex(oi => new { oi.DishId, oi.CreatedAt })
                    .HasDatabaseName("IX_OrderItems_Dish_CreatedAt");

                // Composite index for restaurant-specific order item queries
                entity.HasIndex(oi => new { oi.RestaurantId, oi.OrderId })
                    .HasDatabaseName("IX_OrderItems_Restaurant_Order");

                // Composite index for dish orders in a time period per restaurant
                entity.HasIndex(oi => new { oi.RestaurantId, oi.DishId, oi.CreatedAt })
                    .HasDatabaseName("IX_OrderItems_Restaurant_Dish_CreatedAt");
                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });


            /// ----------- Review Relationships and Indexes

            modelBuilder.Entity<Review>(entity =>
            {
                entity.Property(r => r.SentimentScore)
                    .HasConversion(GenericValueConverter<double, double>.SentimentScore)
                    .HasColumnType("decimal(3,1)");

                entity.HasOne(r => r.Restaurant)
                    .WithMany(r => r.Reviews)
                    .HasForeignKey(r => r.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Customer)
                    .WithMany(c => c.Reviews)
                    .HasForeignKey(r => r.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(r => r.Dish)
                    .WithMany(d => d.Reviews)
                    .HasForeignKey(r => r.DishId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Add indexes for review queries
                entity.HasIndex(r => new { r.RestaurantId, r.CreatedAt })
                    .HasDatabaseName("IX_Reviews_Restaurant_CreatedAt");

                entity.HasIndex(r => new { r.DishId, r.CreatedAt })
                    .HasDatabaseName("IX_Reviews_Dish_CreatedAt");

                // Add composite index for Review ratings
                entity.HasIndex(r => new { r.RestaurantId, r.Rating, r.CreatedAt })
                    .HasDatabaseName("IX_Reviews_Restaurant_Rating_Date");
                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });

            /// ------------ Table Configurations and Indexes

            modelBuilder.Entity<Table>(entity =>
            {

                entity.ToTable("RestaurantTables")
                    .HasOne(t => t.Restaurant)
                    .WithMany(r => r.Tables)
                    .HasForeignKey(t => t.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasAlternateKey(t => new { t.RestaurantId, t.TableNumber })
                    .HasName("AK_Tables_Restaurant_TableNumber");

                // Add composite index for Table status and capacity queries
                // Domain Table uses Status enum property, not IsAvailable boolean
                entity.HasIndex(t => new { t.RestaurantId, t.Status, t.Capacity })
                    .HasDatabaseName("IX_Tables_Restaurant_Status_Capacity");
                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });


            /// ----------- Reservation Configurations and Indexes

            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.HasOne(r => r.Table)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.TableId)
                .OnDelete(DeleteBehavior.Cascade);


                entity.HasOne(r => r.Customer)
                    .WithMany(c => c.Reservations)
                    .HasForeignKey(r => r.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure ReservationStatus enum to be stored as integer
                entity.Property(r => r.Status)
                    .HasConversion<int>()
                    .IsRequired()
                    .HasDefaultValue(ReservationStatus.Pending)
                    .HasComment("0=Pending, 1=Confirmed, 2=Seated, 3=Completed, 4=Cancelled, 5=NoShow");

                // Update index for reservation queries to use ReservationTime instead of non-existent ReservationDate
                entity.HasIndex(r => new { r.RestaurantId, r.ReservationTime })
                    .HasDatabaseName("IX_Reservations_Restaurant_Time");

                // Add additional index for reservation queries by table
                entity.HasIndex(r => new { r.TableId, r.ReservationTime })
                    .HasDatabaseName("IX_Reservations_Table_Time");

                // Add index for filtering reservations by status (e.g., finding active reservations)
                entity.HasIndex(r => new { r.RestaurantId, r.Status, r.ReservationTime })
                    .HasDatabaseName("IX_Reservations_Restaurant_Status_Time");

                // Add index for table availability checks (filtering out cancelled/completed reservations)
                entity.HasIndex(r => new { r.TableId, r.Status, r.ReservationTime })
                    .HasDatabaseName("IX_Reservations_Table_Status_Time");

                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });


            /// ---------- Staff Schedule Relationships and Indexes

            modelBuilder.Entity<StaffSchedule>(entity =>
            {
                entity.HasOne(ss => ss.Restaurant)
                .WithMany(r => r.StaffSchedules)
                .HasForeignKey(ss => ss.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

                // Fix: Configure StaffMember relationship correctly
                entity.HasOne(ss => ss.StaffMember)
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
                entity.HasIndex(ss => new { ss.RestaurantId, ss.StaffMemberId, ss.ShiftStart })
                    .HasDatabaseName("IX_StaffSchedules_Restaurant_Staff_ShiftStart");

                // Add additional index for staff schedule time range queries
                entity.HasIndex(ss => new { ss.RestaurantId, ss.ShiftStart, ss.ShiftEnd })
                    .HasDatabaseName("IX_StaffSchedules_Restaurant_ShiftRange");

                // Optional: Indexes to support queries filtering schedules by creator (admin only in streamlined model)
                entity.HasIndex(ss => ss.CreatedByAdminUserId)
                    .HasDatabaseName("IX_StaffSchedules_CreatedByAdminUserId");
                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });


            /// ------------------ Order Status Relationships and Indexes

            modelBuilder.Entity<OrderStatus>(entity =>
            {
                entity.HasOne(os => os.Restaurant)
               .WithMany(r => r.OrderStatuses)
               .HasForeignKey(os => os.RestaurantId)
               // Changed to Cascade to allow automatic deletion of orphaned statuses on restaurant delete
               .OnDelete(DeleteBehavior.Cascade);

                // Add composite index for OrderStatus name search
                entity.HasIndex(os => new { os.RestaurantId, os.Name })
                    .HasDatabaseName("IX_OrderStatuses_Restaurant_Name");
                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });

            /// ------------------- Customer Loyalty Relationships

            modelBuilder.Entity<CustomerLoyalty>(entity =>
            {
                entity.HasOne(cl => cl.Restaurant)
               .WithMany(r => r.CustomerLoyalties)
               .HasForeignKey(cl => cl.RestaurantId)
               .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cl => cl.Customer)
                    .WithMany(c => c.CustomerLoyalties)
                    .HasForeignKey(cl => cl.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Add unique constraint to ensure one loyalty record per customer per restaurant
                entity.HasIndex(cl => new { cl.RestaurantId, cl.CustomerId })
                    .IsUnique()
                    .HasDatabaseName("IX_CustomerLoyalties_Restaurant_Customer_Unique");

                // Add index for loyalty queries (existing analytics index retained)
                entity.HasIndex(cl => new { cl.RestaurantId, cl.CustomerId, cl.Points })
                    .HasDatabaseName("IX_CustomerLoyalty_Restaurant_Customer_Points");

                // Add composite index for CustomerLoyalty tier queries
                entity.HasIndex(cl => new { cl.RestaurantId, cl.Tier, cl.LastActivity })
                    .HasDatabaseName("IX_CustomerLoyalty_Restaurant_Tier_Activity");

                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });


            /// --------- Loyalty Transaction Relationships and Indexes

            modelBuilder.Entity<LoyaltyTransaction>(entity =>
            {
                entity.HasOne(lt => lt.CustomerLoyalty)
                   .WithMany(cl => cl.Transactions)
                   .HasForeignKey(lt => lt.CustomerLoyaltyId)
                   .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(lt => new { lt.RestaurantId, lt.CustomerLoyaltyId, lt.CreatedAt })
                    .HasDatabaseName("IX_LoyaltyTransactions_Restaurant_CustomerLoyalty_Date");

                // Keep backward-compatible index for queries that filter by CustomerLoyaltyId and date
                entity.HasIndex(lt => new { lt.CustomerLoyaltyId, lt.CreatedAt })
                    .HasDatabaseName("IX_LoyaltyTransactions_CustomerLoyalty_CreatedAt");
                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });


            /// ---------- Promotion Relationships and Indexes

            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.HasOne(p => p.Restaurant)
               .WithMany(r => r.Promotions)
               .HasForeignKey(p => p.RestaurantId)
               .OnDelete(DeleteBehavior.Cascade);

                // Add composite index for active Promotions
                // Note: IsActive() is a computed method, so we index the underlying properties (ValidFrom, ValidTo)
                // Queries can filter active promotions using date range comparisons
                entity.HasIndex(p => new { p.RestaurantId, p.ValidFrom, p.ValidTo })
                    .HasDatabaseName("IX_Promotions_Restaurant_Active_Dates");
                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });


            /// ------------ SaleRecord Relationship Configuration and Indexes

            modelBuilder.Entity<SaleRecord>(entity =>
            {
                // Configure Money value object property with explicit converter
                entity.Property(sr => sr.SaleAmount)
                    .HasConversion(new MoneyValueConverter())
                    .IsRequired();

                entity.HasOne(sr => sr.Restaurant)
                .WithMany(r => r.SaleRecords)
                .HasForeignKey(sr => sr.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(sr => sr.Dish)
                    .WithMany(d => d.SaleRecords)
                    .HasForeignKey(sr => sr.DishId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Add index for sales analysis
                entity.HasIndex(sr => new { sr.RestaurantId, sr.DishId, sr.SaleDate })
                    .HasDatabaseName("IX_SaleRecords_Restaurant_Dish_Date");
                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });


            /// ------------ Customer Relationships configuration and Indexes
            modelBuilder.Entity<Customer>(entity =>
            {
                // Primary key configuration
                entity.HasKey(e => e.Id);

                // Configure one-to-one relationship with ApplicationUser
                entity.HasOne(e => e.ApplicationUser)
                    .WithOne(au => au.CustomerProfile)
                    .HasForeignKey<Customer>(e => e.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure one-to-many relationship with Order
                entity.HasMany(e => e.Orders)
                    .WithOne(o => o.Customer)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);  // Prevent customer deletion if they have orders

                // Configure one-to-many relationship with Review
                entity.HasMany(e => e.Reviews)
                    .WithOne(r => r.Customer)
                    .HasForeignKey(r => r.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);  // Allow anonymous reviews

                // Configure one-to-many relationship with Reservation
                entity.HasMany(e => e.Reservations)
                    .WithOne(r => r.Customer)
                    .HasForeignKey(r => r.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);  // Prevent customer deletion with active reservations

                // Configure one-to-many relationship with CustomerLoyalty
                entity.HasMany(e => e.CustomerLoyalties)
                    .WithOne(cl => cl.Customer)
                    .HasForeignKey(cl => cl.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);  // Delete loyalty records when customer is deleted

                // Indexes for common query patterns

                // Primary index for authentication
                entity.HasIndex(e => e.ApplicationUserId)
                    .IsUnique()
                    .HasDatabaseName("IX_Customers_ApplicationUser");

                // Index for customer lookups by username and status
                entity.HasIndex(e => new { e.UserName, e.IsActive })
                    .HasDatabaseName("IX_Customers_Username_Status");

                // Index for customer search by name
                entity.HasIndex(e => new { e.Name, e.IsActive })
                    .HasDatabaseName("IX_Customers_Name_Status");

                // Index for customer activity tracking
                entity.HasIndex(e => new { e.LastActivityDate, e.IsActive })
                    .HasDatabaseName("IX_Customers_Activity_Status");

                // Field validations
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(20);

                entity.Property(e => e.PreferredLanguage)
                    .HasMaxLength(2);

                entity.Property(e => e.TimeZoneId)
                    .HasMaxLength(50);

                entity.Property(e => e.Notes)
                    .HasMaxLength(1000);

                // Multi-tenant boundaries
                entity.HasQueryFilter(e => !e.IsDeleted);  // Global query filter for soft delete

                // Multi-tenant query filter to ensure customers are scoped to their restaurant tenant

                // entity.HasQueryFilter(e => e.RestaurantTenantId == _currentRestaurantTenantId);
            });


            /// ---------- MenuType Relationships configuration and Indexes

            modelBuilder.Entity<MenuType>(entity =>
            {
                // Primary key configuration
                entity.HasKey(e => e.Id);

                // Configure many-to-one relationship with Restaurant
                entity.HasOne(mt => mt.Restaurant)
                    .WithMany(r => r.MenuTypes)
                    .HasForeignKey(mt => mt.RestaurantId)
                    .OnDelete(DeleteBehavior.Cascade);  // Delete menu types when restaurant is deleted

                // Configure one-to-many relationship with Menu
                entity.HasMany(mt => mt.Menus)
                    .WithOne(m => m.MenuType)
                    .HasForeignKey(m => m.MenuTypeId)
                    .OnDelete(DeleteBehavior.Restrict);  // Prevent deletion of menu type if it has menus

                // Field validations
                entity.Property(mt => mt.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(mt => mt.Description)
                    .HasMaxLength(500);

                // Add composite index for menu type availability and ordering
                entity.HasIndex(mt => new { mt.RestaurantId, mt.IsActive, mt.DisplayOrder })
                    .HasDatabaseName("IX_MenuTypes_Restaurant_Active_Order");

                // Ensure menu type names are unique per restaurant
                entity.HasIndex(mt => new { mt.RestaurantId, mt.Name })
                    .IsUnique()
                    .HasDatabaseName("IX_MenuTypes_Restaurant_UniqueName");

                // Add time-based querying support
                entity.HasIndex(mt => new { mt.RestaurantId, mt.DefaultStartTime, mt.DefaultEndTime })
                    .HasDatabaseName("IX_MenuTypes_Restaurant_TimeRange");

                // Multi-tenant boundaries
                entity.HasQueryFilter(mt => !mt.IsDeleted);  // Global query filter for soft delete
            });

            // Configure value conversions for value objects
            ConfigureEmailValueConversion(modelBuilder);
            ConfigureAddressValueConversion(modelBuilder);
            ConfigurePhoneNumberValueConversion(modelBuilder);
            ConfigureMoneyValueConversion(modelBuilder);
            ConfigurePercentageValueConversion(modelBuilder);
            ConfigureDishNameValueConversion(modelBuilder);
            ConfigureRatingValueConversion(modelBuilder);
        }

        /// <summary>
        /// Configures value conversion for Email value objects across all entities.
        /// This allows Email to be stored as a string in the database while maintaining
        /// type safety and domain validation in the application.
        /// </summary>
        /// <param name="modelBuilder">The model builder used to configure the context.</param>
        /// <remarks>
        /// Uses the <see cref="EmailValueConverter"/> to handle bidirectional conversion
        /// between Email value objects and their string database representation.
        /// </remarks>
        private void ConfigureEmailValueConversion(ModelBuilder modelBuilder)
        {
            // Create a single converter instance to be reused across all Email properties
            var converter = new EmailValueConverter();

            // Find all Email properties across all entity types
            var emailProperties = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(Email));

            // Configure each Email property with the converter and constraints
            foreach (var property in emailProperties)
            {
                property.SetValueConverter(converter);
                property.SetMaxLength(254); // Match Email validation constraint
            }
        }

        /// <summary>
        /// Configures value conversion for properties of type Address in the model, enabling proper storage and
        /// retrieval of Address objects in the database.   
        /// </summary>
        /// <remarks>This method applies a custom value converter to all properties of type Address across
        /// all entities in the model. The converter ensures that Address objects are correctly serialized and
        /// deserialized when interacting with the database. A maximum length of 1000 is set for these properties to
        /// accommodate the serialized Address data. Adjust the maximum length as needed based on the complexity and
        /// expected size of Address instances.</remarks>
        /// <param name="modelBuilder">The model builder used to define entity mappings and configuration for the database context.</param>
        private void ConfigureAddressValueConversion(ModelBuilder modelBuilder)
        {
            var converter = new AddressValueConverter();

            var addressProperties = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(Address));

            foreach (var property in addressProperties)
            {
                property.SetValueConverter(converter);
                property.SetMaxLength(1000); // Adjust based on Address complexity
            }
        }

        /// <summary>
        /// Configures value conversion for PhoneNumber value objects across all entities.
        /// This allows PhoneNumber to be stored as a string in the database while maintaining
        /// type safety and domain validation in the application.
        /// </summary>
        /// <param name="modelBuilder">The model builder used to configure the context.</param>
        /// <remarks>
        /// Uses the <see cref="PhoneNumberValueConverter"/> to handle bidirectional conversion
        /// between PhoneNumber value objects and their string database representation.
        /// The original formatted phone number is preserved in the database.
        /// </remarks>
        private void ConfigurePhoneNumberValueConversion(ModelBuilder modelBuilder)
        {
            // Create a single converter instance to be reused across all PhoneNumber properties
            var converter = new PhoneNumberValueConverter();

            // Find all PhoneNumber properties across all entity types
            var phoneProperties = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(PhoneNumber));

            // Configure each PhoneNumber property with the converter and constraints
            foreach (var property in phoneProperties)
            {
                property.SetValueConverter(converter);
                property.SetMaxLength(20); // Standard max length for international phone numbers
            }
        }

        /// <summary>
        /// Configures value conversion for Money value objects across all entities.
        /// This allows Money to be stored as a JSON string in the database while maintaining
        /// type safety and domain validation in the application.
        /// </summary>
        /// <param name="modelBuilder">The model builder used to configure the context.</param>
        /// <remarks>
        /// Uses the <see cref="MoneyValueConverter"/> to handle bidirectional conversion
        /// between Money value objects and their JSON database representation.
        /// Stores both amount and currency code as: {"Amount":99.99,"Currency":"USD"}
        /// </remarks>
        private void ConfigureMoneyValueConversion(ModelBuilder modelBuilder)
        {
            // Create a single converter instance to be reused across all Money properties
            var converter = new MoneyValueConverter();

            // Find all Money properties across all entity types
            var moneyProperties = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(Money));

            // Configure each Money property with the converter and constraints
            foreach (var property in moneyProperties)
            {
                property.SetValueConverter(converter);
                property.SetMaxLength(100); // Sufficient for {"Amount":999999999.99,"Currency":"XXX"}
            }
        }

        /// <summary>
        /// Configures value conversion for Percentage value objects across all entities.
        /// This allows Percentage to be stored as a decimal in the database while maintaining
        /// type safety and domain validation in the application.
        /// </summary>
        /// <param name="modelBuilder">The model builder used to configure the context.</param>
        /// <remarks>
        /// Uses the <see cref="PercentageValueConverter"/> to handle bidirectional conversion
        /// between Percentage value objects and their decimal database representation.
        /// Stored as decimal (0.0 to 1.0): 0.15 represents 15%
        /// </remarks>
        private void ConfigurePercentageValueConversion(ModelBuilder modelBuilder)
        {
            // Create a single converter instance to be reused across all Percentage properties
            var converter = new PercentageValueConverter();

            // Find all Percentage properties across all entity types
            var percentageProperties = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(Percentage));

            // Configure each Percentage property with the converter
            foreach (var property in percentageProperties)
            {
                property.SetValueConverter(converter);
                // Decimal precision is handled by the database provider (typically DECIMAL(18,4))
            }
        }

        /// <summary>
        /// Configures value conversion for DishName value objects across all entities.
        /// This allows DishName to be stored as a string in the database while maintaining
        /// type safety and domain validation in the application.
        /// </summary>
        /// <param name="modelBuilder">The model builder used to configure the context.</param>
        /// <remarks>
        /// Uses the <see cref="DishNameValueConverter"/> to handle bidirectional conversion
        /// between DishName value objects and their string database representation.
        /// </remarks>
        private void ConfigureDishNameValueConversion(ModelBuilder modelBuilder)
        {
            // Create a single converter instance to be reused across all DishName properties
            var converter = new DishNameValueConverter();

            // Find all DishName properties across all entity types
            var dishNameProperties = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(DishName));

            // Configure each DishName property with the converter and constraints
            foreach (var property in dishNameProperties)
            {
                property.SetValueConverter(converter);
                property.SetMaxLength(100); // Match DishName validation constraint
            }
        }

        /// <summary>
        /// Configures value conversion for Rating value objects across all entities.
        /// This allows Rating to be stored as an integer in the database while maintaining
        /// type safety and domain validation in the application.
        /// </summary>
        /// <param name="modelBuilder">The model builder used to configure the context.</param>
        /// <remarks>
        /// Uses the <see cref="RatingValueConverter"/> to handle bidirectional conversion
        /// between Rating value objects and their integer database representation.
        /// </remarks>
        private void ConfigureRatingValueConversion(ModelBuilder modelBuilder)
        {
            // Create a single converter instance to be reused across all Rating properties
            var converter = new RatingValueConverter();

            // Find all Rating properties across all entity types
            var ratingProperties = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(Rating));

            // Configure each Rating property with the converter
            foreach (var property in ratingProperties)
            {
                property.SetValueConverter(converter);
                // No additional length constraint needed for integers
            }
        }


        /// <summary>
        /// Configures model-wide conventions to ensure data consistency, such as automatically converting all DateTime values to UTC.
        /// </summary>
        /// <remarks>
        /// This method applies a global convention to all properties of type <see cref="DateTime"/> and nullable <see cref="DateTime?"/>.
        /// It uses the <see cref="UtcDateTimeValueConverter"/> to achieve the following:
        /// 
        /// 1.  **On Save to Database**: Any `DateTime` value, regardless of its `Kind` (`Local` or `Unspecified`), is converted to its UTC equivalent
        ///     before being stored. This prevents time zone ambiguity in the database.
        /// 
        /// 2.  **On Read from Database**: When a `DateTime` is read from the database, its `Kind` is explicitly set to `DateTimeKind.Utc`.
        ///     This ensures that the application code always treats the value as a UTC timestamp, preventing incorrect local time conversions.
        /// 
        /// This centralized approach is a best practice for handling time zones, as it eliminates the need for manual `.ToUniversalTime()` calls
        /// throughout the application and prevents common time zone-related bugs.
        /// </remarks>
        /// <param name="configurationBuilder">A builder used to configure conventions for the model.</param>
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            /// Use UtcDateTimeValueConverter for all DateTime and DateTime? properties
            configurationBuilder.Properties<DateTime>()
                .HaveConversion<UtcDateTimeValueConverter>();

            configurationBuilder.Properties<DateTime?>()
                .HaveConversion<UtcDateTimeValueConverter>();
        }

        /// SaveChanges() Override: I added a synchronous SaveChanges() override. 
        /// It also calls SetAuditProperties() before calling the base implementation, ensuring that both synchronous and asynchronous saves apply the exact same auditing and business logic.

        /// <summary>
        /// Saves all changes made in this context to the underlying database.
        /// </summary>
        /// <remarks>This method commits all tracked changes to the database in a single transaction. If
        /// an error occurs, no changes are saved. Override this method to add custom logic before or after saving
        /// changes.</remarks>
        /// <returns>The number of state entries written to the database.</returns>
        public override int SaveChanges()
        {
            SetAuditProperties();
            return base.SaveChanges();
        }

        /// SaveChangesAsync() Refactored: The SaveChangesAsync method is now much simpler. It calls SetAuditProperties() and then proceeds with saving the changes. 
        /// I removed the transaction block from this method as it's generally better to let the caller manage transaction scope if needed, or let EF Core manage it implicitly.

        /// <summary>
        /// Overrides SaveChangesAsync to automatically manage audit fields and soft-delete behavior
        /// for all entities deriving from EntityBase, including ApplicationUser.
        ///
        /// Audit Fields Managed:
        /// - CreatedAt: Set when entity is added
        /// - UpdatedAt: Set when entity is modified
        /// - IsDeleted: Set to true instead of deleting the entity (soft-delete)
        ///
        /// Behavior:
        /// - On Added: Sets CreatedAt and UpdatedAt to current UTC time, IsDeleted to false
        /// - On Modified: Updates UpdatedAt to current UTC time
        /// - On Deleted: Converts to Modified state, sets IsDeleted to true, updates UpdatedAt
        ///
        /// This ensures consistent audit tracking and prevents physical deletion of records,
        /// allowing for data recovery and historical auditing.
        ///
        /// Note:
        /// - Callers must ensure that any necessary business logic or validation is performed
        ///   before invoking SaveChangesAsync, as this method focuses solely on audit management.
        /// - For hard-deletion scenarios, a separate method should be implemented to bypass
        ///   the soft-delete logic.
        ///
        /// Performance Considerations:
        /// - This method is optimized for clarity and reasonable performance by processing only
        ///   relevant states and minimizing reflection/lookup overhead. If SaveChanges becomes
        ///   a hotspot, consider profiling and offloading heavier domain logic to a service layer
        ///   executed before SaveChanges is called.
        ///
        /// Safety and Caveats:
        /// - Because audit and soft-delete logic run for every SaveChanges invocation, ensure callers
        ///   are aware that Deleted entries will not be removed from the database unless a hard-deletion
        ///   path is intentionally implemented.
        /// - Be mindful of migrations and administrative operations that need to bypass tenant filters
        ///   or soft-delete behavior; provide a clear escape (e.g. an admin flag or a separate maintenance
        ///   context) if necessary.
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) // cancellationToken is used to cancel the async operation if needed.
        {
            SetAuditProperties();
            
            // =====================================================================
            // DOMAIN EVENT COLLECTION AND DISPATCHING
            // =====================================================================
            // This section implements the event sourcing pattern where domain events
            // are collected from aggregates and dispatched AFTER successful persistence.
            // This ensures that events are only published when the database transaction
            // succeeds, maintaining data consistency.
            //
            // Event Flow:
            // 1. Collect all domain events from tracked aggregates
            // 2. Clear events from aggregates (prevent re-dispatch on retry)
            // 3. Save changes to database (atomic transaction)
            // 4. Dispatch events to handlers via MediatR (if save successful)
            // =====================================================================
            
            // 1. Collect domain events from all tracked aggregates that support events
            var domainEvents = CollectDomainEvents();
            
            // 2. Clear events from aggregates to prevent re-dispatch
            ClearDomainEventsFromAggregates();

            try
            {
                // 3. Save changes to database FIRST (ensures data consistency)
                var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                
                // 4. Dispatch events AFTER successful save
                if (domainEvents.Count > 0 && _domainEventDispatcher != null)
                {
                    await _domainEventDispatcher.DispatchEventsAsync(domainEvents, cancellationToken)
                        .ConfigureAwait(false);
                }
                
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new DbUpdateConcurrencyException(
                    "Concurrency conflict detected while saving changes to the database.", ex);
            }
        }
        
        /// <summary>
        /// Collects all domain events from tracked aggregates.
        /// </summary>
        /// <returns>A list of domain events to be dispatched after persistence.</returns>
        /// <remarks>
        /// This method iterates through all tracked entities that implement domain events
        /// and collects their pending events. Currently supports:
        /// - Order aggregate
        /// - CustomerLoyalty aggregate
        /// - Menu aggregate
        /// - SaleRecord entity
        /// 
        /// New aggregates that raise domain events should be added here.
        /// </remarks>
        private List<IDomainEvent> CollectDomainEvents()
        {
            var domainEvents = new List<IDomainEvent>();
            
            // Collect from Order aggregates
            var orderEntries = ChangeTracker.Entries<Order>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();
            
            foreach (var order in orderEntries)
            {
                domainEvents.AddRange(order.DomainEvents);
            }
            
            // Collect from CustomerLoyalty aggregates
            var loyaltyEntries = ChangeTracker.Entries<CustomerLoyalty>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();
            
            foreach (var loyalty in loyaltyEntries)
            {
                domainEvents.AddRange(loyalty.DomainEvents);
            }
            
            // Collect from Menu aggregates
            var menuEntries = ChangeTracker.Entries<Menu>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();
            
            foreach (var menu in menuEntries)
            {
                domainEvents.AddRange(menu.DomainEvents);
            }
            
            // Collect from SaleRecord entities
            var saleRecordEntries = ChangeTracker.Entries<SaleRecord>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();
            
            foreach (var saleRecord in saleRecordEntries)
            {
                domainEvents.AddRange(saleRecord.DomainEvents);
            }
            
            return domainEvents;
        }
        
        /// <summary>
        /// Clears domain events from all tracked aggregates to prevent re-dispatch.
        /// </summary>
        /// <remarks>
        /// This must be called BEFORE saving to the database to ensure events
        /// are not dispatched multiple times if SaveChangesAsync is retried.
        /// </remarks>
        private void ClearDomainEventsFromAggregates()
        {
            // Clear from Order aggregates
            var orderEntries = ChangeTracker.Entries<Order>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();
            
            foreach (var order in orderEntries)
            {
                order.ClearDomainEvents();
            }
            
            // Clear from CustomerLoyalty aggregates
            var loyaltyEntries = ChangeTracker.Entries<CustomerLoyalty>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();
            
            foreach (var loyalty in loyaltyEntries)
            {
                loyalty.ClearDomainEvents();
            }
            
            // Clear from Menu aggregates
            var menuEntries = ChangeTracker.Entries<Menu>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();
            
            foreach (var menu in menuEntries)
            {
                menu.ClearDomainEvents();
            }
            
            // Clear from SaleRecord entities
            var saleRecordEntries = ChangeTracker.Entries<SaleRecord>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();
            
            foreach (var saleRecord in saleRecordEntries)
            {
                saleRecord.ClearDomainEvents();
            }
        }


        /// SetAuditProperties() Method: I extracted the entire body of your original SaveChangesAsync (excluding the transaction and the final base.        /// SaveChangesAsync call) into a new private method, SetAuditProperties().
        /// This method now contains all the logic for profile synchronization, business rule management, and audit field stamping.
        
        /// <summary>
        /// Sets audit-related properties and synchronizes profile and business rule data for tracked entities before
        /// changes are saved to the database context.
        /// </summary>
        /// <remarks>This method updates standard audit fields such as creation and modification
        /// timestamps, and manages soft-delete behavior for entities derived from EntityBase. It also ensures that
        /// profile relationships for ApplicationUser entities are synchronized and that only one active business rule
        /// of each type exists per admin user. This method should be called before persisting changes to maintain data
        /// consistency and enforce business rules.</remarks>
        private void SetAuditProperties()
        {
            var now = DateTime.UtcNow;

            // Gather all tracked EntityBase entries(including ApplicationUser) in relevant states ( Added, Modified, Deleted)
            var entriesEstates = ChangeTracker
                .Entries<EntityBase>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .ToList();

            /// <summary>
            /// Profile Type Management Logic
            /// This section delegates profile synchronization to the ApplicationUser entity.
            /// The ApplicationUser.SynchronizeProfiles() method handles:
            /// - Profile type validation
            /// - Profile relationship management
            /// - Data consistency
            /// - Cleanup of old relationships
            /// </summary>

            // Get ApplicationUser entries that are being added or modified
            var appUserEntries = ChangeTracker
                .Entries<ApplicationUser>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .ToList();

            // Track processed users to avoid duplicate synchronization
            var processedUsers = new HashSet<string>();

            foreach (var entry in appUserEntries)
            {
                var appUser = entry.Entity;

                // Skip if already processed
                if (!processedUsers.Add(appUser.Id))
                    continue;

                // Use the entity's synchronization method
                appUser.SynchronizeProfiles();

                // If entity is modified and ProfileId changed, the UpdatedAt is already set by SynchronizeProfiles
                if (entry.State == EntityState.Modified)
                {
                    var profileIdProperty = entry.Property(nameof(ApplicationUser.ProfileId));
                    if (profileIdProperty.IsModified)
                    {
                        entry.Property(nameof(ApplicationUser.UpdatedAt)).IsModified = true;
                    }
                }
            }

            /// <summary>
            /// Business Rule Management Logic
            /// This section handles the synchronization and management of business rules:
            /// 1. Rule Deactivation:
            ///    - Automatically deactivates existing active rules when a new active rule of the same type is saved
            ///    - Ensures only one rule per type is active at a time
            ///    - Maintains historical record of all rule changes
            /// 
            /// 2. Admin User Synchronization:
            ///    - Tracks which admin users have been updated to prevent duplicate updates
            ///    - Uses HashSet for efficient duplicate checking
            ///    - Updates corresponding AdminUser properties when rules change
            /// </summary>

            // Get business rule entries that are being added or modified
            var businessRuleEntries = ChangeTracker
                .Entries<BusinessRule>()
                .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified) && e.Entity.IsCurrentValue)
                .ToList();

            // Track updated admin users to avoid duplicate updates
            var updatedAdminUsers = new HashSet<int>();

            // Process business rule changes and synchronize with AdminUser properties
            foreach (var entry in businessRuleEntries)
            {
                var rule = entry.Entity;
                if (!updatedAdminUsers.Add(rule.AdminUserId))
                    continue;

                if (rule.IsCurrentValue)
                {
                    var existingActiveRules = ChangeTracker
                        .Entries<BusinessRule>()
                        .Where(e => e.Entity.AdminUserId == rule.AdminUserId &&
                                  e.Entity.RuleType == rule.RuleType &&
                                  e.Entity.IsCurrentValue &&
                                  e.Entity != rule)
                        .ToList();

                    foreach (var existingRule in existingActiveRules)
                    {
                        existingRule.Entity.IsCurrentValue = false;
                        existingRule.State = EntityState.Modified;
                    }
                }

                if (rule.AdminUser != null)
                {
                    rule.SynchronizeWithAdminUser();
                }
            }

            // Process standard audit fields for entities deriving from EntityBase including entities ApplicationUser.
            foreach (var entry in entriesEstates)
            {
                var createdAtProp = entry.Property(nameof(EntityBase.CreatedAt));
                var updatedAtProp = entry.Property(nameof(EntityBase.UpdatedAt));
                var isDeletedProp = entry.Property(nameof(EntityBase.IsDeleted));

                switch (entry.State)
                {
                    case EntityState.Added: // handle audit fields for Added entities
                        if (!(createdAtProp.CurrentValue is DateTime created) || created == default)
                        {
                            createdAtProp.CurrentValue = now;
                        }
                        updatedAtProp.CurrentValue = now;
                        isDeletedProp.CurrentValue = false;
                        break;

                    case EntityState.Modified: // handle audit fields for Modified entities
                        if (createdAtProp.IsModified)
                            createdAtProp.IsModified = false;
                        updatedAtProp.CurrentValue = now;
                        break;
                    // As well I can implement a hard-delete method that bypasses the soft-delete logic in my `AppDbContext`.(test later)
                    case EntityState.Deleted: // handle soft-delete for Deleted entities
                                              // Convert to Modified state for soft-delete
                        entry.State = EntityState.Modified;
                        isDeletedProp.CurrentValue = true;
                        if (createdAtProp.IsModified)
                            createdAtProp.IsModified = false;
                        updatedAtProp.CurrentValue = now;
                        break;
                }
            }
        }
    }
}
