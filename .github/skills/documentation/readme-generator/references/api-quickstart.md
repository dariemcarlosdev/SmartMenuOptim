# API Quickstart Reference

Templates and patterns for writing quickstart sections in READMEs for ASP.NET Core API projects.

## Quickstart Section Structure

1. **Prerequisites** — tools needed to run the API locally
2. **Start the server** — single command to get the API running
3. **Verify it works** — health check or simple GET request
4. **Authentication** — how to obtain and use a token
5. **Try key endpoints** — curl examples for core operations
6. **Swagger UI link** — interactive documentation URL
7. **Environment variables** — configuration table

## Start the Server

```markdown
## Quick Start

### 1. Start infrastructure
```bash
docker compose up -d
```

### 2. Run the API
```bash
dotnet run --project src/EscrowApp.Web
```

The API is now running at `https://localhost:5001`.
```

**Detection:** Read the `applicationUrl` from `Properties/launchSettings.json` under the project's profile. Look for the HTTPS URL first, fall back to HTTP.

```json
{
  "profiles": {
    "EscrowApp.Web": {
      "applicationUrl": "https://localhost:5001;http://localhost:5000"
    }
  }
}
```

## Health Check Verification

```markdown
### 3. Verify the API is running
```bash
curl -s https://localhost:5001/health | jq .
```

Expected response:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567"
}
```
```

**Detection:** Look for `.MapHealthChecks("/health")` or `AddHealthChecks()` in `Program.cs` or startup configuration.

## Authentication Setup

### Entra ID (Azure AD) Bearer Token

```markdown
### Authentication

This API uses Microsoft Entra ID for authentication. Obtain a bearer token:

```bash
# Using Azure CLI
az login
TOKEN=$(az account get-access-token \
  --resource api://{client-id} \
  --query accessToken -o tsv)

# Use the token in requests
curl -H "Authorization: Bearer $TOKEN" \
  https://localhost:5001/api/escrows
```
```

### API Key Authentication

```markdown
### Authentication

Include your API key in the `X-Api-Key` header:

```bash
curl -H "X-Api-Key: your-api-key" \
  https://localhost:5001/api/escrows
```
```

### JWT Token (IdentityServer / Custom)

```markdown
### Authentication

Obtain a JWT token from the token endpoint:

```bash
TOKEN=$(curl -s -X POST https://localhost:5001/connect/token \
  -d "grant_type=client_credentials" \
  -d "client_id=your-client-id" \
  -d "client_secret=your-client-secret" \
  -d "scope=escrow.read escrow.write" \
  | jq -r '.access_token')

curl -H "Authorization: Bearer $TOKEN" \
  https://localhost:5001/api/escrows
```
```

**Detection:** Check `Program.cs` or startup for:
- `AddMicrosoftIdentityWebApi` → Entra ID
- `AddAuthentication().AddJwtBearer()` → JWT
- Custom `ApiKeyAuthenticationHandler` → API Key

## Core Endpoint Examples

Provide curl examples for the 3–5 most important API operations:

```markdown
### Example Requests

**Create an escrow transaction**
```bash
curl -X POST https://localhost:5001/api/escrows \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "buyerId": "550e8400-e29b-41d4-a716-446655440000",
    "sellerId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
    "amount": 15000.00,
    "currency": "USD",
    "description": "Software license payment"
  }'
```

**Get escrow by ID**
```bash
curl -H "Authorization: Bearer $TOKEN" \
  https://localhost:5001/api/escrows/{escrow-id}
```

**List escrows with pagination**
```bash
curl -H "Authorization: Bearer $TOKEN" \
  "https://localhost:5001/api/escrows?page=1&pageSize=20&status=Active"
```

**Release escrow funds**
```bash
curl -X POST https://localhost:5001/api/escrows/{escrow-id}/release \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "releaseReason": "Goods delivered and accepted" }'
```
```

**Detection:** Scan controllers or minimal API endpoints to find route patterns. Use `[HttpPost]`, `[HttpGet]`, `MapPost()`, `MapGet()` attributes/calls to identify available endpoints.

## Swagger / OpenAPI

```markdown
### API Documentation

Interactive API documentation is available in Development mode:

| Resource | URL |
|----------|-----|
| Swagger UI | https://localhost:5001/swagger |
| OpenAPI Spec (JSON) | https://localhost:5001/swagger/v1/swagger.json |
| OpenAPI Spec (YAML) | https://localhost:5001/swagger/v1/swagger.yaml |
```

**Detection:** Look for `AddSwaggerGen()`, `UseSwagger()`, `UseSwaggerUI()` in `Program.cs`. Check for `Swashbuckle.AspNetCore` or `NSwag` package references.

## Environment Variables Table

```markdown
### Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | — | ✅ |
| `AzureAd__TenantId` | Microsoft Entra ID tenant ID | — | ✅ |
| `AzureAd__ClientId` | App registration client ID | — | ✅ |
| `AzureAd__Instance` | Entra ID instance URL | `https://login.microsoftonline.com/` | ❌ |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` | ❌ |
| `Logging__LogLevel__Default` | Minimum log level | `Information` | ❌ |
| `Redis__ConnectionString` | Redis cache connection | — | ❌ |
```

**Detection:** Scan `appsettings.json`, `appsettings.Development.json`, and `Program.cs` for `IConfiguration` bindings and `IOptions<T>` registrations.

## Docker Compose Quickstart

```markdown
### Running with Docker

Start all services (API + PostgreSQL + Redis) with a single command:

```bash
docker compose up --build
```

The API will be available at `https://localhost:5001`.

To run in detached mode:
```bash
docker compose up -d --build
```

Stop all services:
```bash
docker compose down
```

Reset database (removes volumes):
```bash
docker compose down -v
docker compose up --build
```
```

**Detection:** Check for `docker-compose.yml` or `compose.yml` at the repo root. List the services defined to document what gets started.

## Complete Quickstart Example

Full quickstart section for an ASP.NET Core API with PostgreSQL and Entra ID:

```markdown
## Quick Start

### Prerequisites
- .NET SDK 10.0+ (`dotnet --version`)
- Docker & Docker Compose (`docker compose version`)
- Azure CLI (`az --version`) — for Entra ID authentication

### 1. Clone and configure
```bash
git clone https://github.com/NexTruzt/EscrowApp.git
cd EscrowApp
cp .env.example .env
# Edit .env with your Entra ID tenant/client IDs
```

### 2. Start infrastructure
```bash
docker compose up -d postgres redis
```

### 3. Build and run
```bash
dotnet restore
dotnet ef database update --project src/EscrowApp.Infrastructure
dotnet run --project src/EscrowApp.Web
```

### 4. Verify
```bash
curl -s https://localhost:5001/health | jq .
# Expected: { "status": "Healthy" }
```

### 5. Explore the API
Open [Swagger UI](https://localhost:5001/swagger) in your browser for interactive documentation.
```
