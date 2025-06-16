
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Shared.Data;
using Microsoft.AspNetCore.Builder; // Add this using directive  
using Microsoft.AspNetCore.Hosting; // Add this using directive  
using Microsoft.Extensions.Hosting;
using SmartMenuOptim.Shared.Data.Repositories;
using SmartMenuOptim.Shared.Data.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting; // Add this using directive  

namespace SmartMenuOptim.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Registering the UnityOfWork service
        builder.Services.AddScoped<IUnityOfWork, UnityOfWork>();
        // Registering the Repository service
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Add CORS policy to allow cross-origin requests from the frontend
        var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: MyAllowSpecificOrigins,
                policy =>
                {
                    policy.WithOrigins(
                        "https://localhost:7060",
                        "https://your-frontend.com"
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


