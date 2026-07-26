# Tools and Resources Reference

> **Load when:** Defining MCP tool schemas, implementing resource providers, or designing prompt templates.

## Tool Design Principles

### Good Tool Design

Each MCP tool should follow these principles:

| Principle | Description | Example |
|---|---|---|
| **Single purpose** | One tool does one thing | `create-escrow` not `manage-escrow` |
| **Clear naming** | Verb-noun pattern | `get-escrow-status`, `process-payment` |
| **Rich descriptions** | AI must understand when to use it | Include use cases and constraints |
| **Typed parameters** | JSON Schema with constraints | `amountCents: integer, minimum: 1` |
| **Predictable output** | Consistent response format | Always return structured text |
| **Idempotent reads** | GET-like tools have no side effects | `get-escrow` never modifies data |

### Tool Schema Definition

```json
{
    "name": "create-escrow",
    "description": "Creates a new escrow transaction between a buyer and seller. The escrow holds funds until both parties agree to release. Returns the escrow ID and initial status. Use this when a user wants to initiate a new financial transaction with escrow protection.",
    "inputSchema": {
        "type": "object",
        "properties": {
            "buyerId": {
                "type": "string",
                "description": "The buyer's unique account identifier (format: USR-XXXXX)"
            },
            "sellerId": {
                "type": "string",
                "description": "The seller's unique account identifier (format: USR-XXXXX)"
            },
            "amountCents": {
                "type": "integer",
                "minimum": 100,
                "maximum": 100000000,
                "description": "The escrow amount in the smallest currency unit (e.g., cents for USD). Minimum: $1.00"
            },
            "currency": {
                "type": "string",
                "enum": ["USD", "EUR", "GBP"],
                "default": "USD",
                "description": "ISO 4217 currency code"
            },
            "description": {
                "type": "string",
                "maxLength": 500,
                "description": "A brief description of the transaction purpose"
            }
        },
        "required": ["buyerId", "sellerId", "amountCents"]
    }
}
```

### Tool Response Patterns

```csharp
// Success response — structured text
return "Escrow ESC-12345 created successfully.\n" +
       $"- Amount: {amountCents / 100m:C} {currency}\n" +
       $"- Buyer: {buyerId}\n" +
       $"- Seller: {sellerId}\n" +
       $"- Status: Pending\n" +
       $"- Expires: {DateTime.UtcNow.AddDays(30):yyyy-MM-dd}";

// Error response — clear message, no stack traces
return "Cannot create escrow: The buyer account USR-001 has not completed " +
       "identity verification. Please complete KYC verification first.";

// List response — tabular format
return "Active Escrows (3 of 15):\n\n" +
       "| ID | Amount | Status | Created |\n" +
       "|---|---|---|---|\n" +
       "| ESC-001 | $5,000.00 | Active | 2024-01-15 |\n" +
       "| ESC-002 | $12,500.00 | Pending | 2024-01-16 |\n" +
       "| ESC-003 | $750.00 | Disputed | 2024-01-17 |";
```

## Resource Providers

Resources provide read-only data access to AI clients. They use URI-based addressing.

### Resource URI Patterns

| URI Pattern | Description | Example |
|---|---|---|
| `escrow://{id}` | Single escrow details | `escrow://ESC-12345` |
| `schema://{table}` | Database schema | `schema://escrows` |
| `config://{section}` | App configuration | `config://payment-settings` |
| `docs://{topic}` | Documentation | `docs://api-reference` |

### Resource Implementation (.NET)

```csharp
// Static resource — known at startup
[McpServerResourceType]
public sealed class SchemaResources
{
    [McpServerResource(
        Uri = "schema://escrow",
        Name = "Escrow Schema",
        Description = "JSON Schema for the Escrow entity",
        MimeType = "application/schema+json")]
    public Task<string> GetEscrowSchema()
    {
        return Task.FromResult("""
        {
            "$schema": "http://json-schema.org/draft-07/schema#",
            "type": "object",
            "properties": {
                "id": { "type": "string", "pattern": "^ESC-[0-9]+$" },
                "buyerId": { "type": "string" },
                "sellerId": { "type": "string" },
                "amountCents": { "type": "integer", "minimum": 100 },
                "currency": { "type": "string", "enum": ["USD", "EUR", "GBP"] },
                "status": { "type": "string", "enum": ["pending", "active", "completed", "disputed", "cancelled"] },
                "createdAt": { "type": "string", "format": "date-time" }
            },
            "required": ["id", "buyerId", "sellerId", "amountCents", "currency", "status"]
        }
        """);
    }
}
```

### Dynamic Resource Templates

```csharp
// Resource template — URI pattern with parameters
[McpServerResource(
    UriTemplate = "escrow://{escrowId}",
    Name = "Escrow Details",
    Description = "Full details of a specific escrow transaction",
    MimeType = "application/json")]
public async Task<string> GetEscrowResource(string escrowId, CancellationToken ct)
{
    var escrow = await _repository.GetByIdAsync(escrowId, ct);
    if (escrow is null)
        throw new ResourceNotFoundException($"Escrow {escrowId} not found");

    return JsonSerializer.Serialize(escrow, _jsonOptions);
}
```

## Prompt Templates

Prompts provide reusable, parameterized conversation starters.

### Prompt Implementation

```csharp
[McpServerPromptType]
public sealed class EscrowPrompts(IEscrowService escrowService)
{
    [McpServerPrompt(
        Name = "analyze-escrow-risk",
        Description = "Analyzes an escrow transaction for potential risk factors including " +
                      "amount thresholds, party verification, geographic risk, and velocity")]
    public async Task<IEnumerable<McpPromptMessage>> AnalyzeRisk(
        [Description("The escrow ID to analyze")] string escrowId,
        CancellationToken ct = default)
    {
        var escrow = await escrowService.GetByIdAsync(escrowId, ct);

        return
        [
            new McpPromptMessage
            {
                Role = "user",
                Content = new McpContent
                {
                    Type = "text",
                    Text = $"""
                        Analyze this escrow transaction for risk factors:

                        {JsonSerializer.Serialize(escrow, new JsonSerializerOptions { WriteIndented = true })}

                        Evaluate the following risk dimensions:
                        1. **Amount Risk:** Is the amount unusually high for this party's history?
                        2. **Verification Risk:** Are both parties fully KYC verified?
                        3. **Velocity Risk:** How many transactions have these parties initiated recently?
                        4. **Geographic Risk:** Are the parties in high-risk jurisdictions?
                        5. **Pattern Risk:** Does this match known fraud patterns?

                        Provide a risk score (1-10) for each dimension and an overall assessment.
                        """
                }
            }
        ];
    }
}
```

## Input Validation Best Practices

```csharp
[McpServerTool(Name = "transfer-funds")]
public async Task<string> TransferFunds(
    [Description("Source escrow ID")] string escrowId,
    [Description("Amount to transfer in cents")] long amountCents,
    CancellationToken ct = default)
{
    // Validate format
    if (!escrowId.StartsWith("ESC-") || escrowId.Length < 5)
        return "Error: Invalid escrow ID format. Expected: ESC-XXXXX";

    // Validate business rules
    if (amountCents <= 0)
        return "Error: Transfer amount must be positive.";

    if (amountCents > 10_000_000_00) // $10M limit
        return "Error: Transfer amount exceeds the maximum limit of $10,000,000.";

    // Guard against injection — never pass raw input to SQL or commands
    var sanitizedId = Regex.Replace(escrowId, @"[^A-Za-z0-9\-]", "");

    var result = await _service.TransferAsync(sanitizedId, amountCents, ct);
    return $"Transfer of {amountCents / 100m:C} completed for escrow {sanitizedId}.";
}
```
