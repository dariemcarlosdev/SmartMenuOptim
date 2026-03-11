using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMenuOptim.Domain.Features.Restaurants;

namespace SmartMenuOptim.Infrastructure.Features.Restaurants.Configurations;

/// <summary>
/// EF Core configuration for the BusinessHours entity (child of Restaurant aggregate).
/// </summary>
/// <remarks>
/// BusinessHours is a child entity within the Restaurant aggregate root.
/// It represents the operating hours for each day of the week.
/// </remarks>
internal sealed class BusinessHoursConfiguration : IEntityTypeConfiguration<BusinessHours>
{
    public void Configure(EntityTypeBuilder<BusinessHours> builder)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // TABLE & PRIMARY KEY
        // ═══════════════════════════════════════════════════════════════════════
        builder.ToTable("BusinessHours");

        builder.HasKey(bh => bh.Id);

        builder.Property(bh => bh.Id)
            .UseIdentityAlwaysColumn()
            .ValueGeneratedOnAdd();

        // ═══════════════════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════
        builder.Property(bh => bh.RestaurantId)
            .IsRequired();

        builder.Property(bh => bh.DayOfWeek)
            .IsRequired()
            .HasConversion<int>(); // Store as int in database

        builder.Property(bh => bh.OpenTime)
            .IsRequired();

        builder.Property(bh => bh.CloseTime)
            .IsRequired();

        // IsClosed is a computed property (OpenTime == CloseTime), not stored in database
        builder.Ignore(bh => bh.IsClosed);

        // ═══════════════════════════════════════════════════════════════════════
        // RELATIONSHIPS
        // ═══════════════════════════════════════════════════════════════════════
        // Configured from Restaurant side - one Restaurant has many BusinessHours
        // Note: Navigation property on Restaurant entity as OperatingHours collection

        // ═══════════════════════════════════════════════════════════════════════
        // INDEXES
        // ═══════════════════════════════════════════════════════════════════════
        // Unique constraint to prevent duplicate hours for same day per restaurant
        builder.HasIndex(bh => new { bh.RestaurantId, bh.DayOfWeek })
            .IsUnique()
            .HasDatabaseName("IX_BusinessHours_Restaurant_Day_Unique");

        builder.HasIndex(bh => bh.RestaurantId)
            .HasDatabaseName("IX_BusinessHours_RestaurantId");
    }
}
