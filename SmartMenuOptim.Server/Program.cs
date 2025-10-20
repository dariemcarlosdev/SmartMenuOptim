using SmartMenuOptim.Server.Extensions;

namespace SmartMenuOptim.Server;

/// <summary>
/// The main entry point for the SmartMenuOptim Server application.
/// This class is responsible for configuring and running the web application.
/// </summary>
public class Program
{
    /// <summary>
    /// The main method that configures and starts the web server.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static void Main(string[] args)
    {
        // 1. Initialize the WebApplication builder. This sets up the application's configuration,
        // services, and logging.
        var builder = WebApplication.CreateBuilder(args);

        // 2. Configure Azure Key Vault as a configuration source.
        // This extension method loads secrets from Azure Key Vault, making them available
        // throughout the application's configuration.
        builder.AddKeyVaultConfiguration();
        
        // 3. Register all necessary services with the dependency injection container
        // using custom extension methods for better organization.
        
        // Registers UI-specific services like MudBlazor and Razor Components.
        builder.Services.AddUiServices();
        
        // Registers custom application services (e.g., AIService, ReviewService).
        builder.Services.AddAppServices();
        
        // Configures HttpClients, including resilience policies like retries and circuit breakers,
        // for communicating with the backend API.
        builder.Services.AddHttpClients();


        // Sets up rate limiting policies to protect the application from excessive requests.
        builder.Services.AddRateLimiting();

        // 4. Add console logging to the application's logging providers.
        builder.Logging.AddConsole();
        
        // 5. Build the WebApplication instance. This composes the services and middleware pipeline.
        var app = builder.Build();

        // 6. For diagnostic purposes, retrieve configuration and logger after the app is built
        // to log the backend API's base URL. This helps confirm that the configuration is loaded correctly.
        var config = app.Services.GetRequiredService<IConfiguration>();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("BackendAPI BaseUrl from config: {BaseUrl}", config["BackendApi:BaseUrl"]);

        // 7. Configure the HTTP request pipeline using a custom extension method.
        // This encapsulates the setup of all middleware (e.g., exception handling, HSTS, HTTPS redirection).
        app.ConfigurePipeline();

        // 8. Run the application. This starts the web server and begins listening for incoming requests.
        app.Run();
    }
}