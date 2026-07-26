# Cloud-Native .NET Reference

> **Load when:** Configuring Docker, health checks, configuration management, or .NET Aspire.

## Docker Configuration

### Multi-Stage Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.sln .
COPY src/Domain/*.csproj src/Domain/
COPY src/Application/*.csproj src/Application/
COPY src/Infrastructure/*.csproj src/Infrastructure/
COPY src/Presentation/*.csproj src/Presentation/
RUN dotnet restore

COPY . .
RUN dotnet publish src/Presentation -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Presentation.dll"]
```

### Docker Compose for Development

```yaml
services:
  escrow-api:
    build: .
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=db;Database=EscrowDb;User=sa;Password=${DB_PASSWORD};TrustServerCertificate=True
    depends_on:
      db:
        condition: service_healthy

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${DB_PASSWORD}
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${DB_PASSWORD}" -C -Q "SELECT 1"
      interval: 10s
      retries: 5
```

## Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sqlserver",
        tags: ["db", "ready"])
    .AddCheck<EscrowServiceHealthCheck>("escrow-service", tags: ["ready"])
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### Custom Health Check

```csharp
public sealed class EscrowServiceHealthCheck(
    IEscrowRepository repository) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var canConnect = await repository.CanConnectAsync(ct);
            return canConnect
                ? HealthCheckResult.Healthy("Escrow service is responsive")
                : HealthCheckResult.Degraded("Escrow service slow to respond");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Escrow service is down", ex);
        }
    }
}
```

## Configuration (Options Pattern)

```csharp
// Options class
public sealed class EscrowOptions
{
    public const string SectionName = "Escrow";
    public decimal MaxTransactionAmount { get; init; } = 1_000_000;
    public string[] SupportedCurrencies { get; init; } = ["USD", "EUR", "GBP"];
    public int DisputeWindowDays { get; init; } = 30;
    public TimeSpan AutoReleaseTimeout { get; init; } = TimeSpan.FromDays(14);
}

// Registration
builder.Services.Configure<EscrowOptions>(
    builder.Configuration.GetSection(EscrowOptions.SectionName));

// Validation at startup
builder.Services.AddOptions<EscrowOptions>()
    .Bind(builder.Configuration.GetSection(EscrowOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Usage via DI
public sealed class EscrowService(IOptions<EscrowOptions> options)
{
    private readonly EscrowOptions _options = options.Value;
    
    public bool IsAmountValid(decimal amount) =>
        amount > 0 && amount <= _options.MaxTransactionAmount;
}
```

## .NET Aspire (Service Defaults)

```csharp
// AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .AddDatabase("escrowdb");

var api = builder.AddProject<Projects.EscrowApp>("escrow-api")
    .WithReference(sql)
    .WithExternalHttpEndpoints();

builder.Build().Run();

// Service Defaults (shared across services)
builder.AddServiceDefaults(); // Adds: health checks, OpenTelemetry, service discovery
```

## Resilience with Polly

```csharp
builder.Services.AddHttpClient<PaymentGatewayClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentGateway:BaseUrl"]!);
})
.AddResilienceHandler("payment-pipeline", builder =>
{
    builder
        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .HandleResult(r => r.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(15)
        })
        .AddTimeout(TimeSpan.FromSeconds(10));
});
```

## Logging & Telemetry

```csharp
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation());
```
