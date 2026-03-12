using Azure.Identity;
using Microsoft.Extensions.Http.Resilience;
using MudBlazor.Services;
using Polly;
using SmartMenuOptim.Domain.Extensions;
using SmartMenuOptim.Server.Features.Restaurants.Services;
using SmartMenuOptim.Server.Features.Restaurants.State;
using SmartMenuOptim.Server.Services.Abstractions;
using SmartMenuOptim.Server.Services.ClientServices;
using SmartMenuOptim.Server.State;

namespace SmartMenuOptim.Server.Extensions;

/// <summary>
/// Provides extension methods for setting up services in the IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures all Blazor Server services including UI, application services, HTTP clients, and Azure Key Vault.
    /// This unified method sets up the complete service configuration for the Blazor Server application.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder to configure.</param>
    /// <returns>The configured WebApplicationBuilder.</returns>
    public static WebApplicationBuilder BlazorServerServiceCollection(this WebApplicationBuilder builder)
    {
        // 1. Configure Azure Key Vault as a configuration source
        builder.AddKeyVaultConfiguration();
        
        // 2. Add UI services (MudBlazor, Razor Components)
        builder.Services.AddUiServices();
        
        // 3. Add custom application services and domain services
        builder.Services.AddAppServices();
        
        // 4. Configure HttpClients with resilience patterns
        builder.Services.AddHttpClients();
        
        return builder;
    }
    /// <summary>
    /// Adds Azure Key Vault as a configuration source.
    /// </summary>
    public static WebApplicationBuilder AddKeyVaultConfiguration(this WebApplicationBuilder builder)
    {
        // Add Key Vault configuration( before configuration is built) and then Get Key Vault name from environment/app settings (recommended)
        // This is to ensure that the Key Vault secrets are available before any services are added
        var keyVaultName = builder.Configuration["KeyVaultName"]; // Set this in Azure App Settings
        if (!string.IsNullOrEmpty(keyVaultName))
        {
            var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
            builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
        }
        return builder;
    }

    /// <summary>
    /// Adds UI-related services required for MudBlazor components and interactive Razor components to the specified
    /// service collection.
    /// </summary>
    /// <remarks>This method registers services necessary for MudBlazor dialogs, snackbars, and other UI
    /// features, as well as support for interactive server-side Razor components. Call this method during application
    /// startup to enable these UI capabilities.</remarks>
    /// <param name="services">The service collection to which the UI services will be added. Cannot be null.</param>
    /// <returns>The same instance of <see cref="IServiceCollection"/> that was provided, with UI services registered.</returns>
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        // This line is required for MudBlazor dialogs, snackbars, etc.
        services.AddMudServices();
        // Add services to the container.
        services.AddRazorComponents().AddInteractiveServerComponents();
        return services;
    }

    /// <summary>
    /// Adds application and domain services to the specified service collection for dependency injection.
    /// </summary>
    /// <remarks>This method registers core application services, domain services, and logging with the
    /// dependency injection container. It is intended to be called during application startup to configure required
    /// services.</remarks>
    /// <param name="services">The service collection to which the application and domain services will be added. Cannot be null.</param>
    /// <returns>The same instance of <see cref="IServiceCollection"/> that was provided, to allow for method chaining.</returns>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // UI Services
        services.AddScoped<IAIService, AIService>();
        services.AddScoped<ISaleRecordService, SaleRecordService>();
        services.AddScoped<IReviewService, ReviewService>();

        // Client Services (HTTP-based adapters for API communication)
        services.AddScoped<IRestaurantClientService, RestaurantClientService>();

        // State Containers (Scoped for per-circuit state in Blazor Server)
        services.AddScoped<RestaurantDetailState>();
        services.AddScoped<RestaurantListState>();

        services.AddLogging();
        return services;
    }

    /// <summary>
    /// Adds and configures a named HttpClient for communicating with the backend API, including resilience policies
    /// such as circuit breaker and retry, to the specified service collection.
    /// </summary>
    /// <remarks>The configured HttpClient uses the base address specified by the 'BackendApi:BaseUrl'
    /// configuration setting and sets the 'Accept' header to 'application/json'. Circuit breaker and retry policies are
    /// applied to improve resilience when communicating with the backend API. The circuit breaker temporarily blocks
    /// requests after a threshold of failures, while the retry policy attempts failed requests up to three times with
    /// exponential backoff. This method is intended for use in server-side applications that consume a backend
    /// API.</remarks>
    /// <param name="services">The service collection to which the HttpClient and its resilience policies will be added. Cannot be null.</param>
    /// <returns>The same IServiceCollection instance that was provided, to support method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the required 'BackendApi:BaseUrl' configuration value is missing or empty.</exception>
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        var httpClientBuilder = services.AddHttpClient("BackendAPI", (serviceProvider, client) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var baseUrl = config["BackendApi:BaseUrl"];
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            if (string.IsNullOrEmpty(baseUrl))
            {
                logger.LogError("BackendApi:BaseUrl is not configured");
                throw new InvalidOperationException("BackendApi:BaseUrl configuration is missing");
            }

            try
            {
                client.BaseAddress = new Uri(baseUrl);
                logger.LogInformation("HttpClient configured with BaseAddress: {BaseAddress}", baseUrl);
                
                // Add default headers if needed
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            }
            catch (UriFormatException ex)
            {
                logger.LogError(ex, "Invalid BackendApi:BaseUrl configuration: {BaseUrl}", baseUrl);
                throw;
            }
        });

        /*
         1.	Circuit Breaker & Retry Policy These are client-side resilience patterns that protect the Blazor server when making calls to the API. 
            They handle transient failures and network issues between the Blazor server and the API.
         
            •	These are currently in the Server project because they're configured for the HttpClient that calls the API
            •	This is actually the CORRECT placement because:
            •	The Server (Blazor) project is the consumer of the API
            •	These patterns protect the client-side communication with the API
            •	They handle transient failures between the Blazor server and the API.    
        
         */

        // Circuit breaker is a pattern that prevents an application from performing an operation that's likely to fail.
        // Stop trying to perform the operation for a period of time.
        // Prevent cascading failures and improve the stability of the application. Enable the system to recover more quickly from transient faults.
        // Resilience Circuit breaker implementation using Microsoft.Extensions.Http.Resilience
        // Circuit breaker will break the circuit for 15 seconds if there are 10 requests and 10% or more of them fail.
        httpClientBuilder.AddResilienceHandler("circuit-breaker-pipeline", builder =>
        {
            //Add circuit breaker
            builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions()
            {
                FailureRatio = 0.1, // 10% failure ratio
                MinimumThroughput = 10,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15)
            });
        });

        // Retry is a pattern that allows an application to retry an operation that has failed due to a transient fault.(e.g., 503 Service Unavailable)
        // transient faults occur when there are temporary issues with the network or the service being called.
        // Resileence Retry policy implementation using Microsoft.Extensions.Http.Resilience
        // Retry policy will retry 3 times with exponential backoff starting at 2 seconds and max delay of 10 seconds.
        // Use Exponential backoff means that the delay will increase exponentially with each retry.
        httpClientBuilder.AddResilienceHandler("retry-pipeline", builder =>
        {
            //Add retry policy
            builder.AddRetry(new HttpRetryStrategyOptions()
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                MaxDelay = TimeSpan.FromSeconds(10)
            });
        });

        return services;
    }
}