using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMenuOptim.Domain.Aggregates.OrderAggregate;
using SmartMenuOptim.Domain.Entities;

namespace SmartMenuOptim.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Entity Type Configuration for the Order aggregate root.
    /// According to Clean Architecture principles, configuration classes for entities should reside in the Infrastructure layer.
    /// This class defines the persistence mapping for the Order entity, including table mappings, relationships, 
    /// constraints, indexes, and other database-related settings.
    /// </summary>
    /// <remarks>
    /// By placing configuration here, we ensure that the domain and application layers remain free of infrastructure concerns, 
    /// promoting a clean separation of responsibilities.
    /// 
    /// <para><strong>Usage in AppDbContext:</strong></para>
    /// <code>
    /// public class AppDbContext : DbContext
    /// {
    ///     protected override void OnModelCreating(ModelBuilder modelBuilder)
    ///     {
    ///         // Apply individual configuration
    ///         modelBuilder.ApplyConfiguration(new OrderConfiguration());
    ///         
    ///         // OR apply all configurations from assembly at once (recommended)
    ///         modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderConfiguration).Assembly);
    ///         
    ///         base.OnModelCreating(modelBuilder);
    ///     }
    /// }
    /// </code>
    /// 
    /// <para><strong>Benefits:</strong></para>
    /// <list type="bullet">
    ///     <item>Separates entity configuration from DbContext, keeping it focused and maintainable</item>
    ///     <item>Follows the Single Responsibility Principle - one configuration class per entity</item>
    ///     <item>Makes configurations reusable and testable</item>
    ///     <item>Aligns with Clean Architecture and DDD principles</item>
    /// </list>
    /// </remarks>
    internal class OrderConfiguration : IEntityTypeConfiguration<Order>
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

            //builder.Property(o => o.OrderNumber)
            //    .IsRequired()
            //    .HasMaxLength(50);

            builder.Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            builder.Property(o => o.CreatedAt)
                .IsRequired();

            builder.Property(o => o.UpdatedAt)
                .IsRequired(false);

            //builder.HasIndex(o => o.OrderNumber)
            //    .IsUnique();

            builder.HasIndex(o => o.CreatedAt);

            builder.HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
