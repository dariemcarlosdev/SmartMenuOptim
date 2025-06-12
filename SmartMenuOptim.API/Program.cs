using Microsoft.EntityFrameworkCore;
using SmartMenuOptim.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// register the DbContext with dependency injection
// PostgreSQL database context configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add CORS policy to allow cross-origin requests from the frontend
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// Configure CORS to allow specific origins
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        
        //for quickly allowing all origins, methods, and headers, you can use AllowAnyOrigin, AllowAnyMethod, and AllowAnyHeader
        //policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        
        policy =>
        {
            policy.WithOrigins(
                // Allow specific origins for CORS requests from the frontend
                "https://localhost:7060",  // localhost for the frontend development server
                "https://your-frontend.com"   // // Production frontend domain
                )
                .AllowAnyMethod()
                .AllowAnyHeader();
            // If you want to allow credentials (cookies, authorization headers, etc.), uncomment the line below
            //forward-thinking for production, you might want to restrict origins to specific domains
            // .WithOrigins("https://your-production-domain.com")
            // .WithExposedHeaders("Content-Disposition") // for file downloads
            // .AllowCredentials(); // Allow credentials if needed

        });
});



var app = builder.Build();

//seeding the database with initial data
DbSeeder.Seed(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Enable CORS middleware before any authorization/auth middleware
app.UseCors(MyAllowSpecificOrigins);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
