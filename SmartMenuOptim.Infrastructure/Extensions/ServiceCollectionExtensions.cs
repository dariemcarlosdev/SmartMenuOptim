using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartMenuOptim.Domain.Entities.GlobalEntities;
using SmartMenuOptim.Domain.Repositories;
using SmartMenuOptim.Domain.Services.Abstraction;
using SmartMenuOptim.Infrastructure.BackgroundJobs;
using SmartMenuOptim.Infrastructure.Infrastructure.Services.Azure;
using SmartMenuOptim.Infrastructure.Persistence.Context;
using SmartMenuOptim.Infrastructure.Persistence.Repositories;

namespace SmartMenuOptim.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for registering Infrastructure layer services in the IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure-related services, including data access and external service adapters, to the application's
    /// dependency injection container.
    /// </summary>
    /// <remarks>This method registers services required for the application's infrastructure layer, such as
    /// database contexts, repositories, and adapters for external systems. It is typically called during application
    /// startup to ensure all necessary infrastructure dependencies are available.</remarks>
    /// <param name="services">The service collection to which the infrastructure services will be added.</param>
    /// <param name="configuration">The application configuration used to configure infrastructure services.</param>
    /// <returns>The service collection with the infrastructure services registered. This enables method chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add data services (DbContext, repositories, Unit of Work)
        services.AddDataServices(configuration);
        
        // Add external service adapters (Hexagonal Architecture adapters implementing domain ports)
        services.AddExternalServiceAdapters();
        
        return services;
    }

    
    /// <summary>
    /// Adds the application's data access services, including the database context and repository patterns, to the
    /// specified service collection.
    /// </summary>
    /// <remarks>This method configures the application's Entity Framework Core DbContext to use PostgreSQL
    /// and registers the Unit of Work and generic repository patterns for dependency injection. It also suppresses
    /// warnings about pending model changes when the database schema is already up to date.</remarks>
    /// <param name="services">The service collection to which the data services will be added. Cannot be null.</param>
    /// <param name="configuration">The application configuration used to retrieve the database connection string. Cannot be null.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the required 'DefaultConnection' connection string is missing from the configuration.</exception>
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Entity Framework Core DbContext with PostgreSQL
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection") ??
                throw new InvalidOperationException("DefaultConnection string is missing!"));
        });

        // Register the Unit of Work and repository patterns for data access.
        services.AddScoped<IUnityOfWork, UnityOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }


    /// <summary>
    /// Adds external service adapters that implement domain ports (interfaces).
    /// These are Hexagonal Architecture adapters implementing ports defined in the domain layer.
    /// This separation allows the core domain logic to remain independent of external services and technologies.
    /// Adapters can be swapped out for different implementations following the Dependency Inversion Principle.
    /// </summary>
    /// <remarks>
    /// Hexagonal Architecture (Ports and Adapters):
    /// - PORTS: Interfaces defined in the Domain layer (ISentimentAnalyzer, IAiTextGenerator)
    /// - ADAPTERS: Concrete implementations in Infrastructure layer (SentimentService, OpenIaGptService)
    /// 
    /// Registered External Service Adapters:
    /// 
    /// 1. Sentiment Analysis (ISentimentAnalyzer PORT):
    ///    - Current: SentimentService (Azure AI Text Analytics)
    ///    - Alternative: GoogleSentimentService, MockSentimentAnalyzer (for testing)
    ///    - Purpose: Analyzes text sentiment for reviews and feedback
    /// 
    /// 2. AI Text Generation (IAiTextGenerator PORT):
    ///    - Current: OpenIaGptService (OpenAI GPT API)
    ///    - Alternative: AzureAiTextGeneratorService, MockAiTextGenerator (for testing)
    ///    - Purpose: Generates AI-powered improvement suggestions and content
    /// 
    /// Benefits of this Architecture:
    /// - Domain layer remains pure and independent of external dependencies
    /// - Easy to swap implementations (e.g., switch from OpenAI to Azure OpenAI)
    /// - Simplified testing with mock implementations
    /// - Compliance with SOLID principles (Dependency Inversion, Open/Closed)
    /// - Technology decisions isolated to Infrastructure layer
    /// 
    /// Example Usage in Application Layer:
    /// <code>
    /// public class AiImprovementService
    /// {
    ///     private readonly IAiTextGenerator _aiTextGenerator; // Domain PORT
    ///     
    ///     // Infrastructure ADAPTER (OpenIaGptService) is injected via DI
    ///     public AiImprovementService(IAiTextGenerator aiTextGenerator)
    ///     {
    ///         _aiTextGenerator = aiTextGenerator;
    ///     }
    /// }
    /// </code>
    /// </remarks>
    /// <param name="services">The service collection to which the external service adapters will be added.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddExternalServiceAdapters(this IServiceCollection services)
    {
        // Sentiment Analysis service, implementing the ISentimentAnalyzer PORT from the Domain layer.
        // This is an ADAPTER in Hexagonal Architecture, and an infrastructure service.
        services.AddScoped<ISentimentAnalyzer, SentimentService>();
        // Alternative implementations (uncomment to use):
        // services.AddScoped<ISentimentAnalyzer, GoogleSentimentService>(); // Google
        // services.AddScoped<ISentimentAnalyzer, MockSentimentAnalyzer>(); // Testing

        // AI Text Generation service, implementing the IAiTextGenerator PORT from the Domain layer.
        // This is an ADAPTER in Hexagonal Architecture, and an infrastructure service.
        services.AddScoped<IAiTextGenerator, OpenIaGptService>();
        // Alternative implementations (uncomment to use):
        // services.AddScoped<IAiTextGenerator, AzureAiTextGeneratorService>(); // Azure OpenAI
        // services.AddScoped<IAiTextGenerator, MockAiTextGenerator>(); // Testing

        return services;
    }

    /// <summary>
    /// Adds background job services including hosted services for periodic tasks.
    /// </summary>
    /// <remarks>
    /// Background Jobs:
    /// - ReservationAutoCleanupBackgroundService: Periodically cleans up expired/no-show reservations
    /// 
    /// Configuration:
    /// Background jobs are configured via appsettings.json sections (e.g., "ReservationCleanup").
    /// </remarks>
    /// <param name="services">The service collection to which background services will be added.</param>
    /// <param name="configuration">The application configuration for background job settings.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options from appsettings.json
        services.Configure<ReservationCleanupOptions>(
            configuration.GetSection("ReservationCleanup"));

        // Register background services (IHostedService)
        services.AddHostedService<ReservationAutoCleanupBackgroundService>();

        return services;
    }
}

