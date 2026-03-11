using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMenuOptim.Domain.Aggregates.MenuAggregate;

namespace SmartMenuOptim.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the MenuDish join entity.
/// </summary>
/// <remarks>
/// MenuDish represents the many-to-many relationship between Menu and Dish
/// with additional properties like DisplayOrder, SpecialPrice, and Notes.
/// </remarks>
internal sealed class MenuDishConfiguration : IEntityTypeConfiguration<MenuDish>
{
    public void Configure(EntityTypeBuilder<MenuDish> builder)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // TABLE & PRIMARY KEY (COMPOSITE)
        // ═══════════════════════════════════════════════════════════════════════
        builder.ToTable("MenuDishes");

        builder.HasKey(md => new { md.MenuId, md.DishId });

        // ═══════════════════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════
        // DisplayOrder is always set by the domain (Menu.AddDish method)
        // Use ValueGeneratedNever to ensure EF Core always includes it in INSERT
        builder.Property(md => md.DisplayOrder)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(md => md.SpecialPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(md => md.Notes)
            .HasMaxLength(500);

        // ═══════════════════════════════════════════════════════════════════════
        // RELATIONSHIPS
        // ═══════════════════════════════════════════════════════════════════════
        builder.HasOne(md => md.Menu)
            .WithMany(m => m.MenuDishes)
            .HasForeignKey(md => md.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(md => md.Dish)
            .WithMany(d => d.MenuDishes)
            .HasForeignKey(md => md.DishId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restaurant tenant relationship (from TenantEntityBase)
        builder.HasOne(md => md.Restaurant)
            .WithMany()
            .HasForeignKey(md => md.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        // ═══════════════════════════════════════════════════════════════════════
        // INDEXES
        // ═══════════════════════════════════════════════════════════════════════
        builder.HasIndex(new[] { "DishId", "MenuId" })
            .HasDatabaseName("IX_MenuDishes_Dish_Menu");

        builder.HasIndex(new[] { "RestaurantId", "MenuId", "DishId" })
            .HasDatabaseName("IX_MenuDishes_Restaurant_Menu_Dish");

        builder.HasIndex(md => new { md.MenuId, md.DisplayOrder })
            .HasDatabaseName("IX_MenuDishes_Menu_DisplayOrder");

        // ═══════════════════════════════════════════════════════════════════════
        // QUERY FILTERS
        // ═══════════════════════════════════════════════════════════════════════
        builder.HasQueryFilter(md => !md.IsDeleted);
    }
}
