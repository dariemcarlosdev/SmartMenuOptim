using Asp.Versioning;
using Microsoft.AspNetCore.Builder; // Add this using directive  
using Microsoft.AspNetCore.Hosting; // Add this using directive  
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SmartMenuOptim.API.Data;
using SmartMenuOptim.API.Services;
using SmartMenuOptim.API.Services.Interfaces;
using SmartMenuOptim.Shared.Data.Context;
using SmartMenuOptim.Shared.Data.Interfaces;
using SmartMenuOptim.Shared.Data.Repositories;
using System.Threading.RateLimiting;

namespace SmartMenuOptim.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Set the application to listen on the port defined in the PORT environment variable (for Azure App Service compatibility)
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
        // The benefit of rate limiting is to protect the API from being overwhelmed by too many requests in a short period of time.
        // In the context of Azure App Service, rate limiting can help manage traffic spikes and ensure fair usage among clients.
        // Scopped to SmartMenuOptim.API the benefit is to protect backend resources and maintain performance.
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

        // Versioning Rest API with URL segment Swagger support

        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new QueryStringApiVersionReader("X-Api-Version"),
                new UrlSegmentApiVersionReader());
        })
            .AddMvc() // this is required for ApiExplorer and controllers
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV"; // e.g., v1, v1.1
                options.SubstituteApiVersionInUrl = true;
            });


        // Health check endpoint
        builder.Services.AddHealthChecks();

        // For improvement use LazyLoadingProxies if needed in the future, this ensure that related entities are loaded automatically when accessed.
        // improves performance by loading only the necessary data.
        //builder.Services.AddDbContext<AppDbContext>(options =>
        //    options.UseLazyLoadingProxies().UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection string is missing!")));

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection string is missing!")));

        // Registering the UnityOfWork service
        builder.Services.AddScoped<IUnityOfWork, UnityOfWork>();
        // Registering both IRepository and IRepositoryWithIncludes using the same Repository implementation
        // •    This allows you to inject either IRepository<T> or IRepositoryWithIncludes<T> anywhere in your application.
        // •    IRepositoryWithIncludes<T> extends IRepository<T>, but some consumers may only require the basic interface, while others need the advanced includes functionality.
        // •	Registering both ensures maximum flexibility and compatibility for all parts of your codebase, including legacy or third-party code that expects the base interface.
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        builder.Services.AddScoped(typeof(IRepositoryWithIncludes<>), typeof(Repository<>));
        // Registering the SentimentService
        builder.Services.AddScoped<ISentimentService, SentimentService>();
        // Registering the AiImprovementService
        builder.Services.AddScoped<IAiImprovementStrategyService, AiImprovementService>();
        // Registering the OpenIaGptService
        builder.Services.AddScoped<IOpenIAGptService, OpenIaGptService>();

        // Add CORS policy to allow cross-origin requests from the frontend
        // CORS policy is scoped to SmartMenuOptim.API to allow requests from the frontend application.
        // CORS policies are essential for web applications that interact with APIs hosted on different domains or ports.
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

        // Use the rate limiting middleware
        app.UseRateLimiter();

        // Health check endpoint at /health
        app.MapHealthChecks("/health").AllowAnonymous();

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

        // In Program.cs, register the middleware:
        // app.UseMiddleware<TenantResolverMiddleware>();
        // app.UseMiddleware<RateLimittitngMiddleware>();

        app.Run();
    }
}


