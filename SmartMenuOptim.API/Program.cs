
using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.Shared.Data;
using Microsoft.AspNetCore.Builder; // Add this using directive  
using Microsoft.AspNetCore.Hosting; // Add this using directive  
using Microsoft.Extensions.Hosting; // Add this using directive  

namespace SmartMenuOptim.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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


