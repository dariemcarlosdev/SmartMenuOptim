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
    /// According to Clean Architecture principles, configuration classes for entities should reside in the Infrastructure layer.
    /// This class is intended to hold the configuration settings for the Order entity.
    /// By placing it here, we ensure that the domain and application layers remain free of infrastructure concerns, promoting a clean separation of responsibilities.
    /// Example configurations might include table mappings, relationships, constraints, and other database-related settings.
    /// </summary>
    internal class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        /// <summary>
        /// Configures the entity type mapping for the Order entity.
        /// </summary>
        /// <remarks>This method is typically called by the Entity Framework infrastructure when building
        /// the model. It defines table mapping, keys, property configurations, indexes, and relationships for the Order
        /// entity.I need to take out any configuration defined in AppDbContext OnModelCreating method and move it here.</remarks>
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
