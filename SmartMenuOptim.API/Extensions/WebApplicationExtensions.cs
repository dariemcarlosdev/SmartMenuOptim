using Sentry;
using SmartMenuOptim.API.Data;
using SmartMenuOptim.Infrastructure.Middlewares;

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
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Adds a custom middleware for centralized exception handling.
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Adds endpoint routing to the middleware pipeline. This is necessary for matching requests to endpoints.
        app.UseRouting();
        
        // Integrates Sentry's performance tracing to monitor and trace requests.
        app.UseSentryTracing();

        // Exposes a health check endpoint at /health, which can be used by monitoring services.
        app.MapHealthChecks("/health").AllowAnonymous();

        // Seeds the database with initial data when the application starts.
        DbSeeder.Seed(app);

        // Conditionally adds Swagger middleware for API documentation in the development environment.
        // Swagger UI provides an interactive way to explore and test the API.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

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
}
