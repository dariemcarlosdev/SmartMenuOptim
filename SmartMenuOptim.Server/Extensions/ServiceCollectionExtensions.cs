using Azure.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using MudBlazor.Services;
using Polly;
using SmartMenuOptim.Server.Services;
using SmartMenuOptim.Server.Services.Interfaces;
using System.Threading.RateLimiting;

namespace SmartMenuOptim.Server.Extensions;

/// <summary>
/// Provides extension methods for setting up services in the IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
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
    /// Adds services required for the user interface.
    /// </summary>
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        // This line is required for MudBlazor dialogs, snackbars, etc.
        services.AddMudServices();
        // Add services to the container.
        services.AddRazorComponents().AddInteractiveServerComponents();
        return services;
    }

    /// <summary>
    /// Adds custom application services.
    /// </summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IAIService, AIService>();
        services.AddScoped<ISaleRecordService, SaleRecordService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddLogging();
        return services;
    }

    /// <summary>
    /// Adds and configures the HttpClient for communicating with the backend API, including resilience policies.
    /// </summary>
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        // Add httpClient for external API calls
        // Implement resiliency with Polly. This is to handle transient faults when calling the backend API.
        // Resiliency policies can include retries, circuit breakers, timeouts, etc. The benefit is to improve the stability and reliability of the application when making HTTP calls.
        // Allowing 5 exceptions before breaking the circuit.
        // Retry 3 times with exponential backoff and circuit breaker policy.
        var httpClientBuilder = services.AddHttpClient("BackendAPI", (serviceProvider, client) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var baseUrl = config["BackendApi:BaseUrl"]; // access the BaseUrl from configuration
            client.BaseAddress = new Uri(baseUrl);
        });

        // Circuit breaker is a pattern that prevents an application from performing an operation that's likely to fail. Stop trying to perform the operation for a period of time.
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

    /// <summary>
    /// Adds rate limiting services to protect the application from excessive requests.
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        //Implement rate limiting and throttling mechanism to limit the number of requests a client can make to the API within a specified time period.
        // The benefit of rate limiting is to protect the API from being overwhelmed by too many requests in a short period of time.
        // In the context of Azure App Service, rate limiting can help manage traffic spikes and ensure fair usage among clients.
        // Ensures fair usage, and protects the stability and availability of the service
        // Scopped to SmartMenuOptim.API the benefit is to protect backend resources and maintain performance.
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