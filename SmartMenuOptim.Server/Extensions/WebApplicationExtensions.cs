using SmartMenuOptim.Server.Components;

namespace SmartMenuOptim.Server.Extensions;

/// <summary>
/// Provides extension methods for configuring the web application's request pipeline.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the HTTP request pipeline (middleware) for the application.
    /// </summary>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        
        // Use the rate limiting middleware
        app.UseRateLimiter();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }
}