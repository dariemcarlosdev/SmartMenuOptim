using Sentry;
using SmartMenuOptim.API.Data;
using System.Threading.Tasks;

namespace SmartMenuOptim.API.Extensions;

/// <summary>
/// Extension methods for configuring the web application's request pipeline.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the HTTP request pipeline (middleware) for the application.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication ConfigureHtPipeline(this WebApplication app)
    {
        // SWAGGER CONFIGURATION FIXES:
        // 1. Middleware Order Fix:
        //    - Moved UseSwagger() and UseSwaggerUI() to the beginning of the pipeline
        //    - This ensures Swagger middleware is registered before routing
        //    - Prevents potential middleware conflicts
        // 
        // 2. Configuration Changes:
        //    - Removed environment check (if (app.Environment.IsDevelopment()))
        //    - Swagger is now always available, which helps with API testing
        //    - Added explicit SwaggerEndpoint configuration
        //    - Set RoutePrefix to "swagger" for consistent access
        // 
        // 3. Access URLs:
        //    - Swagger UI will be available at:
        //      * HTTP:  http://localhost:5000/swagger
        //      * HTTPS: https://localhost:7119/swagger
        // 
        // 4. Pipeline Order:
        //    - UseSwagger()
        //    - UseSwaggerUI()
        //    - UseMiddleware<ExceptionHandlingMiddleware>
        //    - UseRouting()
        //    - Other middleware...
        // 
        // Note: If you want to restrict Swagger to development only,
        // wrap the Swagger middleware in:
        // if (app.Environment.IsDevelopment())
        // {
        //     app.UseSwagger();
        //     app.UseSwaggerUI(...);
        // }

        // Move Swagger before routing to ensure proper middleware order
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartMenuOptim API V1");
            options.RoutePrefix = "swagger";
        });

        // Add rate limiting early in the pipeline
        app.UseRateLimiter();

        // Adds endpoint routing to the middleware pipeline. This is necessary for matching requests to endpoints.
        app.UseRouting();
        
        // Integrates Sentry's performance tracing to monitor and trace requests.
        app.UseSentryTracing();

        // Exposes a health check endpoint at /health, which can be used by monitoring services.
        app.MapHealthChecks("/health").AllowAnonymous();

       // The name of the CORS policy to be used.
        const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        
        // Adds the CORS middleware to the pipeline to allow cross-origin requests from configured origins.
        // It's important to place this before UseAuthentication and UseAuthorization.
        app.UseCors(MyAllowSpecificOrigins); 
        
        // Redirects HTTP requests to HTTPS, enhancing security.
        app.UseHttpsRedirection();
        
        // Adds the authorization middleware to enforce authorization policies.
        app.UseAuthorization();
        
        // Maps controller actions to endpoints, enabling them to handle requests.
        app.MapControllers();

        return app;
    }

    /// <summary>
    /// Initializes the application's database by seeding it with initial data asynchronously.
    /// </summary>
    /// <remarks>Call this method during application startup to ensure the database contains required initial
    /// data before handling requests.</remarks>
    /// <param name="app">The <see cref="WebApplication"/> instance whose database will be seeded. Must not be null.</param>
    /// <returns>A task that represents the asynchronous database initialization operation.</returns>
    public static async Task InitializeDataBaseAsync(this WebApplication app)
    {        // Seeds the database with initial data when the application starts.
        await DbSeeder.SeedAsync(app.Services);
    }
}
