---
name: api-documenter
description: "Generate API documentation with endpoint inventory, models, and OpenAPI specs. Triggers: api docs, openapi, swagger, endpoint documentation"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: documentation
  triggers: api docs, document api, openapi, swagger, endpoint documentation
  role: api-architect
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: document
  related-skills: readme-generator, adr-creator
---

# API Documenter — NexTruzt.io Escrow Platform

Generate API documentation by scanning ASP.NET Core controllers, minimal API endpoints, and MediatR handlers. Produces markdown references and OpenAPI 3.1 specifications for the NexTruzt.io fintech escrow platform (.NET 10, Blazor Server, Clean Architecture, CQRS/MediatR, PostgreSQL/EF Core).

## When to Use

- Documenting new or changed API endpoints after a feature merge
- Generating or updating the OpenAPI 3.1 / Swagger spec from source
- Creating consumer-facing API reference docs for escrow flows
- Auditing endpoints for missing responses, params, or auth gaps
- Preparing partner integration documentation

## Core Workflow

```
1. SCAN ENDPOINTS
   → Find [ApiController] classes, [Http*] attributes, MapGroup/MapGet minimal APIs
   → Locate MediatR IRequest<T> / IRequestHandler<,> pairs behind each endpoint
   → Collect: HTTP method, route template, handler, auth attributes
   ✅ Checkpoint: endpoint inventory table complete — no fictional routes

2. EXTRACT PARAMETERS & MODELS
   → Path/query/header params with types from [FromRoute], [FromQuery], [FromHeader]
   → Request body DTOs — record types, FluentValidation rules, nullability
   → Response DTOs — success + error shapes, pagination wrappers
   ✅ Checkpoint: every endpoint has params, request body, and response schema

3. DOCUMENT AUTH & SECURITY
   → [Authorize(Policy = "...")] and [AllowAnonymous] per endpoint
   → Security schemes: Bearer JWT (Entra ID), API key, OAuth 2.0 scopes
   → Rate limiting policies from RateLimiterOptions
   ✅ Checkpoint: auth requirements mapped for 100% of endpoints

4. GENERATE OUTPUT
   → Markdown API reference grouped by domain aggregate (Escrow, Payment, User)
   → OpenAPI 3.1 YAML with $ref components, discriminators, Problem Details errors
   → Order: GET → POST → PUT → PATCH → DELETE per resource
   ✅ Checkpoint: output validates against OpenAPI linter, no broken $refs
```

## Reference Guide

Load references on-demand based on the documentation task:

| Reference | Load When | Key Topics |
|---|---|---|
| [OpenAPI Spec](references/openapi-spec.md) | OpenAPI 3.1 patterns | Schema design, component reuse, discriminators |
| [Swagger Integration](references/swagger-integration.md) | Swashbuckle/NSwag setup | ASP.NET Core integration, XML comments, filters |
| [Endpoint Documentation](references/endpoint-documentation.md) | Per-endpoint docs | Parameters, responses, examples, error formats |
| [Authentication Docs](references/authentication-docs.md) | Auth flow documentation | OAuth 2.0/OIDC flows, Bearer JWT, API key patterns |

## Quick Reference

Minimal per-endpoint documentation block:

```markdown
### POST /api/v1/escrows

Create a new escrow transaction.

**Auth:** Bearer JWT — Policy: `escrow:create`

| Param | In | Type | Required | Description |
|-------|-----|------|----------|-------------|
| X-Idempotency-Key | header | string | Yes | Client-generated UUID for idempotent creation |

**Request Body** (`application/json`):
{ "buyerId": "uuid", "sellerId": "uuid", "amount": 15000.00, "currency": "USD" }

**201 Created** → `EscrowResponse { id, status, createdAt }`
**400** → `ProblemDetails` (validation errors)
**401** → Missing/invalid token
**409** → Duplicate idempotency key
```

## Constraints

**MUST DO:** Scan actual source code (no fictional endpoints) · Include all endpoints even undocumented · Document errors with Problem Details (RFC 7807) · Map auth per endpoint · Use accurate types from DTOs/FluentValidation · Include example JSON · Validate OpenAPI 3.1 (no broken `$ref`) · Group by domain aggregate

**MUST NOT:** Invent endpoints/params · Omit error codes · Skip auth requirements · Generate invalid OpenAPI · Hard-code secrets/keys · Document internal infra endpoints · Assume auth without checking `[Authorize]`

## Output Template

```markdown
# {API Name} — v{version} Reference
**Base URL:** `{base-url}` | **Auth:** Bearer JWT (Entra ID)

## Endpoint Summary
| Method | Endpoint | Description | Auth Policy |
|--------|----------|-------------|-------------|
| POST | /escrows | Create escrow | escrow:create |
| GET | /escrows/{id} | Get details | escrow:read |

## {Resource} → (full endpoint blocks per Quick Reference format above)
## Models → (field tables per response DTOs)
```

```yaml
openapi: "3.1.0"
info: { title: "{API Name}", version: "1.0.0" }
servers: [{ url: "https://api.nextruzt.io/v1" }]
security: [{ bearerAuth: [] }]
paths:
  /escrows:
    post:
      operationId: createEscrow
      tags: [Escrow]
      requestBody:
        content:
          application/json:
            schema: { $ref: "#/components/schemas/CreateEscrowRequest" }
      responses:
        "201": { content: { application/json: { schema: { $ref: "#/components/schemas/EscrowResponse" } } } }
        "400": { $ref: "#/components/responses/ValidationError" }
components:
  securitySchemes:
    bearerAuth: { type: http, scheme: bearer, bearerFormat: JWT }
  schemas:
    ProblemDetails:
      type: object
      properties:
        type: { type: string, format: uri }
        title: { type: string }
        status: { type: integer }
        errors: { type: object, additionalProperties: { type: array, items: { type: string } } }
  responses:
    ValidationError:
      description: Validation failed
      content:
        application/problem+json:
          schema: { $ref: "#/components/schemas/ProblemDetails" }
```
