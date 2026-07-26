# Integration Testing — WebApplicationFactory, TestContainers

## Purpose

Provide patterns for integration tests that verify real component interaction using WebApplicationFactory for API tests and TestContainers for database tests.

## WebApplicationFactory — API Integration Tests

### Base Test Setup

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

public sealed class EscrowApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public EscrowApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real database with in-memory for testing
                services.RemoveAll<DbContextOptions<EscrowDbContext>>();
                services.AddDbContext<EscrowDbContext>(options =>
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

                // Replace external services with test doubles
                services.RemoveAll<IPaymentGateway>();
                services.AddScoped<IPaymentGateway, FakePaymentGateway>();
            });
        });
        _client = _factory.CreateClient();
    }
}
```

### API Endpoint Tests

```csharp
[Fact]
public async Task CreateEscrow_WhenValidRequest_ShouldReturn201()
{
    // Arrange
    var request = new CreateEscrowRequest
    {
        BuyerId = Guid.NewGuid(),
        SellerId = Guid.NewGuid(),
        Amount = 5000m,
        Currency = "USD"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/escrow", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var created = await response.Content.ReadFromJsonAsync<EscrowDto>();
    created.Should().NotBeNull();
    created!.Amount.Should().Be(5000m);
    created.Status.Should().Be("Pending");
}

[Fact]
public async Task GetEscrow_WhenNotAuthenticated_ShouldReturn401()
{
    // Arrange — no auth token
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync($"/api/escrow/{Guid.NewGuid()}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

## TestContainers — Real Database Tests

### Setup with SQL Server Container

```csharp
using Testcontainers.MsSql;

public sealed class EscrowDatabaseTests : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private EscrowDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<EscrowDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        _context = new EscrowDbContext(options);
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task Repository_WhenAddingEscrow_ShouldPersistToDatabase()
    {
        // Arrange
        var repo = new EscrowRepository(_context);
        var escrow = EscrowTransaction.Create(
            UserId.New(), UserId.New(), Money.From(1000m));

        // Act
        await repo.AddAsync(escrow, CancellationToken.None);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await repo.GetByIdAsync(escrow.Id, CancellationToken.None);
        retrieved.Should().NotBeNull();
        retrieved!.Amount.Should().Be(Money.From(1000m));
    }
}
```

## Custom WebApplicationFactory

For shared test configuration across multiple test classes:

```csharp
public sealed class EscrowAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<EscrowDbContext>>();
            services.AddDbContext<EscrowDbContext>(o =>
                o.UseSqlServer(_dbContainer.GetConnectionString()));
        });
    }

    public async Task InitializeAsync() => await _dbContainer.StartAsync();
    public new async Task DisposeAsync() => await _dbContainer.DisposeAsync();
}
```

## Test Categories

Use traits to categorize and selectively run tests:

```csharp
[Trait("Category", "Integration")]
[Trait("Category", "Database")]
public sealed class EscrowRepositoryIntegrationTests { }
```

```bash
# Run only unit tests (fast feedback)
dotnet test --filter "Category!=Integration"

# Run only integration tests (CI pipeline)
dotnet test --filter "Category=Integration"
```

## Best Practices

- **Isolate state:** Each test gets its own database or transaction scope
- **Test real behaviors:** Integration tests should verify actual SQL, HTTP, serialization
- **Use `IAsyncLifetime`:** For async setup/teardown with containers
- **Seed test data:** Create a `TestDataSeeder` for common scenarios
- **Don't mock in integration tests:** The whole point is testing real interactions
