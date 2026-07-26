# Authentication Documentation Patterns

## OAuth 2.0 — Authorization Code + PKCE

Recommended flow for interactive clients (Blazor, SPAs, mobile):

```
1. Client generates code_verifier + code_challenge (SHA256)
2. Redirect to Entra ID authorize endpoint with code_challenge
3. User authenticates → redirect back with authorization_code
4. Exchange code + code_verifier for tokens
5. Include access_token as: Authorization: Bearer {token}
```

**Entra ID Endpoints:**

| Purpose | URL |
|---------|-----|
| Authorize | `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize` |
| Token | `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token` |
| JWKS | `https://login.microsoftonline.com/{tenant}/discovery/v2.0/keys` |

## Bearer JWT Token Claims

| Claim | Description |
|-------|-------------|
| `iss` | `https://login.microsoftonline.com/{tenant}/v2.0` |
| `aud` | `api://{client-id}` |
| `sub` | Unique user identifier |
| `scp` | Space-delimited scopes |
| `roles` | App roles assigned |
| `exp` | Expiration (Unix epoch) |
| `oid` | Entra ID object ID |

**Validation:** Server verifies signature (JWKS), issuer, audience, expiration, not-before, and required scopes/roles on every request.

## API Key Pattern

For server-to-server integrations: `X-API-Key: ntzt_live_k1_a3b8c9d0...`

- Format: `ntzt_{env}_k{version}_{secret}` (32+ chars)
- Scoped to specific permissions, optional IP allowlist, 365-day expiration
- Provides application-level auth (not user-level) — prefer OAuth client credentials for new integrations

## Scope-Based Authorization

| Scope | Endpoints |
|-------|-----------|
| `escrow.read` | GET /escrows, GET /escrows/{id} |
| `escrow.write` | POST /escrows, PUT /escrows/{id} |
| `escrow.release` | POST /escrows/{id}/release |
| `escrow.cancel` | DELETE /escrows/{id} |
| `payment.read` | GET /payments |
| `user.read` | GET /users/{id} |

Scopes map to ASP.NET Core policies: `policy.RequireClaim("scp", "escrow.read").RequireAuthenticatedUser()`

## Token Acquisition — curl Examples

**Client Credentials (machine-to-machine):**

```bash
curl -X POST "https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token" \
  -d "grant_type=client_credentials&client_id={id}&client_secret={secret}&scope=api://{id}/.default"

# Use token:
curl -H "Authorization: Bearer {access_token}" https://api.nextruzt.io/v1/escrows
```

**Postman:** Auth tab → OAuth 2.0 → Authorization Code (With PKCE) → set Auth URL, Token URL, Client ID, Scopes, Code Challenge Method: SHA-256.

## OpenAPI Security Schemes

```yaml
components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
    oauth2:
      type: oauth2
      flows:
        authorizationCode:
          authorizationUrl: https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize
          tokenUrl: https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
          scopes:
            api://{client-id}/escrow.read: Read escrow transactions
            api://{client-id}/escrow.write: Create and modify escrows
    apiKey:
      type: apiKey
      in: header
      name: X-API-Key
```

**Per-endpoint override:** `security: [{ bearerAuth: [escrow.read] }, { apiKey: [] }]` (accept either). Use `security: []` for unauthenticated endpoints (e.g., `/health`).

## Auth Error Responses

| Status | Meaning | Action |
|--------|---------|--------|
| 401 | Missing/invalid token | Obtain new access token |
| 403 | Valid token, insufficient scope | Request additional permissions |

```json
{
  "type": "https://api.nextruzt.io/errors/forbidden",
  "title": "Forbidden",
  "status": 403,
  "detail": "The 'escrow.release' scope is required for this operation."
}
```
