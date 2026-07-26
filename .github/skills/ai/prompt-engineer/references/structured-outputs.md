# Structured Outputs Reference

> **Load when:** Designing JSON output schemas, configuring function calling, or ensuring reliable parsing.

## Output Mode Selection

| Mode | Use When | Reliability | Model Support |
|---|---|---|---|
| JSON Mode | Need valid JSON, schema-free | High | Claude, GPT-4, Gemini |
| JSON Schema | Need specific fields and types | Very High | GPT-4, Claude (via prompt) |
| Function Calling | Need structured tool invocation | Highest | GPT-4, Claude, Gemini |
| XML Mode | Need hierarchical structured data | Medium | Claude (native), others via prompt |
| Markdown | Need human-readable structured output | Medium | All models |

## JSON Schema Design

### Schema for Escrow Risk Assessment

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["risk_level", "risk_score", "factors", "recommendation"],
  "properties": {
    "risk_level": {
      "type": "string",
      "enum": ["low", "medium", "high", "critical"],
      "description": "Overall risk classification"
    },
    "risk_score": {
      "type": "number",
      "minimum": 0,
      "maximum": 100,
      "description": "Numeric risk score (0=safe, 100=critical)"
    },
    "factors": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["name", "severity", "detail"],
        "properties": {
          "name": { "type": "string" },
          "severity": { "type": "string", "enum": ["low", "medium", "high"] },
          "detail": { "type": "string", "maxLength": 200 }
        }
      },
      "minItems": 1,
      "maxItems": 10
    },
    "recommendation": {
      "type": "string",
      "enum": ["approve", "review", "reject", "escalate"]
    }
  },
  "additionalProperties": false
}
```

### Embedding Schema in Prompts

```text
Assess the risk of this escrow transaction.

Respond with ONLY valid JSON matching this exact schema:
{
  "risk_level": "low" | "medium" | "high" | "critical",
  "risk_score": number (0-100),
  "factors": [{"name": string, "severity": "low"|"medium"|"high", "detail": string}],
  "recommendation": "approve" | "review" | "reject" | "escalate"
}

Do not include any text before or after the JSON object.

Transaction:
{transaction_json}
```

## Function Calling Patterns

### Tool Definition for Escrow Platform

```json
{
  "name": "create_escrow",
  "description": "Creates a new escrow transaction between buyer and seller with specified terms",
  "parameters": {
    "type": "object",
    "required": ["buyer_id", "seller_id", "amount", "currency"],
    "properties": {
      "buyer_id": {
        "type": "string",
        "pattern": "^USR-[A-Z0-9]{6}$",
        "description": "Buyer's unique account ID"
      },
      "seller_id": {
        "type": "string",
        "pattern": "^USR-[A-Z0-9]{6}$",
        "description": "Seller's unique account ID"
      },
      "amount": {
        "type": "number",
        "minimum": 0.01,
        "maximum": 1000000,
        "description": "Transaction amount in specified currency"
      },
      "currency": {
        "type": "string",
        "enum": ["USD", "EUR", "GBP"],
        "description": "ISO 4217 currency code"
      },
      "conditions": {
        "type": "array",
        "items": { "type": "string" },
        "description": "Release conditions (optional)",
        "maxItems": 5
      }
    }
  }
}
```

## Parsing Strategies

### Defensive JSON Parsing in C#

```csharp
public sealed class LlmResponseParser
{
    public static Result<T> ParseJson<T>(string rawOutput) where T : class
    {
        // Strip markdown code fences if present
        var json = rawOutput
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();

        // Try parsing
        try
        {
            var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });
            return result is not null
                ? Result<T>.Success(result)
                : Result<T>.Failure("Deserialization returned null");
        }
        catch (JsonException ex)
        {
            return Result<T>.Failure($"JSON parse error: {ex.Message}");
        }
    }
}
```

### Output Validation Pipeline

```
Raw LLM Output
  → Strip markdown fences / whitespace
  → Parse JSON
  → Validate against schema
  → Type-check enum values
  → Range-check numeric fields
  → Return typed result or error
```

## Common Pitfalls

| Pitfall | Symptom | Fix |
|---|---|---|
| No explicit format instruction | Model returns prose instead of JSON | Add "Respond with ONLY valid JSON" |
| Missing `additionalProperties: false` | Model adds extra fields | Set `additionalProperties: false` in schema |
| No enum constraints | Model invents new categories | Use `enum` with explicit allowed values |
| Relying on markdown fences | Inconsistent fence placement | Strip fences in parser, not in prompt |
| Complex nested schemas | Model omits nested fields | Flatten schema or use two-pass extraction |

## Multi-Format Output

For complex reports that need both structured data and prose:

```text
Respond with JSON containing both structured data and narrative sections:
{
  "structured": {
    "risk_level": "high",
    "risk_score": 78,
    "factors": [...]
  },
  "narrative": {
    "summary": "Brief 1-2 sentence summary",
    "details": "Detailed analysis paragraph",
    "recommendation": "Action items"
  }
}
```
