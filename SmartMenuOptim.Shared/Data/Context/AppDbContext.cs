using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Shared.Data.Entities;

namespace SmartMenuOptim.Shared.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure entity relationships and properties here if needed
            base.OnModelCreating(modelBuilder);
        }
        // DbSets for your entities
        public DbSet<Review> Reviews { get; set; }
        public DbSet<SaleRecord> SaleRecords { get; set; }

        // Add other DbSets as needed
    }
}
