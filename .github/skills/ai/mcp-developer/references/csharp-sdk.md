# C# MCP SDK Reference

> **Load when:** Building an MCP server or client with .NET/C#.

## Server Implementation

### Setup

```bash
dotnet new console -n EscrowApp.McpServer
cd EscrowApp.McpServer
dotnet add package ModelContextProtocol
dotnet add package Microsoft.Extensions.Hosting
```

### Minimal MCP Server (stdio)

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMcpServer()
    .WithStdioTransport()
    .WithTools<EscrowTools>()
    .WithTools<PaymentTools>();

// Register application services for DI
builder.Services.AddHttpClient("EscrowApi", client =>
{
    client.BaseAddress = new Uri("https://api.nextruzt.io");
});
builder.Services.AddScoped<IEscrowService, EscrowService>();

var app = builder.Build();
await app.RunAsync();
```

### Tool Handler Class

```csharp
using ModelContextProtocol;
using System.ComponentModel;

[McpServerToolType]
public sealed class EscrowTools(IEscrowService escrowService, ILogger<EscrowTools> logger)
{
    [McpServerTool(Name = "create-escrow",
        Description = "Creates a new escrow transaction between a buyer and seller. " +
                      "Returns the escrow ID and confirmation details.")]
    public async Task<string> CreateEscrow(
        [Description("The buyer's unique account identifier")] string buyerId,
        [Description("The seller's unique account identifier")] string sellerId,
        [Description("The escrow amount in the smallest currency unit (e.g., cents)")] long amountCents,
        [Description("ISO 4217 currency code (e.g., USD, EUR, GBP)")] string currency = "USD",
        CancellationToken ct = default)
    {
        logger.LogInformation("Creating escrow: {BuyerId} → {SellerId}, {Amount} {Currency}",
            buyerId, sellerId, amountCents, currency);

        var result = await escrowService.CreateAsync(
            new CreateEscrowRequest(buyerId, sellerId, amountCents, currency), ct);

        return $"Escrow {result.Id} created: {amountCents / 100m:F2} {currency} " +
               $"from {buyerId} to {sellerId}. Status: {result.Status}";
    }

    [McpServerTool(Name = "get-escrow",
        Description = "Retrieves full details of an escrow transaction by ID, " +
                      "including status, amounts, parties, and timeline.")]
    public async Task<string> GetEscrow(
        [Description("The unique escrow transaction identifier (e.g., ESC-12345)")] string escrowId,
        CancellationToken ct = default)
    {
        var escrow = await escrowService.GetByIdAsync(escrowId, ct);
        if (escrow is null)
            return $"Escrow {escrowId} not found.";

        return System.Text.Json.JsonSerializer.Serialize(escrow,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(Name = "list-escrows",
        Description = "Lists escrow transactions with optional filtering by status. " +
                      "Returns up to 50 results per page.")]
    public async Task<string> ListEscrows(
        [Description("Filter by escrow status: pending, active, completed, disputed, cancelled")]
        string? status = null,
        [Description("Page number (1-based)")] int page = 1,
        [Description("Number of results per page (max 50)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        var escrows = await escrowService.ListAsync(status, page, pageSize, ct);
        return System.Text.Json.JsonSerializer.Serialize(escrows,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
```

### Payment Tools (Separate Tool Class)

```csharp
[McpServerToolType]
public sealed class PaymentTools(IPaymentService paymentService)
{
    [McpServerTool(Name = "process-payment",
        Description = "Initiates a payment for a funded escrow. " +
                      "Only works on escrows in 'active' status.")]
    public async Task<string> ProcessPayment(
        [Description("The escrow ID to process payment for")] string escrowId,
        [Description("Payment method: card, bank_transfer, or crypto")] string method = "card",
        CancellationToken ct = default)
    {
        try
        {
            var result = await paymentService.ProcessAsync(escrowId, method, ct);
            return $"Payment {result.TransactionId} processed for escrow {escrowId}. " +
                   $"Status: {result.Status}";
        }
        catch (InvalidOperationException ex)
        {
            return $"Cannot process payment: {ex.Message}";
        }
    }
}
```

## Integration with Clean Architecture

### Connecting MCP to MediatR

```csharp
[McpServerToolType]
public sealed class MediatRTools(IMediator mediator)
{
    [McpServerTool(Name = "create-escrow")]
    public async Task<string> CreateEscrow(
        [Description("Buyer's account ID")] string buyerId,
        [Description("Seller's account ID")] string sellerId,
        [Description("Amount in cents")] long amountCents,
        CancellationToken ct = default)
    {
        var command = new CreateEscrowCommand(buyerId, sellerId, amountCents);
        var result = await mediator.Send(command, ct);
        return System.Text.Json.JsonSerializer.Serialize(result);
    }

    [McpServerTool(Name = "get-escrow")]
    public async Task<string> GetEscrow(
        [Description("Escrow ID")] string escrowId,
        CancellationToken ct = default)
    {
        var query = new GetEscrowQuery(escrowId);
        var result = await mediator.Send(query, ct);
        return result is not null
            ? System.Text.Json.JsonSerializer.Serialize(result)
            : $"Escrow {escrowId} not found.";
    }
}
```

## HTTP+SSE Transport

For web-based MCP servers:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<EscrowTools>();

var app = builder.Build();

app.MapMcp(); // Maps /mcp/sse and /mcp/message endpoints

app.Run();
```

## Client Implementation

### Connecting to an MCP Server

```csharp
using ModelContextProtocol;
using ModelContextProtocol.Client;

// Connect to a stdio-based MCP server
await using var client = await McpClientFactory.CreateAsync(
    new McpClientOptions
    {
        ClientInfo = new McpImplementation { Name = "escrow-client", Version = "1.0.0" }
    },
    new StdioClientTransportOptions
    {
        Command = "dotnet",
        Arguments = ["run", "--project", "src/EscrowApp.McpServer"]
    });

// List available tools
var tools = await client.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"Tool: {tool.Name} — {tool.Description}");
}

// Call a tool
var result = await client.CallToolAsync("create-escrow", new Dictionary<string, object?>
{
    ["buyerId"] = "USR-001",
    ["sellerId"] = "USR-002",
    ["amountCents"] = 500000L,
    ["currency"] = "USD"
});

Console.WriteLine(result.Content.First().Text);
```

## Client Configuration

### mcp-config.json for .NET Servers

```json
{
  "mcpServers": {
    "escrow-tools": {
      "command": "dotnet",
      "args": ["run", "--project", "src/EscrowApp.McpServer"],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ConnectionStrings__Default": "Host=localhost;Database=escrow"
      }
    }
  }
}
```

### Publishing as a dotnet tool

```xml
<!-- EscrowApp.McpServer.csproj -->
<PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>escrow-mcp</ToolCommandName>
</PropertyGroup>
```

```bash
# Install and use as a global tool
dotnet pack
dotnet tool install --global --add-source ./nupkg EscrowApp.McpServer

# Client configuration with global tool
# { "command": "escrow-mcp", "args": [] }
```

## Error Handling Patterns

```csharp
[McpServerTool(Name = "risky-operation")]
public async Task<string> RiskyOperation(
    [Description("Input parameter")] string input,
    CancellationToken ct = default)
{
    // Validate input before processing
    if (string.IsNullOrWhiteSpace(input))
        return "Error: Input parameter is required and cannot be empty.";

    try
    {
        var result = await _service.ProcessAsync(input, ct);
        return System.Text.Json.JsonSerializer.Serialize(result);
    }
    catch (NotFoundException)
    {
        return $"Resource '{input}' not found.";
    }
    catch (UnauthorizedAccessException)
    {
        return "Error: Insufficient permissions to perform this operation.";
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        logger.LogError(ex, "Unexpected error in MCP tool {Tool}", nameof(RiskyOperation));
        return "An unexpected error occurred. Please try again or contact support.";
    }
}
```
