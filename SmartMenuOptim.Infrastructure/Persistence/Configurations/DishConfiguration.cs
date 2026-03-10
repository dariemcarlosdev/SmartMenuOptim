using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;

namespace SmartMenuOptim.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Dish aggregate root.
/// </summary>
/// <remarks>
/// Dish represents a menu item with pricing, categorization, and nutritional information.
/// Each dish belongs to a category and can be part of multiple menus.
/// </remarks>
internal sealed class DishConfiguration : IEntityTypeConfiguration<Dish>
{
    public void Configure(EntityTypeBuilder<Dish> builder)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // TABLE & PRIMARY KEY
        // ═══════════════════════════════════════════════════════════════════════
        builder.ToTable("Dishes");
        builder.HasKey(d => d.Id);

        // ═══════════════════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════
        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Description)
            .HasMaxLength(1000);

        builder.Property(d => d.DishPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(d => d.Calories)
            .HasColumnType("int");

        builder.Property(d => d.IsVegetarian)
            .IsRequired();

        builder.Property(d => d.IsSpicy)
            .IsRequired();

        builder.Property(d => d.Ingredients)
            .HasMaxLength(2000);

        builder.Property(d => d.IsActive)
            .IsRequired();

        // ═══════════════════════════════════════════════════════════════════════
        // RELATIONSHIPS
        // ═══════════════════════════════════════════════════════════════════════
        builder.HasOne(d => d.Category)
            .WithMany(c => c.Dishes)
            .HasForeignKey(d => d.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Restaurant)
            .WithMany(r => r.Dishes)
            .HasForeignKey(d => d.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        // MenuDishes relationship configured in MenuDishConfiguration
        // Reviews relationship configured in ReviewConfiguration
        // SaleRecords relationship configured in SaleRecordConfiguration

        // ═══════════════════════════════════════════════════════════════════════
        // INDEXES
        // ═══════════════════════════════════════════════════════════════════════
        // Unique index for dish names per restaurant
        builder.HasIndex(d => new { d.RestaurantId, d.Name })
            .IsUnique()
            .HasDatabaseName("IX_Dishes_Restaurant_UniqueName");

        builder.HasIndex(d => d.CategoryId)
            .HasDatabaseName("IX_Dishes_CategoryId");

        // Composite index for dish search by price range
        builder.HasIndex(d => new { d.RestaurantId, d.CategoryId, d.DishPrice })
            .HasDatabaseName("IX_Dishes_Restaurant_Category_Price");

        builder.HasIndex(d => new { d.RestaurantId, d.IsActive })
            .HasDatabaseName("IX_Dishes_Restaurant_Active");

        // Index for dietary filtering
        builder.HasIndex(d => new { d.RestaurantId, d.IsVegetarian, d.IsSpicy })
            .HasDatabaseName("IX_Dishes_Restaurant_Dietary");

        // ═══════════════════════════════════════════════════════════════════════
        // QUERY FILTERS
        // ═══════════════════════════════════════════════════════════════════════
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
