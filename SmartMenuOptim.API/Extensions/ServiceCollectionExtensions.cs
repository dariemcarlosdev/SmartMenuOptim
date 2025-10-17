using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.API.Services;
using SmartMenuOptim.API.Services.Interfaces;
using SmartMenuOptim.Shared.Data.Context;
using SmartMenuOptim.Shared.Data.Interfaces;
using SmartMenuOptim.Shared.Data.Repositories;

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
            // Set the default API version to 1.0.
            options.DefaultApiVersion = new ApiVersion(1, 0);
            // Assume the default version when a client doesn't specify one.
            options.AssumeDefaultVersionWhenUnspecified = true;
            // Include the API versions in the response headers.
            options.ReportApiVersions = true;
            // Configure how the API version is read from the request (from query string or URL segment).
            options.ApiVersionReader = ApiVersionReader.Combine(
                new QueryStringApiVersionReader("X-Api-Version"),
                new UrlSegmentApiVersionReader());
        })
        .AddMvc() // Required for controllers and API explorer.
        .AddApiExplorer(options =>
        {
            // Format the group name for Swagger documentation (e.g., 'v1').
            options.GroupNameFormat = "'v'VVV";
            // Substitute the API version in the URL paths.
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
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection string is missing!")));

        // Register the Unit of Work and repository patterns for data access.
        services.AddScoped<IUnityOfWork, UnityOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IRepositoryWithIncludes<>), typeof(Repository<>));

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
        services.AddScoped<IAiImprovementStrategyService, AiImprovementService>();
        services.AddScoped<IOpenIAGptService, OpenIaGptService>();
        
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
                              .AllowAnyHeader(); // Allow any HTTP headers
                    }
                });
        });

        return services;
    }
}
