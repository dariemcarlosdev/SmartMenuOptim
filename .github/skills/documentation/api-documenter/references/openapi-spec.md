# OpenAPI 3.1 Specification Patterns

## Document Structure

```yaml
openapi: "3.1.0"
info: { title: string, version: string, description: string, contact: { name, email } }
servers:
  - url: https://api.nextruzt.io/v1   # Production
  - url: https://api-staging.nextruzt.io/v1  # Staging
security: [{ bearerAuth: [] }]
paths: {}
components: { schemas: {}, responses: {}, parameters: {}, securitySchemes: {} }
tags: []
```

## Schema Best Practices

- Always specify `type`, `format`, and constraints — avoid empty `{}` schemas
- Use `examples` array for documentation: `examples: ["USD", "EUR"]`
- OpenAPI 3.1 nullable uses type arrays: `type: ["string", "null"]` (not `nullable: true`)

```yaml
EscrowAmount:
  type: number
  format: decimal
  minimum: 0.01
  maximum: 10000000

CurrencyCode:
  type: string
  pattern: "^[A-Z]{3}$"
  examples: ["USD", "EUR", "GBP"]
```

## Component Reuse ($ref)

Extract repeated schemas into `components/schemas` — one definition, many references:

```yaml
# Reference inline:  $ref: "#/components/schemas/EscrowResponse"
# Reference response: $ref: "#/components/responses/ValidationError"
# Compose variants:  allOf: [{ $ref: "#/components/schemas/BaseEvent" }, { ... }]
```

**Rules:** Name by domain concept (`EscrowResponse`, not `PostResponseBody`). Never duplicate inline. Use `allOf` for extension. Define standard errors (401, 403, 500) once in `components/responses`.

## Discriminator for Polymorphic Types

```yaml
EscrowEvent:
  discriminator:
    propertyName: eventType
    mapping:
      escrow.created: "#/components/schemas/EscrowCreatedEvent"
      escrow.funded: "#/components/schemas/EscrowFundedEvent"
  oneOf:
    - $ref: "#/components/schemas/EscrowCreatedEvent"
    - $ref: "#/components/schemas/EscrowFundedEvent"
```

## Pagination Schema

```yaml
PaginationMetadata:
  type: object
  required: [page, pageSize, totalCount, totalPages]
  properties:
    page: { type: integer, minimum: 1 }
    pageSize: { type: integer, minimum: 1, maximum: 100 }
    totalCount: { type: integer, minimum: 0 }
    totalPages: { type: integer, minimum: 0 }
    hasNextPage: { type: boolean }
```

Use with `allOf` per list endpoint to specify the `data` items type.

## Error Response — RFC 7807 Problem Details

```yaml
ProblemDetails:
  type: object
  required: [type, title, status]
  properties:
    type: { type: string, format: uri, example: "https://api.nextruzt.io/errors/validation-failed" }
    title: { type: string, example: "Validation Failed" }
    status: { type: integer, example: 400 }
    detail: { type: string }
    traceId: { type: string }
    errors:
      type: object
      additionalProperties: { type: array, items: { type: string } }
```

## Example — Escrow Create Endpoint

```yaml
/escrows:
  post:
    operationId: createEscrow
    summary: Create a new escrow transaction
    tags: [Escrow]
    security: [{ bearerAuth: [escrow:create] }]
    parameters:
      - name: X-Idempotency-Key
        in: header
        required: true
        schema: { type: string, format: uuid }
    requestBody:
      required: true
      content:
        application/json:
          schema: { $ref: "#/components/schemas/CreateEscrowRequest" }
          example:
            buyerId: "550e8400-e29b-41d4-a716-446655440000"
            sellerId: "6ba7b810-9dad-11d1-80b4-00c04fd430c8"
            amount: 15000.00
            currency: "USD"
    responses:
      "201":
        description: Escrow created
        headers:
          Location: { schema: { type: string, format: uri } }
        content:
          application/json:
            schema: { $ref: "#/components/schemas/EscrowResponse" }
      "400": { $ref: "#/components/responses/ValidationError" }
      "401": { $ref: "#/components/responses/Unauthorized" }
      "409": { description: "Duplicate idempotency key" }
```
