# Endpoint Documentation Patterns

## Per-Endpoint Structure

Every endpoint MUST document: route + method, summary, auth policy/scopes, parameters (path/query/header), request body with validation, all response codes with schemas, and example request/response.

## Parameter Documentation

**Path** — always required, document type and format:
`| id | path | uuid | Yes | Escrow transaction ID |`

**Query** — document defaults, ranges, enums:

| Param | In | Type | Required | Default | Description |
|-------|-----|------|----------|---------|-------------|
| page | query | integer | No | 1 | Page number (min: 1) |
| pageSize | query | integer | No | 20 | Items per page (1-100) |
| status | query | string | No | — | Filter: pending, funded, released, disputed, cancelled |

**Header** — idempotency and correlation:
`| X-Idempotency-Key | header | uuid | Yes | Client-generated UUID |`

## Request Body with Validation

Map FluentValidation rules to the documentation table:

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| buyerId | uuid | Yes | Must exist in Users | Buyer identifier |
| sellerId | uuid | Yes | ≠ buyerId | Seller identifier |
| amount | decimal | Yes | 0.01–10M | Transaction amount |
| currency | string | Yes | `^[A-Z]{3}$` | ISO 4217 code |
| description | string | No | Max 500 chars | Transaction description |

Source: scan `AbstractValidator<T>` classes for `RuleFor()` chains → map to validation column.

## Response Codes

| Category | Codes | Always Document |
|----------|-------|-----------------|
| Success | 200, 201, 204 | At least one |
| Client error | 400, 401, 403, 404, 409, 422 | All that apply |
| Server error | 500 | Always |

All errors use RFC 7807 Problem Details:

```json
{
  "type": "https://api.nextruzt.io/errors/validation-failed",
  "title": "Validation Failed",
  "status": 400,
  "traceId": "00-abc123-def456-01",
  "errors": { "amount": ["Amount must be greater than 0."] }
}
```

## Example Request/Response

```bash
curl -X POST https://api.nextruzt.io/v1/escrows \
  -H "Authorization: Bearer eyJhbG..." \
  -H "Content-Type: application/json" \
  -H "X-Idempotency-Key: 7c9e6679-7425-40de-944b-e07fc1f90ae7" \
  -d '{ "buyerId": "550e8400-...", "sellerId": "6ba7b810-...", "amount": 15000.00, "currency": "USD" }'
```

```json
// 201 Created
{
  "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "status": "pending",
  "buyerId": "550e8400-...",
  "sellerId": "6ba7b810-...",
  "amount": 15000.00,
  "currency": "USD",
  "expiresAt": "2025-02-01T10:30:00Z",
  "createdAt": "2025-01-18T10:30:00Z"
}
```

## Markdown Endpoint Block Format

```markdown
### POST /api/v1/escrows
Create a new escrow transaction.
**Auth:** Bearer JWT — Policy: `escrow:create` — Scope: `escrow.write`
**Parameters:** (table)
**Request Body:** (table with validation)
**Responses:**
| Status | Description | Body |
|--------|-------------|------|
| 201 | Created | `EscrowResponse` |
| 400 | Validation | `ProblemDetails` |
| 401 | Unauthorized | `ProblemDetails` |
```

## CRUD Summary — Escrow Resource

| Method | Route | Auth Policy | Success | Key Errors |
|--------|-------|-------------|---------|------------|
| GET | /escrows | escrow:read | 200 paginated | 401 |
| GET | /escrows/{id} | escrow:read | 200 detail | 401, 404 |
| POST | /escrows | escrow:create | 201 + Location | 400, 401, 409 |
| PUT | /escrows/{id} | escrow:write | 200 | 400, 404, 409 (not pending) |
| DELETE | /escrows/{id} | escrow:cancel | 204 | 404, 409 (already funded) |
| POST | /escrows/{id}/release | escrow:release | 200 | 404, 409, 422 |
