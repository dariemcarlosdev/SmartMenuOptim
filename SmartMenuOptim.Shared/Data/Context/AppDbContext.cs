using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Shared.Data.Entities;

namespace SmartMenuOptim.Shared.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure entity relationships and properties here if needed

            // Category-Dish relationship: required
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Dishes)
                .WithOne(d => d.Category)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Dish-Category relationship: required
            modelBuilder.Entity<Dish>()
                .HasMany(d => d.Reviews)
                .WithOne(r => r.Dish)
                .HasForeignKey(r => r.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            // Dish-SaleRecord relationship: required
            modelBuilder.Entity<Dish>()
                .HasMany(d => d.SaleRecords)
                .WithOne(s => s.Dish)
                .HasForeignKey(s => s.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            // Customer-Review relationship: optional
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(modelBuilder);
        }
        // DbSets for your entities
        public DbSet<Review> Reviews { get; set; }
        public DbSet<SaleRecord> SaleRecords { get; set; }
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Customer> Customers { get; set; } // Add Customer DbSet
        public DbSet<AdminUser> AdminUsers { get; set; } // Add AdminUser DbSet

        // Add other DbSets as needed
    }
}
