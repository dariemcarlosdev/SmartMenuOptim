using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMenuOptim.Domain.Aggregates.MenuAggregate;

namespace SmartMenuOptim.Infrastructure.Features.Restaurants.Configurations;

/// <summary>
/// EF Core configuration for the Menu aggregate root.
/// </summary>
/// <remarks>
/// Menu is an aggregate root that represents a collection of dishes
/// available at specific times (e.g., Breakfast Menu, Lunch Menu).
/// </remarks>
internal sealed class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // TABLE & PRIMARY KEY
        // ═══════════════════════════════════════════════════════════════════════
        builder.ToTable("Menus");
        builder.HasKey(m => m.Id);

        // ═══════════════════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════
        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Description)
            .HasMaxLength(1000);

        builder.Property(m => m.AvailableFrom)
            .HasColumnType("time");

        builder.Property(m => m.AvailableTo)
            .HasColumnType("time");

        builder.Property(m => m.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        // ═══════════════════════════════════════════════════════════════════════
        // BACKING FIELD CONFIGURATION
        // ═══════════════════════════════════════════════════════════════════════
        // Configure EF Core to use the private backing field for MenuDishes collection
        // This ensures proper encapsulation while allowing EF Core to track changes
        // When Menu.AddDish() or Menu.RemoveDish() is called, and MenuDishes collection is modified, EF Core will be aware of the changes and persist them correctly.
        builder.Navigation(m => m.MenuDishes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // ═══════════════════════════════════════════════════════════════════════
        // RELATIONSHIPS
        // ═══════════════════════════════════════════════════════════════════════
        builder.HasOne(m => m.Restaurant)
            .WithMany(r => r.Menus)
            .HasForeignKey(m => m.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.MenuType)
            .WithMany(mt => mt.Menus)
            .HasForeignKey(m => m.MenuTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // MenuDishes relationship configured in MenuDishConfiguration

        // ═══════════════════════════════════════════════════════════════════════
        // INDEXES
        // ═══════════════════════════════════════════════════════════════════════
        builder.HasIndex(m => new { m.RestaurantId, m.AvailableFrom, m.AvailableTo })
            .HasDatabaseName("IX_Menus_Restaurant_Availability");

        builder.HasIndex(m => new { m.RestaurantId, m.IsActive, m.AvailableFrom, m.AvailableTo })
            .HasDatabaseName("IX_Menus_Restaurant_Availability_Active");

        builder.HasIndex(m => new { m.RestaurantId, m.Name })
            .HasDatabaseName("IX_Menus_Restaurant_Name");

        // ═══════════════════════════════════════════════════════════════════════
        // QUERY FILTERS
        // ═══════════════════════════════════════════════════════════════════════
        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
