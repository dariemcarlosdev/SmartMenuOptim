
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder; // Add this using directive  
using Microsoft.AspNetCore.Hosting; // Add this using directive  
using Microsoft.Extensions.Hosting;
using SmartMenuOptim.Shared.Data.Repositories;
using SmartMenuOptim.Shared.Data.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using SmartMenuOptim.API.Services;
using SmartMenuOptim.API.Services.Interfaces;
using SmartMenuOptim.Shared.Data.Context;
using SmartMenuOptim.API.Data; // Add this using directive  

namespace SmartMenuOptim.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
        builder.WebHost.UseUrls($"http://*:{port}");


        // Clear default config sources
        builder.Configuration.Sources.Clear();

        // ✅ Dual source configuration logic
        var environment = builder.Environment;

        if (environment.IsDevelopment() && Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
        {
            Console.WriteLine("Loading configuration from User Secrets and Environment Variables (Development mode)");

            // Running in local development environment (not in Docker) load secrets from user secrets.
            builder.Configuration
                .AddUserSecrets<Program>(optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
        }
        else if (Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null) // Running in Azure App Service
        {
            Console.WriteLine("Loading configuration from Azure App Service environment variables");
            // Running in Azure App Service, load configuration from environment variables
            builder.Configuration
                .AddEnvironmentVariables(); // Use App Settings from Azure
        }
        else
        {
            Console.WriteLine("Loading configuration from /app/secrets.json and Environment Variables (Docker/Other)");
            
            // For Docker containers or other hosting scenarios
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("/app/secrets.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
        }

        //Implement rate limiting and throttling
        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("FixedPolicy", policy =>
            {
                policy.Window = TimeSpan.FromMinutes(1);
                policy.PermitLimit = 100; // Allow 100 requests per minute
                policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                policy.QueueLimit = 10; // Allow up to 10 requests in the queue
            });
        });

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection string is missing!")));

        // Registering the UnityOfWork service
        builder.Services.AddScoped<IUnityOfWork, UnityOfWork>();
        // Registering the Repository service
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        // Registering the SentimentService
        builder.Services.AddScoped<ISentimentService, SentimentService>();
        // Registering the AiImprovementService
        builder.Services.AddScoped<IAiImprovementStrategyService, AiImprovementService>();
        // Registering the OpenIaGptService
        builder.Services.AddScoped<IOpenIAGptService, OpenIaGptService>();

        // Add CORS policy to allow cross-origin requests from the frontend
        var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: MyAllowSpecificOrigins,
                policy =>
                {
                    policy.WithOrigins(
                        "https://localhost:7060",
                        "https://smartmenu-server.azurewebsites.net/" // This is the Azure App Service URL for the frontend app. Include it if you want to allow requests from the deployed frontend.
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                });
        });

        var app = builder.Build();

        // Seeding the database with initial data
        DbSeeder.Seed(app);

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseCors(MyAllowSpecificOrigins);
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}


