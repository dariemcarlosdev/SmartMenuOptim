using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartMenuOptim.API;

namespace SmartMenuOptim.Tests.IntegrationTests.Helpers;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// This class provides a test server configuration that uses an in-memory database
/// instead of the real PostgreSQL database used in production.
/// </summary>
/// <typeparam name="TProgram">The entry point class of the application (Program.cs)</typeparam>
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    /// <summary>
    /// Configures the web host for testing purposes.
    /// This method is called by the base WebApplicationFactory to setup the test server.
    /// </summary>
    /// <param name="builder">The IWebHostBuilder used to configure the test server</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configure the test services
        builder.ConfigureServices(services =>
        {
            // Find and remove the real database context configuration
            // This prevents conflicts between PostgreSQL and In-Memory providers
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DbContext>));

            if (descriptor != null)
            {
                // Remove the existing database context configuration
                // This is crucial to avoid the "multiple provider" error
                services.Remove(descriptor);
            }

            // Add the in-memory database context for testing
            // This replaces the real database with a lightweight in-memory version
            services.AddDbContext<DbContext>(options =>
            {
                // Configure the context to use the in-memory database
                // The "TestDatabase" name is used to identify the test database instance
                options.UseInMemoryDatabase("TestDatabase");
            });

            // Create a new service provider with our test configuration
            var serviceProvider = services.BuildServiceProvider();

            // Create a scope for database operations
            // This ensures proper disposal of resources
            using var scope = serviceProvider.CreateScope();
            
            // Get the database context from the service provider
            var db = scope.ServiceProvider.GetRequiredService<DbContext>();

            // Initialize the in-memory database
            // This creates the database schema based on your entity configurations
            db.Database.EnsureCreated();
        });
    }
}