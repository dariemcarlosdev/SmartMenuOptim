using Azure.Identity;
using MudBlazor.Services;
using SmartMenuOptim.Server.Components;
using SmartMenuOptim.Server.Services;
using SmartMenuOptim.Server.Services.Interfaces;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Azure;


namespace SmartMenuOptim.Server;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Key Vault configuration( before configuration is built) and then Get Key Vault name from environment/app settings (recommended)
        // This is to ensure that the Key Vault secrets are available before any services are added
        var keyVaultName = builder.Configuration["KeyVaultName"]; // Set this in Azure App Settings
        if (!string.IsNullOrEmpty(keyVaultName))
        {
            var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
            builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
        }
        // 
        builder.Services.AddMudServices(); // <-- This line is required for MudBlazor dialogs, snackbars, etc.
        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();


        builder.Services.AddScoped<IAIService, AIService>();
        builder.Services.AddScoped<ISaleRecordService, SaleRecordService>();
        builder.Services.AddScoped<IReviewService, ReviewService>();
        builder.Services.AddLogging();

        // Add httpClient for external API calls
        builder.Services.AddHttpClient("BackendAPI", (serviceProvider, client) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var baseUrl = config["BackendApi:BaseUrl"]; // access the BaseUrl from configuration
            //client.BaseAddress = new Uri("https://localhost:7119/");
            client.BaseAddress = new Uri(baseUrl);
        });


        // Implement resiliency with Polly. This is to handle transient faults when calling the backend API.
        // Resiliency policies can include retries, circuit breakers, timeouts, etc. The benefit is to improve the stability and reliability of the application when making HTTP calls.
        // Allowing 5 exceptions before breaking the circuit.
        // Retry 3 times with exponential backoff and circuit breaker policy.

        // Replace the existing code block for adding HttpClient with the following:
        // Note: I do have to commented out the code below because it was causing issues with the HttpClient registration.


        //builder.Services.AddHttpClient<IAIService, AIService>("BackendAPI")
        //    .AddTransientHttpErrorPolicy(policy =>
        //    policy.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
        //    .SetHandlerLifetime(TimeSpan.FromMinutes(5)); // Set lifetime to 5 minutes




        builder.Logging.AddConsole();
        var app = builder.Build();

        var config = app.Services.GetRequiredService<IConfiguration>();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("BackendAPI BaseUrl from config: {BaseUrl}", config["BackendApi:BaseUrl"]);

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}