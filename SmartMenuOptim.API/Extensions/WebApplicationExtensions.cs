using Sentry;
using SmartMenuOptim.API.Data;
using SmartMenuOptim.Infrastructure.Infrastructure.Middlewares;
using System.Threading.Tasks;

namespace SmartMenuOptim.API.Extensions;

/// <summary>
/// Extension methods for configuring the web application's request pipeline.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the HTTP request processing pipeline for the application, including middleware for Swagger, rate
    /// limiting, routing, health checks, CORS, HTTPS redirection, authorization, and controller mapping.
    /// </summary>
    /// <remarks>
    /// <para><strong>MIDDLEWARE PIPELINE ORDER (registered in sequence):</strong></para>
    /// <list type="number">
    ///     <item><strong>GlobalExceptionHandlingMiddleware</strong> - Catches all unhandled exceptions (must be FIRST in pipeline)</item>
    ///     <item><strong>UseSwagger()</strong> - Serves Swagger JSON specification</item>
    ///     <item><strong>UseSwaggerUI()</strong> - Serves Swagger interactive UI at /swagger</item>
    ///     <item><strong>UseRateLimiter()</strong> - Enforces rate limiting policies</item>
    ///     <item><strong>UseRouting()</strong> - Matches requests to endpoints</item>
    ///     <item><strong>UseSentryTracing()</strong> - Performance monitoring and request tracing</item>
    ///     <item><strong>MapHealthChecks("/health")</strong> - Exposes health check endpoint</item>
    ///     <item><strong>UseCors()</strong> - Enables Cross-Origin Resource Sharing</item>
    ///     <item><strong>UseHttpsRedirection()</strong> - Redirects HTTP to HTTPS</item>
    ///     <item><strong>UseAuthorization()</strong> - Enforces authorization policies</item>
    ///     <item><strong>MapControllers()</strong> - Maps controller endpoints</item>
    /// </list>
    /// 
    /// <para><strong>COMPLETE PIPELINE VISUALIZATION:</strong></para>
    /// <code>
    /// ┌─────────────────────────────────────────────────────────────────────────┐
    /// │  HTTP REQUEST PIPELINE (in order of execution)                          │
    /// ├─────────────────────────────────────────────────────────────────────────┤
    /// │                                                                         │
    /// │  Request ──► GlobalExceptionHandlingMiddleware                          │
    /// │                 │                                                       │
    /// │                 ▼                                                       │
    /// │              UseSwagger() ──► UseSwaggerUI()                            │
    /// │                 │                                                       │
    /// │                 ▼                                                       │
    /// │              UseRateLimiter()                                           │
    /// │                 │                                                       │
    /// │                 ▼                                                       │
    /// │              UseRouting()                                               │
    /// │                 │                                                       │
    /// │                 ▼                                                       │
    /// │              UseSentryTracing()                                        │
    /// │                 │                                                       │
    /// │                 ▼                                                       │
    /// │              MapHealthChecks("/health")                                │
    /// │                 │                                                       │
    /// │                 ▼                                                       │
    /// │              UseCors("_myAllowSpecificOrigins")                        │
    /// │                 │                                                       │
    /// │                 ▼                                                       │
    /// │              UseHttpsRedirection()                                     │
    /// │                 │                                                       │
    /// │                 ▼                                                       │
    /// │              UseAuthorization()                                        │
    /// │                 │                                                       │
    /// │                 ▼                                                       │
    /// │              MapControllers() ──► Controller Action                    │
    /// │                                                                         │
    /// └─────────────────────────────────────────────────────────────────────────┘
    /// </code>
    /// 
    /// <para><strong>ACCESS URLs:</strong></para>
    /// <list type="bullet">
    ///     <item>Swagger UI: http://localhost:5000/swagger or https://localhost:7119/swagger</item>
    ///     <item>Health Check: http://localhost:5000/health</item>
    /// </list>
    /// 
    /// <para><strong>IMPORTANT NOTES:</strong></para>
    /// <list type="bullet">
    ///     <item>GlobalExceptionHandlingMiddleware is registered FIRST in this method to catch all downstream exceptions</item>
    ///     <item>Swagger is always available (not restricted to Development environment)</item>
    ///     <item>CORS must be placed before UseAuthentication/UseAuthorization</item>
    ///     <item>UseRouting() must come before endpoint-aware middleware</item>
    /// </list>
    /// </remarks>
    /// <param name="app">The <see cref="WebApplication"/> instance to configure.</param>
    /// <returns>The configured <see cref="WebApplication"/> instance.</returns>
    public static WebApplication ConfigureHtPipeline(this WebApplication app)
    {
        // ═══════════════════════════════════════════════════════════════════════
        // MIDDLEWARE REGISTRATION ORDER (see XML docs for complete pipeline diagram)
        // ═══════════════════════════════════════════════════════════════════════

        // 1. GLOBAL EXCEPTION HANDLING - Must be first to catch all downstream exceptions
        // Handles DomainException (422), EntityNotFoundException (404), and other exception types
        // See: SmartMenuOptim.Domain/docs/08-Exceptions/DOMAIN_EXCEPTION_HANDLING.md
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

        // 2. SWAGGER - API documentation (always available, not just in Development)
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartMenuOptim API V1");
            options.RoutePrefix = "swagger";
        });

        // 3. RATE LIMITING - Throttle requests early in pipeline
        app.UseRateLimiter();

        // 4. ROUTING - Match requests to endpoints (required before endpoint-aware middleware)
        app.UseRouting();

        // 5. SENTRY TRACING - Performance monitoring and distributed tracing
        app.UseSentryTracing();

        // 6. HEALTH CHECKS - Expose /health endpoint for monitoring services
        app.MapHealthChecks("/health").AllowAnonymous();

        // 7. CORS - Cross-Origin Resource Sharing (must be before Auth middleware)
        const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        app.UseCors(MyAllowSpecificOrigins); 

        // 8. HTTPS REDIRECTION - Redirect HTTP to HTTPS for security
        app.UseHttpsRedirection();

        // 9. AUTHORIZATION - Enforce authorization policies
        app.UseAuthorization();

        // 10. CONTROLLERS - Map controller actions to endpoints
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
