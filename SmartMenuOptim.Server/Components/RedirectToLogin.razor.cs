using Microsoft.AspNetCore.Components;

namespace SmartMenuOptim.Server.Components;

public sealed partial class RedirectToLogin : ComponentBase
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IWebHostEnvironment Env { get; set; } = default!;
    [Inject] private ILogger<RedirectToLogin> Logger { get; set; } = default!;

    protected override void OnInitialized()
    {
        var relativePath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

        // Guard the login-loop: don't redirect the login page onto itself.
        if (relativePath.StartsWith("auth/login", StringComparison.OrdinalIgnoreCase))
            return;

        var returnUrl = Uri.EscapeDataString(relativePath);
        var target = $"/auth/login?returnUrl={returnUrl}";

        WriteRedirectLog(relativePath, target);
        NavigationManager.NavigateTo(target, forceLoad: true);
    }

    private void WriteRedirectLog(string from, string target)
    {
        try
        {
            var logDir = Path.Combine(Env.ContentRootPath, "App_Data", "Logs");
            Directory.CreateDirectory(logDir);
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  redirect  from='/{from}'  ->  {target}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(logDir, "auth-redirect.log"), line);
        }
        catch (Exception ex)
        {
            // Never let audit-log IO break the redirect.
            Logger.LogWarning(ex, "Failed writing auth-redirect log");
        }
    }
}
