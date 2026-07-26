---
name: mcp-developer
description: "Builds, debugs, and extends MCP (Model Context Protocol) servers and clients. Implements tool handlers, resource providers, transport layers (stdio/HTTP/SSE), validates schemas."
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: api-architecture
  triggers: MCP, Model Context Protocol, MCP server, MCP client, AI tools, JSON-RPC
  role: specialist
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: code
  related-skills: prompt-engineer, api-documenter, dotnet-core-expert
---

# MCP Developer

A Model Context Protocol specialist that designs, implements, and debugs MCP servers and clients — tool handlers, resource providers, prompt templates, transport layers, and schema validation for AI-integrated .NET applications.

## When to Use This Skill

- Building a new MCP server to expose application capabilities to AI agents
- Implementing MCP tool handlers that wrap existing .NET services
- Creating resource providers for dynamic data (database records, file contents, API responses)
- Debugging MCP protocol issues (JSON-RPC errors, transport failures, schema validation)
- Adding MCP client capabilities to a .NET application for consuming AI tools
- Designing prompt templates for structured AI interactions
- Configuring transport layers (stdio for CLI tools, HTTP/SSE for web services)
- Testing MCP servers with the MCP Inspector or custom test harnesses

## Reference Guide

| Topic | Reference | Load When |
|---|---|---|
| Protocol Specification | `references/protocol.md` | JSON-RPC 2.0, message types, lifecycle |
| TypeScript SDK | `references/typescript-sdk.md` | Node.js MCP server/client implementation |
| C# SDK | `references/csharp-sdk.md` | .NET MCP server/client implementation |
| Tools & Resources | `references/tools-and-resources.md` | Tool definitions, resource providers, schemas |
| Testing & Debugging | `references/testing-debugging.md` | MCP Inspector, protocol compliance |

## Core Workflow

### Step 1 — Define the MCP Surface

Determine what capabilities the server will expose.

1. **Inventory capabilities** — List the operations, data sources, and prompts the AI should access.
2. **Categorize as Tools, Resources, or Prompts:**
   - **Tools** — Actions with side effects or computation: `CreateEscrow`, `ProcessPayment`, `RunQuery`.
   - **Resources** — Read-only data access: `escrow://{id}`, `config://app-settings`, `schema://database`.
   - **Prompts** — Reusable prompt templates with parameters: `analyze-escrow`, `generate-report`.
3. **Design schemas** — Define JSON Schema for each tool's input parameters and return types.
4. **Plan authorization** — Determine which tools require authentication and what scopes they need.

**✅ Validation checkpoint:** Capability inventory complete. Each item classified as Tool, Resource, or Prompt with schema.

### Step 2 — Implement the MCP Server

Build the server using the appropriate SDK.

1. **Choose transport** — stdio for CLI/desktop integrations; HTTP+SSE for web services; WebSocket for bidirectional streaming.
2. **Register capabilities** — Implement tool handlers, resource providers, and prompt templates.
3. **Add input validation** — Validate all tool inputs against their JSON Schema before execution.
4. **Implement error handling** — Return proper JSON-RPC error codes (InvalidParams, InternalError, MethodNotFound).
5. **Add logging** — Log all requests, responses, and errors with correlation IDs for debugging.

**✅ Validation checkpoint:** Server starts, responds to `initialize`, and lists capabilities via `tools/list`.

### Step 3 — Implement Tool Handlers

Build the business logic behind each tool.

1. **Map to existing services** — MCP tools should delegate to existing application services, not duplicate logic.
2. **Handle cancellation** — Respect `CancellationToken` and the MCP cancellation notification.
3. **Return structured results** — Use the MCP content types (text, image, resource) for responses.
4. **Implement progress reporting** — For long-running operations, send progress notifications.
5. **Guard against injection** — Validate and sanitize all AI-provided inputs before passing to services.

**✅ Validation checkpoint:** Each tool handler executes correctly with valid input and returns proper errors for invalid input.

### Step 4 — Test and Validate

Verify protocol compliance and functional correctness.

1. **Use MCP Inspector** — Run the official inspector tool to validate protocol compliance.
2. **Write integration tests** — Test each tool with valid, invalid, and edge-case inputs.
3. **Test with a real client** — Connect Claude, Copilot, or another MCP client and verify end-to-end.
4. **Load test** — Verify the server handles concurrent requests without deadlocks or resource leaks.
5. **Validate schemas** — Ensure all JSON Schemas are valid and match the actual input/output types.

**✅ Validation checkpoint:** MCP Inspector passes. Integration tests pass. Real client interaction works.

### Step 5 — Deploy and Configure

Package the server for distribution and configure clients.

1. **Package the server** — As a dotnet tool, Docker container, or npm package depending on transport.
2. **Write client configuration** — Generate `mcp-config.json` entries for Copilot CLI, Claude Desktop, etc.
3. **Document capabilities** — Write a capability manifest describing each tool, resource, and prompt.
4. **Set up monitoring** — Track tool invocation counts, latency, and error rates.

**✅ Validation checkpoint:** Server deploys successfully. Client configuration works. Monitoring shows tool usage.

## Quick Reference

### .NET MCP Server with Tool Handler

```csharp
// Program.cs — MCP Server with stdio transport
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMcpServer()
    .WithStdioTransport()
    .WithTools<EscrowTools>();

var app = builder.Build();
await app.RunAsync();

// EscrowTools.cs — Tool handler implementation
[McpServerToolType]
public sealed class EscrowTools(IMediator mediator)
{
    [McpServerTool(Name = "create-escrow",
        Description = "Creates a new escrow transaction between buyer and seller")]
    public async Task<string> CreateEscrow(
        [Description("Buyer's account ID")] string buyerId,
        [Description("Seller's account ID")] string sellerId,
        [Description("Escrow amount in cents")] long amountCents,
        [Description("Currency code (ISO 4217)")] string currency = "USD",
        CancellationToken ct = default)
    {
        var command = new CreateEscrowCommand(buyerId, sellerId, amountCents, currency);
        var result = await mediator.Send(command, ct);
        return $"Escrow {result.Id} created: {amountCents / 100m:C} {currency} from {buyerId} to {sellerId}";
    }

    [McpServerTool(Name = "get-escrow-status",
        Description = "Retrieves the current status and details of an escrow transaction")]
    public async Task<string> GetEscrowStatus(
        [Description("The escrow transaction ID")] string escrowId,
        CancellationToken ct = default)
    {
        var query = new GetEscrowQuery(escrowId);
        var escrow = await mediator.Send(query, ct);
        return System.Text.Json.JsonSerializer.Serialize(escrow);
    }
}
```

### Client Configuration (mcp-config.json)

```json
{
  "mcpServers": {
    "escrow-tools": {
      "command": "dotnet",
      "args": ["run", "--project", "src/EscrowApp.McpServer"],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ConnectionStrings__DefaultConnection": "Host=localhost;Database=escrow;Username=app"
      }
    }
  }
}
```

## Constraints

### MUST DO

- Validate all tool inputs against JSON Schema before executing business logic
- Return proper JSON-RPC error codes — never swallow errors or return 200 with error body
- Implement cancellation support via `CancellationToken` for all tool handlers
- Use the official MCP SDKs — do not hand-roll the protocol layer
- Log all tool invocations with correlation IDs for debugging
- Guard against prompt injection — sanitize AI-provided inputs before passing to services
- Document every tool with clear descriptions and parameter documentation
- Test with MCP Inspector before deploying

### MUST NOT

- Do not expose destructive operations (DELETE, DROP) without explicit confirmation mechanisms
- Do not return raw exception details in tool responses — map to user-friendly error messages
- Do not implement custom JSON-RPC parsing — use the SDK's built-in transport
- Do not expose database connection strings, API keys, or secrets through tool responses
- Do not allow unbounded queries — always paginate or limit result sets
- Do not skip schema validation — invalid inputs must be rejected before reaching business logic
- Do not mix transport types in a single server — choose stdio, HTTP+SSE, or WebSocket

## Output Template

```markdown
# MCP Server Specification

**Server Name:** {name}
**Transport:** {stdio | HTTP+SSE | WebSocket}
**SDK:** {TypeScript | C# | Python}
**Version:** {semver}

## Capabilities

### Tools

| Tool Name | Description | Parameters | Returns |
|---|---|---|---|
| `create-escrow` | Creates a new escrow transaction | buyerId, sellerId, amount, currency | Escrow ID and confirmation |
| `get-escrow-status` | Gets escrow details | escrowId | Escrow details JSON |

### Resources

| URI Pattern | Description | MIME Type |
|---|---|---|
| `escrow://{id}` | Escrow transaction details | application/json |
| `schema://escrow` | Escrow JSON Schema | application/schema+json |

### Prompts

| Prompt Name | Description | Arguments |
|---|---|---|
| `analyze-escrow` | Analyzes an escrow for risk factors | escrowId |

## Client Configuration

```json
{configuration object}
```

## Testing Checklist

- [ ] MCP Inspector validation passes
- [ ] All tools respond to valid input
- [ ] All tools reject invalid input with proper error codes
- [ ] Cancellation works for long-running tools
- [ ] Concurrent requests handled correctly
- [ ] No secrets leaked in responses
```

## Integration Notes

### Copilot CLI
Trigger with: `build MCP server`, `create MCP tool`, `debug MCP connection`, `add MCP resource`

### Claude
Include this file in project context. Trigger with: "Build an MCP server for [capabilities]"

### Gemini
Reference via `GEMINI.md` or direct file inclusion. Trigger with: "Create MCP tools for [service]"
