using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMenuOptim.Domain.Aggregates.RestaurantAggregate;
using SmartMenuOptim.Domain.Entities;

namespace SmartMenuOptim.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// According to Clean Architecture principles, configuration classes for entities should reside in the Infrastructure layer.
    /// This class is intended to hold the configuration settings for the Restaurant entity.
    /// By placing it here, we ensure that the domain and application layers remain free of infrastructure concerns, promoting a clean separation of responsibilities.
    /// Example configurations might include table mappings, relationships, constraints, and other database-related settings.
    /// </summary>
    internal class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
    {
        /// <summary>
        /// Configures the entity type mapping for the Restaurant entity.
        /// </summary>
        /// <remarks>
        /// This method is typically called by the Entity Framework infrastructure when building
        /// the model. It defines table mapping, property constraints, and indexes for the Restaurant entity.
        /// The configuration for the Restaurant entity is placed in the Infrastructure layer to adhere to Clean Architecture principles,
        /// and currently it is kept minimal as the domain model does not require complex configurations at this time.
        /// I need to take out any configuration defined in AppDbContext OnModelCreating method and move it here.
        /// </remarks>
        /// <param name="builder">The builder used to configure the Restaurant entity type.</param>
        public void Configure(EntityTypeBuilder<Restaurant> builder)
        {
            builder.ToTable("Restaurants");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(r => r.Location)
                .HasMaxLength(500);

            builder.Property(r => r.ContactPhone)
                .HasMaxLength(20);

            builder.Property(r => r.ContactEmail)
                .HasMaxLength(100);

            builder.HasIndex(r => r.Name);
        }
    }
}
