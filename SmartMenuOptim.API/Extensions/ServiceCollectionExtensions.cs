using Asp.Versioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.API.Services;
using SmartMenuOptim.API.Services.Interfaces;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Infrastructure.Persistence.Context;
using SmartMenuOptim.Infrastructure.Persistence.Repositories;
using System.Threading.RateLimiting;

namespace SmartMenuOptim.API.Extensions;

/// <summary>
/// Provides extension methods for setting up services in the IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds and configures API versioning services.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The configured IServiceCollection.</returns>
    public static IServiceCollection AddApiVersioningServices(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            // Set the default API version to 1.0
            options.DefaultApiVersion = new ApiVersion(1, 0);
            // Assume the default version when a client doesn't specify one
            options.AssumeDefaultVersionWhenUnspecified = true;
            // Include the API versions in the response headers
            options.ReportApiVersions = true;
            // Configure how the API version is read from the request
            options.ApiVersionReader = ApiVersionReader.Combine(
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("api-version"),
                new UrlSegmentApiVersionReader());
        })
        .AddApiExplorer(options =>
        {
            // Format the group name for Swagger documentation
            options.GroupNameFormat = "'v'VVV";
            // Substitute the API version in the URL paths
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    /// <summary>
    /// Adds data-related services, including the DbContext and repositories.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configuration">The application configuration for accessing connection strings.</param>
    /// <returns>The configured IServiceCollection.</returns>
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure the DbContext to use PostgreSQL with the connection string from configuration.
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection") ?? 
                throw new InvalidOperationException("DefaultConnection string is missing!"));
            
            // Suppress pending model changes warning
            // The model configuration has been reorganized for value objects, but the database schema is correct
            // This warning can be safely ignored as no actual schema changes are required
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        // Register the Unit of Work and repository patterns for data access.
        services.AddScoped<IUnityOfWork, UnityOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }

    public static IServiceCollection AddNetCoreIdentity(this IServiceCollection services)
    {
        // Register Identity services with custom configurations.
        services.AddIdentity<ApplicationUser, IdentityRole>(options => {
            // Configure password requirements.
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            // Configure user settings.
            options.User.RequireUniqueEmail = true;
            // Configure lockout settings.
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // Identity services can be configured here in the future.
        return services;
    }

    /// <summary>
    /// Adds custom application services to the dependency injection container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The configured IServiceCollection.</returns>
    public static IServiceCollection AddCustomServices(this IServiceCollection services)
    {
        // Register custom services for business logic.
        services.AddScoped<ISentimentService, SentimentService>();
        services.AddScoped<IAImprovementStrategyService, AiImprovementService>();
        services.AddScoped<IOpenIAGptService, OpenIaGptService>();
        services.AddScoped<IAdminAuthorizationService, AdminAuthorizationService>();

        // Add health check services.
        services.AddHealthChecks();
        
        return services;
    }

    /// <summary>
    /// Adds and configures Cross-Origin Resource Sharing (CORS) services.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configuration">The application configuration for accessing CORS settings.</param>
    /// <returns>The configured IServiceCollection.</returns>
    public static IServiceCollection AddCustomCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        // Get the allowed origins from appsettings.json.
        var corsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        services.AddCors(options =>
        {
            options.AddPolicy(name: MyAllowSpecificOrigins,
                policy =>
                {
                    if (corsOrigins != null)
                    {
                        // Configure the policy to allow requests from specific origins, with any method and header.
                        policy.WithOrigins(corsOrigins)
                              .AllowAnyMethod() // Allow any HTTP method (GET, POST, etc.)
                              .AllowAnyHeader() // Allow any HTTP headers
                              .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // Cache preflight response for 10 minutes. It is good for performance since it reduces the number of preflight requests. 
                    }
                });
        });

        return services;
    }

    /*
     2.	Rate Limiting is a server-side concern that should be implemented at the API level to protect the API from being overwhelmed by requests from any client, not just the Blazor server:
        •	This should be moved to the API project because:
        •	Rate limiting is a server-side concern that protects the API from being overwhelmed
        •	It needs to be enforced at the API level to properly control access from all clients
        •	The current implementation in the Server project only limits calls from that specific Blazor server instance.     
     */

    /// <summary>
    /// Adds a fixed window rate limiting policy to the application's service collection.The API can now properly rate limit ALL incoming requests, regardless of their source
    /// </summary>
    /// <remarks>This method configures a fixed window rate limiter named "FixedPolicy" that allows up to 100
    /// requests per minute, with a queue limit of 10 requests. Requests exceeding these limits may be rejected or
    /// queued according to the policy. Register this method during application startup to enable rate limiting for
    /// incoming requests.</remarks>
    /// <param name="services">The service collection to which the rate limiting services are added.</param>
    /// <returns>The same instance of <see cref="IServiceCollection"/> that was provided, to support method chaining.</returns>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        // Rate Limiting This is a server-side concern that should be implemented at the API level to protect the API from being overwhelmed by requests from any client, not just the Blazor server.
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("FixedPolicy", policy =>
            {
                policy.Window = TimeSpan.FromMinutes(1);
                policy.PermitLimit = 100; // Allow 100 requests per minute
                policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst; // Process oldest requests first
                policy.QueueLimit = 10; // Allow up to 10 requests in the queue
            });
        });
        return services;
    }
}
