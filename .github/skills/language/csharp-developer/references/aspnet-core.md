# ASP.NET Core Reference

> **Load when:** Building minimal APIs, configuring middleware, setting up dependency injection, or routing.

## Application Bootstrap

```csharp
var builder = WebApplication.CreateBuilder(args);

// Service registration (order doesn't matter)
builder.Services.AddApplication();          // MediatR, validators
builder.Services.AddInfrastructure(builder.Configuration); // EF Core, repos
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorization(options => options.AddEscrowPolicies());

var app = builder.Build();

// Middleware pipeline (ORDER MATTERS)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();      // Must come before UseAuthorization
app.UseAuthorization();
app.UseExceptionHandler();

// Endpoint mapping
app.MapEscrowEndpoints();
app.MapPaymentEndpoints();
app.MapHealthChecks("/health");

app.Run();
```

## Middleware Pipeline

```
Request ──→ HTTPS Redirect ──→ Authentication ──→ Authorization ──→ Endpoint
                                                                       │
Response ←── Exception Handler ←── CORS ←── Response Compression ←────┘
```

### Custom Middleware

```csharp
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await next(context);
        }
    }
}

// Register: app.UseMiddleware<CorrelationIdMiddleware>();
```

## Dependency Injection Patterns

### Service Lifetimes

| Lifetime | Use For | Example |
|---|---|---|
| `Singleton` | Stateless, thread-safe services | Configuration wrappers, caching |
| `Scoped` | Per-request state, DbContext | Repositories, unit of work |
| `Transient` | Lightweight, stateless operations | Validators, mappers |

### Registration Patterns

```csharp
// Direct registration
services.AddScoped<IEscrowRepository, EscrowRepository>();

// Factory registration (when construction needs logic)
services.AddScoped<IPaymentGateway>(sp =>
{
    var config = sp.GetRequiredService<IOptions<PaymentOptions>>().Value;
    return config.Provider switch
    {
        "stripe" => new StripeGateway(config),
        "paypal" => new PayPalGateway(config),
        _ => throw new InvalidOperationException($"Unknown provider: {config.Provider}")
    };
});

// Keyed services (.NET 8+)
services.AddKeyedScoped<IPaymentGateway, StripeGateway>("stripe");
services.AddKeyedScoped<IPaymentGateway, PayPalGateway>("paypal");

// Usage with keyed DI
public sealed class PaymentService([FromKeyedServices("stripe")] IPaymentGateway gateway)
{ }
```

### Assembly Scanning

```csharp
// Register all validators in an assembly
services.AddValidatorsFromAssembly(typeof(ApplicationMarker).Assembly);

// Register all MediatR handlers
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ApplicationMarker).Assembly));
```

## Global Error Handling

```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (statusCode, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception is not { } ? null : exception.Message,
            Instance = httpContext.Request.Path
        }, ct);

        return true;
    }
}
```

## Route Conventions

```csharp
// RESTful route patterns for escrow API
// GET    /api/v1/escrows           → List escrows (paginated)
// GET    /api/v1/escrows/{id}      → Get escrow by ID
// POST   /api/v1/escrows           → Create escrow
// PUT    /api/v1/escrows/{id}      → Update escrow
// DELETE /api/v1/escrows/{id}      → Cancel escrow
// POST   /api/v1/escrows/{id}/fund → Fund escrow (action)
// POST   /api/v1/escrows/{id}/release → Release escrow (action)
// POST   /api/v1/escrows/{id}/dispute → File dispute (action)
```

## Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", config =>
    {
        config.Window = TimeSpan.FromMinutes(1);
        config.PermitLimit = 100;
        config.QueueLimit = 10;
    });

    options.AddTokenBucketLimiter("escrow-create", config =>
    {
        config.TokenLimit = 10;
        config.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        config.TokensPerPeriod = 2;
    });

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = 429, Title = "Rate limit exceeded" }, ct);
    };
});

app.UseRateLimiter();
group.MapPost("/", CreateEscrow).RequireRateLimiting("escrow-create");
```
