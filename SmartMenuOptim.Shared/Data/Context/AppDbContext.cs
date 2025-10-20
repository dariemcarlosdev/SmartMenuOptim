using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Shared.Data.Entities;

namespace SmartMenuOptim.Shared.Data.Context
{
    public class AppDbContext : DbContext
    {
        // DbSets for your entities
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<SaleRecord> SaleRecords { get; set; }
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
    
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        // I use fluent API to configure relationships and constraints
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Restaurant-Owner relationship: Each Restaurant is owned by one AdminUser (Owner),
            // and an AdminUser can own multiple Restaurants (One-to-Many, Required).
            modelBuilder.Entity<Restaurant>()
                //.HasQueryFilter(p => !p.IsDeleted); //for soft delete propose
                .HasOne(r => r.Owner)
                .WithMany()
                .HasForeignKey(r => r.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restaurant-Dish relationship: Each Restaurant can have many Dishes,
            // and each Dish belongs to one Restaurant (One-to-Many, Required).
            modelBuilder.Entity<Restaurant>()
                .HasMany(r => r.Dishes)
                .WithOne(d => d.Restaurant)
                .HasForeignKey(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restaurant-Category relationship: Each Restaurant can have many Categories,
            // and each Category belongs to one Restaurant (One-to-Many, Required).
            modelBuilder.Entity<Restaurant>()
                .HasMany(r => r.Categories)
                .WithOne(c => c.Restaurant)
                .HasForeignKey(c => c.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restaurant-Review relationship: Each Restaurant can have many Reviews,
            // and each Review is associated with one Restaurant (One-to-Many, Required).
            modelBuilder.Entity<Restaurant>()
                .HasMany(r => r.Reviews)
                .WithOne(rv => rv.Restaurant)
                .HasForeignKey(rv => rv.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Category-Dish relationship: Each Category can have many Dishes,
            // and each Dish belongs to one Category (One-to-Many, Required).
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Dishes)
                .WithOne(d => d.Category)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Dish-Review relationship: Each Dish can have many Reviews,
            // and each Review is associated with one Dish (One-to-Many, Required).
            modelBuilder.Entity<Dish>()
                .HasMany(d => d.Reviews)
                .WithOne(r => r.Dish)
                .HasForeignKey(r => r.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            // Dish-SaleRecord relationship: Each Dish can have many SaleRecords,
            // and each SaleRecord is associated with one Dish (One-to-Many, Required).
            modelBuilder.Entity<Dish>()
                .HasMany(d => d.SaleRecords)
                .WithOne(s => s.Dish)
                .HasForeignKey(s => s.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            // Customer-Review relationship: Each Customer can have many Reviews,
            // and each Review can optionally be associated with a Customer (One-to-Many, Optional).
            // If a Customer is deleted, set CustomerId to null in Review (anonymous review support).
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        ///  This method overrides the default SaveChangesAsync to implement soft deletion.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) // cancelationToken is optional, but good practice for async methods. It allows the operation to be cancelled if needed.
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

}
