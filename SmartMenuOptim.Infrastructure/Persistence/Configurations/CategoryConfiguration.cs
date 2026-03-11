using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMenuOptim.Domain.Entities.RestaurantEntities;

namespace SmartMenuOptim.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the DishCategory entity.
/// </summary>
/// <remarks>
/// DishCategory organizes dishes into groups (e.g., Appetizers, Main Courses, Desserts).
/// Each category belongs to a specific restaurant (tenant-scoped).
/// </remarks>
internal sealed class CategoryConfiguration : IEntityTypeConfiguration<DishCategory>
{
    public void Configure(EntityTypeBuilder<DishCategory> builder)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // TABLE & PRIMARY KEY
        // ═══════════════════════════════════════════════════════════════════════
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        // ═══════════════════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        // DisplayOrder is always set by the domain, use ValueGeneratedNever
        // to ensure EF Core always includes it in INSERT statements
        builder.Property(c => c.DisplayOrder)
            .IsRequired()
            .ValueGeneratedNever();

        // IsActive is always set by the domain (defaults to true in EntityBase),
        // use ValueGeneratedNever to ensure EF Core always includes it in INSERT statements
        builder.Property(c => c.IsActive)
            .IsRequired()
            .ValueGeneratedNever();

        // ═══════════════════════════════════════════════════════════════════════
        // RELATIONSHIPS
        // ═══════════════════════════════════════════════════════════════════════
        builder.HasOne(c => c.Restaurant)
            .WithMany(r => r.Categories)
            .HasForeignKey(c => c.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Dishes relationship configured in DishConfiguration

        // ═══════════════════════════════════════════════════════════════════════
        // INDEXES
        // ═══════════════════════════════════════════════════════════════════════
        // Unique index for category names per restaurant to enforce uniqueness
        builder.HasIndex(c => new { c.RestaurantId, c.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false") // PostgreSQL syntax for filtered index
            .HasDatabaseName("IX_Categories_Restaurant_UniqueName");

        builder.HasIndex(c => new { c.RestaurantId, c.DisplayOrder })
            .HasDatabaseName("IX_Categories_Restaurant_DisplayOrder");

        builder.HasIndex(c => new { c.RestaurantId, c.IsActive })
            .HasDatabaseName("IX_Categories_Restaurant_Active");

        // ═══════════════════════════════════════════════════════════════════════
        // QUERY FILTERS
        // ═══════════════════════════════════════════════════════════════════════
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
