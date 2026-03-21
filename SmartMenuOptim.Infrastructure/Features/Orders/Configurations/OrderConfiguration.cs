using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Entities;

namespace SmartMenuOptim.Infrastructure.Features.Orders.Configurations;

/// <summary>
/// Entity Type Configuration for the Order aggregate root.
/// </summary>
/// <remarks>
/// By placing configuration here, we ensure that the domain and application layers remain free of infrastructure concerns, 
/// promoting a clean separation of responsibilities.
/// </remarks>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <summary>
    /// Entity Framework Core configuration for the Order entity.
    /// Defines table mapping, keys, properties, indexes, and relationships.
    /// </summary>
    /// <param name="builder">The builder used to configure the Order entity type.</param>
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedOnAdd();

        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .IsRequired(false);

        builder.HasIndex(o => o.CreatedAt);

        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
