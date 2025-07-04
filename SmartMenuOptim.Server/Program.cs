using SmartMenuOptim.Server.Components;
using SmartMenuOptim.Server.Services;
using SmartMenuOptim.Server.Services.Interfaces;

namespace SmartMenuOptim.Server;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddScoped<IAIService, AIService>();
        builder.Services.AddScoped<ISaleRecordService, SaleRecordService>();
        builder.Services.AddScoped<IReviewService, ReviewService>();
        builder.Services.AddScoped<IUnderperformingService, UnderperformingService>();
        builder.Services.AddLogging();

        // Add httpClient for external API calls
        builder.Services.AddHttpClient("BackendAPI", (serviceProvider, client) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var baseUrl = config["BackendApi:BaseUrl"];
            //client.BaseAddress = new Uri("https://localhost:7119/");
            client.BaseAddress = new Uri(baseUrl);
        });

        // Add httpClient for external API calls
        //builder.Services.AddHttpClient("BackendAPI", (serviceProvider, client) =>
        //{
        //    var config = serviceProvider.GetRequiredService<IConfiguration>();
        //    var baseUrl = config["BackendApi:BaseUrl"];
        //    //client.BaseAddress = new Uri("https://localhost:7119/");
        //    client.BaseAddress = new Uri(baseUrl);
        //});

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