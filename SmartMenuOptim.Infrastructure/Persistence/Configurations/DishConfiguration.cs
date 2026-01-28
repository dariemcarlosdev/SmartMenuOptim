using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMenuOptim.Domain.Aggregates.DishAggregate;
using SmartMenuOptim.Domain.Entities;

namespace SmartMenuOptim.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// According to Clean Architecture principles, configuration classes for entities should reside in the Infrastructure layer.
    /// This class is intended to hold the configuration settings for the Dish entity.
    /// By placing it here, we ensure that the domain and application layers remain free of infrastructure concerns, promoting a clean separation of responsibilities.
    /// Example configurations might include table mappings, relationships, constraints, and other database-related settings.
    /// </summary>
    internal class DishConfiguration : IEntityTypeConfiguration<Dish>
    {
        //This is a sample configuration. Adjust properties and relationships as per your actual Dish entity definition.
        // Currently, it configures the table name, primary key, and some properties of the Dish entity which actually is defined in AppDbContext.
        // DbContext will automatically apply this configuration during model creation.
        // Make sure to expand this configuration based on the actual structure and requirements of your Dish entity.
        /// I need to take out any configuration defined in AppDbContext OnModelCreating method and move it here.
        public void Configure(EntityTypeBuilder<Dish> builder)
        {
            builder.ToTable("Dishes");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(d => d.Description)
                .HasMaxLength(1000);

            builder.Property(d => d.DishPrice)
                .HasPrecision(18, 2);

            builder.HasIndex(d => d.Name);
        }
    }
}
