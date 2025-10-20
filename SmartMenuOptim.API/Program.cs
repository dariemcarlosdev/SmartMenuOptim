using Asp.Versioning;
using Microsoft.AspNetCore.Builder; // Add this using directive  
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting; // Add this using directive  
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SmartMenuOptim.API.Data;
using SmartMenuOptim.API.Services;
using SmartMenuOptim.API.Services.Interfaces;
using SmartMenuOptim.Infrastructure.Middlewares;
using SmartMenuOptim.Shared.Data.Context;
using SmartMenuOptim.Shared.Data.Interfaces;
using SmartMenuOptim.Shared.Data.Repositories;
using System.Threading.RateLimiting;
using SmartMenuOptim.API.Extensions;

namespace SmartMenuOptim.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure the web host to listen on a specific port.
        // This is important for containerized environments like Docker or cloud platforms like Azure App Service.
        var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
        builder.WebHost.UseUrls($"http://*:{port}")

          // Integrate Sentry for real-time error tracking and performance monitoring.
          // Sentry helps in identifying, diagnosing, and resolving issues quickly.
          .UseSentry( options => 
          {
              options.Dsn = "https://fa9c287a9080835a56e63569f803c34b@o4510109391454208.ingest.us.sentry.io/4510109651435520";
              options.MaxBreadcrumbs = 50;
              options.Debug = true;
              options.SendDefaultPii = true;
          });



        // Clear default configuration sources to set up a custom configuration strategy.
        builder.Configuration.Sources.Clear();

        // Set up a flexible configuration system that adapts to different environments.
        var environment = builder.Environment;

        // For local development (not in a Docker container), load configuration from user secrets and environment variables.
        // User secrets are a secure way to store sensitive data during development.
        if (environment.IsDevelopment() && Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
        {
            Console.WriteLine("Loading configuration from User Secrets and Environment Variables (Development mode)");

            // Running in local development environment (not in Docker) load secrets from user secrets.
            builder.Configuration
                .AddUserSecrets<Program>(optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
        }
        // When deployed to Azure App Service, configuration is loaded from environment variables,
        // which are managed as App Settings in the Azure portal.
        else if (Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null) // Running in Azure App Service
        {
            Console.WriteLine("Loading configuration from Azure App Service environment variables");
            // Running in Azure App Service, load configuration from environment variables
            builder.Configuration
                .AddEnvironmentVariables(); // Use App Settings from Azure
        }
        // For Docker containers or other hosting environments, load configuration from a JSON file and environment variables.
        // This allows for configuration to be mounted into the container or passed as environment variables.
        else
        {
            Console.WriteLine("Loading configuration from /app/secrets.json and Environment Variables (Docker/Other)");
            
            // For Docker containers or other hosting scenarios
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("/app/secrets.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
        }


        // Add and configure services for the application's dependency injection container.
        // Using extension methods helps to keep the Program.cs file clean and organized.
        builder.Services.AddApiVersioningServices();
        builder.Services.AddDataServices(builder.Configuration);
        builder.Services.AddCustomServices();
        builder.Services.AddCustomCorsPolicy(builder.Configuration);

        // Add services for controllers, API explorer (for Swagger), and Swagger generation.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline using an extension method.
        // This encapsulates middleware registration and keeps the Program.cs file tidy.
        app.ConfigurePipeline();

        // In Program.cs, register the middleware:
        // app.UseMiddleware<TenantResolverMiddleware>();
        // app.UseMiddleware<RateLimittitngMiddleware>();

        // Run the application.
        app.Run();
    }
}


