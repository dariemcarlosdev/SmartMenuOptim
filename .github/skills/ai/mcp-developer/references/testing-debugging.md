# MCP Testing and Debugging Reference

> **Load when:** Using MCP Inspector, debugging protocol issues, or writing MCP integration tests.

## MCP Inspector

The official tool for testing and debugging MCP servers interactively.

### Installation and Usage

```bash
# Run with npx (no install required)
npx @modelcontextprotocol/inspector

# Launches a web UI at http://localhost:5173

# Connect to a stdio server
# In the Inspector UI:
# - Transport: stdio
# - Command: dotnet
# - Args: run --project src/EscrowApp.McpServer

# Connect to an HTTP+SSE server
# In the Inspector UI:
# - Transport: SSE
# - URL: http://localhost:3001/sse
```

### Inspector Features

| Feature | What It Does | When to Use |
|---|---|---|
| **Initialize** | Sends initialize handshake | Verify server starts correctly |
| **List Tools** | Shows all registered tools | Verify tools are discoverable |
| **Call Tool** | Execute a tool with custom args | Test tool behavior |
| **List Resources** | Shows all registered resources | Verify resource URIs |
| **Read Resource** | Fetch a resource by URI | Test resource providers |
| **List Prompts** | Shows all registered prompts | Verify prompt templates |
| **Get Prompt** | Render a prompt with args | Test prompt generation |
| **Protocol Log** | Shows raw JSON-RPC messages | Debug protocol issues |

### Verifying Protocol Compliance

In the Inspector, verify:

1. ✅ Server responds to `initialize` with correct capabilities
2. ✅ `tools/list` returns all expected tools with schemas
3. ✅ `resources/list` returns all expected resources
4. ✅ Tool calls with valid input return content
5. ✅ Tool calls with invalid input return errors (not crashes)
6. ✅ Cancellation is handled gracefully

## Integration Testing

### Testing MCP Tools with xUnit

```csharp
public sealed class EscrowToolsTests : IAsyncLifetime
{
    private IMcpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = await McpClientFactory.CreateAsync(
            new McpClientOptions
            {
                ClientInfo = new McpImplementation { Name = "test-client", Version = "1.0.0" }
            },
            new StdioClientTransportOptions
            {
                Command = "dotnet",
                Arguments = ["run", "--project", "../src/EscrowApp.McpServer"]
            });
    }

    [Fact]
    public async Task ListTools_ReturnsExpectedTools()
    {
        var tools = await _client.ListToolsAsync();

        Assert.Contains(tools, t => t.Name == "create-escrow");
        Assert.Contains(tools, t => t.Name == "get-escrow");
        Assert.Contains(tools, t => t.Name == "list-escrows");
    }

    [Fact]
    public async Task CreateEscrow_WithValidInput_ReturnsEscrowId()
    {
        var result = await _client.CallToolAsync("create-escrow", new Dictionary<string, object?>
        {
            ["buyerId"] = "USR-TEST-001",
            ["sellerId"] = "USR-TEST-002",
            ["amountCents"] = 500000L,
            ["currency"] = "USD"
        });

        var text = result.Content.First().Text;
        Assert.Contains("ESC-", text);
        Assert.Contains("created", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateEscrow_WithMissingBuyerId_ReturnsError()
    {
        var result = await _client.CallToolAsync("create-escrow", new Dictionary<string, object?>
        {
            ["sellerId"] = "USR-TEST-002",
            ["amountCents"] = 500000L
        });

        // Should return error result, not throw
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task GetEscrow_WithNonExistentId_ReturnsNotFound()
    {
        var result = await _client.CallToolAsync("get-escrow", new Dictionary<string, object?>
        {
            ["escrowId"] = "ESC-NONEXISTENT"
        });

        var text = result.Content.First().Text;
        Assert.Contains("not found", text, StringComparison.OrdinalIgnoreCase);
    }

    public async Task DisposeAsync()
    {
        if (_client is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
    }
}
```

### Testing Resource Providers

```csharp
[Fact]
public async Task ReadResource_EscrowUri_ReturnsJsonContent()
{
    // Arrange — create an escrow first
    await _client.CallToolAsync("create-escrow", new Dictionary<string, object?>
    {
        ["buyerId"] = "USR-TEST-001",
        ["sellerId"] = "USR-TEST-002",
        ["amountCents"] = 100000L
    });

    // Act — read the resource
    var resources = await _client.ListResourcesAsync();
    var escrowResource = resources.FirstOrDefault(r => r.Uri.StartsWith("escrow://"));

    if (escrowResource is not null)
    {
        var content = await _client.ReadResourceAsync(escrowResource.Uri);
        var text = content.Contents.First().Text;

        // Assert — valid JSON with expected fields
        var json = JsonDocument.Parse(text);
        Assert.True(json.RootElement.TryGetProperty("id", out _));
        Assert.True(json.RootElement.TryGetProperty("status", out _));
    }
}
```

## Debugging Common Issues

### Server Won't Start

```bash
# Check if the server binary works standalone
dotnet run --project src/EscrowApp.McpServer

# Common issues:
# 1. Missing dependencies — run 'dotnet restore' first
# 2. Port conflict (HTTP transport) — check if port is in use
# 3. Missing environment variables — check required config
# 4. stdout pollution — ensure nothing writes to stdout except MCP protocol
```

### Tools Not Appearing

```csharp
// Verify tool registration in Program.cs
builder.Services.AddMcpServer()
    .WithStdioTransport()
    .WithTools<EscrowTools>()     // Is this line present?
    .WithTools<PaymentTools>();   // Are ALL tool classes registered?

// Verify tool class has the attribute
[McpServerToolType]  // This attribute is required!
public sealed class EscrowTools { ... }

// Verify tool methods have the attribute
[McpServerTool(Name = "create-escrow")]  // Required on each tool method
public async Task<string> CreateEscrow(...) { ... }
```

### Protocol Errors

| Error | Likely Cause | Fix |
|---|---|---|
| "Parse error (-32700)" | Invalid JSON in response | Check for Console.WriteLine pollution |
| "Method not found (-32601)" | Tool name mismatch | Verify `Name` in `[McpServerTool]` |
| "Invalid params (-32602)" | Schema validation failed | Check parameter types and required fields |
| "Internal error (-32603)" | Unhandled exception | Add try-catch in tool handler |
| Connection drops | Server crashed | Check stderr logs for exceptions |
| Timeout | Tool takes too long | Add CancellationToken support |

### Debugging Protocol Messages

```csharp
// Enable protocol-level logging
builder.Logging.AddFilter("ModelContextProtocol", LogLevel.Debug);

// Or manually log all JSON-RPC messages
builder.Services.AddMcpServer()
    .WithStdioTransport()
    .WithTools<EscrowTools>();

// Stderr is safe for logging (stdout is protocol-only)
Console.Error.WriteLine("Debug: Tool invoked with args...");
```

### Testing with curl (HTTP Transport)

```bash
# Initialize
curl -X POST http://localhost:3001/message \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"curl","version":"1.0.0"}}}'

# List tools
curl -X POST http://localhost:3001/message \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'

# Call a tool
curl -X POST http://localhost:3001/message \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"get-escrow","arguments":{"escrowId":"ESC-12345"}}}'
```

## Performance Testing

```csharp
[Fact]
public async Task ToolCall_UnderLoad_CompletesWithinTimeout()
{
    var tasks = Enumerable.Range(0, 50).Select(async i =>
    {
        var sw = Stopwatch.StartNew();
        var result = await _client.CallToolAsync("list-escrows", new Dictionary<string, object?>
        {
            ["page"] = 1,
            ["pageSize"] = 10
        });
        sw.Stop();
        return sw.ElapsedMilliseconds;
    });

    var durations = await Task.WhenAll(tasks);
    var p99 = durations.OrderBy(d => d).ElementAt((int)(durations.Length * 0.99));

    Assert.True(p99 < 5000, $"P99 latency was {p99}ms, expected < 5000ms");
}
```
