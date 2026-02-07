using Asp.Versioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using SmartMenuOptim.Application.Extensions;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Extensions;
using SmartMenuOptim.Infrastructure.Extensions;
using SmartMenuOptim.Infrastructure.Persistence.Context;
using System.Threading.RateLimiting;

namespace SmartMenuOptim.API.Extensions;

/// <summary>
/// Provides extension methods for setting up API-specific services in the IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures all API services including controllers, infrastructure, application, domain, CORS, Swagger, and rate limiting.
    /// This unified method sets up the complete service configuration for the Web API application.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder to configure.</param>
    /// <returns>The configured WebApplicationBuilder.</returns>
    public static WebApplicationBuilder ApiServiceCollection(this WebApplicationBuilder builder)
    {
        // 1. Add Controllers
        builder.Services.AddControllers();
        
        // 2. Add API versioning (currently commented out, but available for future use)
        // builder.Services.AddApiVersioningServices();
        
        // 3. Add Infrastructure layer services (data access, external services)
        builder.Services.AddInfrastructureServices(builder.Configuration);
        
        // 4. Add Identity services (ASP.NET Core specific)
        builder.Services.AddNetCoreIdentity();
        
        // 5. Add Application layer services (use cases and orchestration)
        builder.Services.AddApplicationServices();
        
        // 6. Add Domain layer services (business logic)
        builder.Services.AddDomainServices();
        
        // 7. Add CORS policy (API-specific concern)
        builder.Services.AddCustomCorsPolicy(builder.Configuration);
        
        // 8. Add API documentation services
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // 9. Add rate limiting services (API-level protection)
        builder.Services.AddRateLimiting();
        
        // 10. Add health check services
        builder.Services.AddHealthChecks();
        
        return builder;
    }

    /// <summary>
    /// Adds API versioning and API explorer services to the specified service collection, enabling support for
    /// versioned APIs and version discovery in documentation tools.
    /// </summary>
    /// <remarks>This method configures default API versioning behavior, including setting a default API
    /// version, supporting versioning via headers, query strings, and URL segments, and enabling API version reporting
    /// in responses. It also configures the API explorer to support versioned documentation, such as with
    /// Swagger.</remarks>
    /// <param name="services">The service collection to which the API versioning and explorer services will be added. Cannot be null.</param>
    /// <returns>The same instance of <see cref="IServiceCollection"/> that was provided, to support method chaining.</returns>
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
    /// Adds ASP.NET Core Identity services with custom configuration to the specified service collection.
    /// </summary>
    /// <remarks>This method configures Identity with specific password, user, and lockout settings, and
    /// registers Entity Framework stores and default token providers. This is kept in the API layer as it's
    /// an ASP.NET Core-specific concern that requires web framework dependencies.</remarks>
    /// <param name="services">The service collection to which the Identity services will be added.</param>
    /// <returns>The same service collection instance, enabling further configuration.</returns>
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

        return services;
    }

    /// <summary>
    /// Adds a custom CORS policy to the service collection using origins specified in the configuration.
    /// </summary>
    /// <remarks>The policy allows requests from the specified origins with any HTTP method and header, and
    /// caches preflight responses for 10 minutes. The allowed origins must be defined in the configuration; if not
    /// specified, no origins will be allowed by this policy.</remarks>
    /// <param name="services">The service collection to which the CORS policy will be added.</param>
    /// <param name="configuration">The application configuration containing the allowed CORS origins under the 'Cors:AllowedOrigins' section.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
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
    /// Adds fixed window rate limiting services to the application's dependency injection container.
    /// </summary>
    /// <remarks>This method configures a fixed window rate limiter that restricts clients to a maximum number
    /// of requests per minute, helping to protect the API from excessive or abusive traffic. Rate limiting should be
    /// applied at the API level to ensure all clients are subject to the same restrictions.</remarks>
    /// <param name="services">The service collection to which the rate limiting services will be added.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance with rate limiting services registered. This enables
    /// method chaining.</returns>
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
