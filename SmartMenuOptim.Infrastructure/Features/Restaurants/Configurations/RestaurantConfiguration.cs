using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMenuOptim.Domain.Aggregates.RestaurantAggregate;
using SmartMenuOptim.Infrastructure.Persistence.Context.Converters;

namespace SmartMenuOptim.Infrastructure.Features.Restaurants.Configurations;

/// <summary>
/// EF Core configuration for the Restaurant aggregate root.
/// </summary>
/// <remarks>
/// <para>
/// Following Clean Architecture principles, this configuration resides in the Infrastructure layer
/// to keep domain and application layers free of persistence concerns.
/// </para>
/// <para>
/// Configures:
/// - Table mapping and primary key
/// - Value object conversions (Address, Email, PhoneNumber)
/// - Operational properties (IsAcceptingOrders, MaxSimultaneousOrders)
/// - Relationships (Owner, BusinessHours)
/// - Indexes for query optimization
/// - Soft delete query filter
/// </para>
/// </remarks>
internal sealed class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // TABLE & PRIMARY KEY
        // ═══════════════════════════════════════════════════════════════════════
        builder.ToTable("Restaurants");
        builder.HasKey(r => r.Id);

        // ═══════════════════════════════════════════════════════════════════════
        // BASIC PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════
        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        builder.Property(r => r.TimeZoneId)
            .HasMaxLength(100)
            .HasDefaultValue("UTC");

        // ═══════════════════════════════════════════════════════════════════════
        // VALUE OBJECT CONVERSIONS
        // ═══════════════════════════════════════════════════════════════════════
        // Configure value object properties with explicit converters
        // This prevents EF Core from treating them as separate entities

        builder.Property(r => r.Location)
            .HasConversion(new AddressValueConverter())
            .HasColumnName("Address")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(r => r.ContactEmail)
            .HasConversion(new EmailValueConverter())
            .HasColumnName("Email")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(r => r.ContactPhone)
            .HasConversion(new PhoneNumberValueConverter())
            .HasColumnName("PhoneNumber")
            .HasMaxLength(20)
            .IsRequired();

        // ═══════════════════════════════════════════════════════════════════════
        // OPERATIONAL PROPERTIES
        // ═══════════════════════════════════════════════════════════════════════
        // IsAcceptingOrders - critical for knowing if orders can be placed
        // Use cases:
        // 1. Restaurant temporarily closes for orders (holidays, maintenance)
        // 2. Limits order intake during peak times
        // 3. Enables dynamic control over order flow
        // 4. UI filtering of restaurants accepting orders
        builder.Property(r => r.IsAcceptingOrders)
            .IsRequired()
            .HasDefaultValue(false);

        // MaxSimultaneousOrders - important for managing order volume
        // Use cases:
        // 1. Prevents overwhelming kitchen staff during busy periods
        // 2. Allows restaurants to set capacity limits based on resources
        // 3. Enables dynamic adjustment of order limits
        // 4. Kitchen workflow optimization
        builder.Property(r => r.MaxSimultaneousOrders)
            .IsRequired()
            .HasDefaultValue(50);

        // ═══════════════════════════════════════════════════════════════════════
        // RELATIONSHIPS
        // ═══════════════════════════════════════════════════════════════════════
        builder.HasOne(r => r.Owner)
            .WithMany(a => a.OwnedRestaurants)
            .HasForeignKey(r => r.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // BusinessHours relationship configured in BusinessHoursConfiguration

        // ═══════════════════════════════════════════════════════════════════════
        // INDEXES
        // ═══════════════════════════════════════════════════════════════════════
        builder.HasIndex(r => r.Name)
            .HasDatabaseName("IX_Restaurants_Name");

        builder.HasIndex(r => r.OwnerId)
            .HasDatabaseName("IX_Restaurants_OwnerId");

        builder.HasIndex(r => new { r.IsAcceptingOrders, r.IsActive })
            .HasDatabaseName("IX_Restaurants_AcceptingOrders_Active");

        // ═══════════════════════════════════════════════════════════════════════
        // QUERY FILTERS
        // ═══════════════════════════════════════════════════════════════════════
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
