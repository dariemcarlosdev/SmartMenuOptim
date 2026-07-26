# Swagger Integration — ASP.NET Core (.NET 10)

## Swashbuckle Setup

```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.*" />
<PackageReference Include="Swashbuckle.AspNetCore.Annotations" Version="7.*" />
```

## NSwag Alternative

Use NSwag (`NSwag.AspNetCore 14.*`) when you need typed C# or TypeScript client generation, compile-time doc generation, or ReDoc UI.

## XML Documentation Comments

Enable in `.csproj`: `<GenerateDocumentationFile>true</GenerateDocumentationFile>`

Key XML tags: `<summary>` (description), `<param>` (parameter), `<returns>` (response body), `<response code="201">` (status code mapping), `<remarks>` (extended notes).

```csharp
/// <summary>Creates a new escrow transaction.</summary>
/// <param name="command">Escrow creation parameters.</param>
/// <response code="201">Escrow created successfully.</response>
/// <response code="400">Validation failed — see ProblemDetails.</response>
[HttpPost]
[ProducesResponseType(typeof(EscrowResponse), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<EscrowResponse>> Create(
    [FromBody] CreateEscrowCommand command, CancellationToken ct) { ... }
```

## Custom Operation Filters

```csharp
// Adds X-Idempotency-Key header to decorated endpoints
public sealed class IdempotencyKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!context.MethodInfo.GetCustomAttributes(typeof(RequiresIdempotencyKeyAttribute), true).Any())
            return;
        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Idempotency-Key", In = ParameterLocation.Header,
            Required = true, Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        });
    }
}
```

## API Versioning with Swagger

```csharp
builder.Services.AddApiVersioning(o => {
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
}).AddApiExplorer(o => { o.GroupNameFormat = "'v'VVV"; o.SubstituteApiVersionInUrl = true; });
```

Generate separate Swagger docs per version: `options.SwaggerDoc("v1", ...)` / `options.SwaggerDoc("v2", ...)`.

## Complete Example — Program.cs with JWT Auth

```csharp
using System.Reflection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NexTruzt.io Escrow API", Version = "v1",
        Description = "Fintech escrow transaction management API"
    });

    // XML comments
    var xmlPath = Path.Combine(AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);

    // JWT Bearer auth
    options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        Description = "JWT from Microsoft Entra ID"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference
                { Type = ReferenceType.SecurityScheme, Id = "bearerAuth" } },
            Array.Empty<string>()
        }
    });

    options.OperationFilter<IdempotencyKeyOperationFilter>();
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(ui => {
        ui.SwaggerEndpoint("/swagger/v1/swagger.json", "Escrow API v1");
        ui.RoutePrefix = "swagger";
    });
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```
