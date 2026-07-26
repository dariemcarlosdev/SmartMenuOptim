# Docker Builds Reference

## Multi-Stage Dockerfile for .NET

Multi-stage builds separate the build environment from the runtime image, reducing final image size and attack surface.

```dockerfile
# ── Stage 1: Build ──
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy project files first for layer caching
COPY ["EscrowApp/EscrowApp.csproj", "EscrowApp/"]
COPY ["EscrowApp.Domain/EscrowApp.Domain.csproj", "EscrowApp.Domain/"]
COPY ["EscrowApp.Application/EscrowApp.Application.csproj", "EscrowApp.Application/"]
COPY ["EscrowApp.Infrastructure/EscrowApp.Infrastructure.csproj", "EscrowApp.Infrastructure/"]
RUN dotnet restore "EscrowApp/EscrowApp.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "EscrowApp/EscrowApp.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:ContinuousIntegrationBuild=true

# ── Stage 2: Runtime ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

# Copy published output
COPY --from=build /app/publish .

# Configure health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

# Switch to non-root user
USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "EscrowApp.dll"]
```

## Layer Ordering for Optimal Caching

Docker caches layers sequentially — when a layer changes, all subsequent layers are invalidated. Order operations from least to most frequently changing:

```
1. Base image          (changes rarely)
2. System packages     (changes rarely)
3. .csproj files       (changes when dependencies change)
4. dotnet restore      (cached unless .csproj changes)
5. Source code COPY    (changes on every commit)
6. dotnet build/publish (rebuilds when source changes)
```

**Critical pattern — copy project files before source:**

```dockerfile
# GOOD: Restore is cached unless .csproj changes
COPY ["src/App/App.csproj", "src/App/"]
RUN dotnet restore "src/App/App.csproj"
COPY . .
RUN dotnet publish -c Release -o /app

# BAD: Restore runs on every build because source changes invalidate the COPY layer
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app
```

For solutions with many projects, copy all `.csproj` and `.sln` files first:

```dockerfile
COPY ["*.sln", "./"]
COPY ["src/*/*.csproj", "./"]
RUN for file in *.csproj; do \
      dir=$(basename "$file" .csproj); \
      mkdir -p "src/$dir" && mv "$file" "src/$dir/"; \
    done
RUN dotnet restore
```

## Non-Root User Configuration

Running containers as root is a security risk. Always configure a non-root user:

### Alpine-based images

```dockerfile
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser
```

### Debian/Ubuntu-based images

```dockerfile
RUN groupadd -r appgroup && useradd -r -g appgroup -s /sbin/nologin appuser
USER appuser
```

**Considerations:**
- Set `USER` after copying files — build steps may need root
- Ensure the app listens on a non-privileged port (≥ 1024), e.g., 8080
- ASP.NET Core defaults to port 8080 in .NET 8+ container images
- If writing temp files, ensure the user has write access to the target directory

## Health Check Configuration

Add health checks directly in the Dockerfile:

```dockerfile
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1
```

**Parameters:**
- `--interval` — time between checks (default 30s)
- `--timeout` — max time for a single check (default 30s)
- `--start-period` — grace period for container startup (default 0s)
- `--retries` — consecutive failures before marking unhealthy (default 3)

For ASP.NET Core, map a health endpoint:

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgresql")
    .AddCheck<EscrowServiceHealthCheck>("escrow-service");

app.MapHealthChecks("/health");
```

Use `wget` (Alpine) or `curl` (Debian) — choose based on your base image. Prefer `wget` for Alpine since `curl` requires an additional package.

## .dockerignore Best Practices

Reduce build context size and prevent sensitive files from leaking into the image:

```dockerignore
# Build output
**/bin/
**/obj/
**/out/
**/publish/

# IDE and OS files
**/.vs/
**/.vscode/
**/.idea/
**/node_modules/
**/*.user
**/*.suo
**/launchSettings.json

# Git
.git
.gitignore

# Docker
**/Dockerfile*
**/.dockerignore
docker-compose*.yml

# CI/CD
.github/
.azure-pipelines/

# Secrets and config (NEVER include in image)
**/*.pfx
**/*.key
**/appsettings.Development.json
**/appsettings.Local.json
**/.env
**/secrets/

# Documentation
**/*.md
LICENSE
```

**Why it matters:**
- Smaller build context = faster image builds
- Prevents secrets and development config from being embedded in the image
- Excludes unnecessary files from layer cache invalidation

## Image Scanning with Trivy

Scan images for vulnerabilities before pushing to a registry:

### GitHub Actions

```yaml
- name: Build Docker Image
  run: docker build -t myapp:${{ github.sha }} .

- name: Scan with Trivy
  uses: aquasecurity/trivy-action@18f2510ee396bbf400402947e0f18c8ea63fd575 # v0.28
  with:
    image-ref: myapp:${{ github.sha }}
    format: 'sarif'
    output: 'trivy-results.sarif'
    severity: 'CRITICAL,HIGH'
    exit-code: '1'  # Fail on critical/high findings

- name: Upload Trivy SARIF
  uses: github/codeql-action/upload-sarif@7e187e1c529d80bac7b87a16e5e6d5e5b4a12bb4 # v3
  if: always()
  with:
    sarif_file: 'trivy-results.sarif'
```

### Local Development

```bash
# Scan a local image
trivy image myapp:latest

# Scan with severity filter and fail on findings
trivy image --severity CRITICAL,HIGH --exit-code 1 myapp:latest

# Scan filesystem (pre-build)
trivy fs --severity CRITICAL,HIGH .
```

**Scanning best practices:**
- Scan in CI before pushing to registry — block vulnerable images
- Upload SARIF results to GitHub Security tab for centralized tracking
- Set `exit-code: 1` with `severity: CRITICAL,HIGH` to fail pipelines on serious issues
- Scan base images separately to track upstream vulnerabilities
- Use `.trivyignore` to suppress accepted risks with documented justification

## Production Dockerfile — ASP.NET Core + Blazor Server

```dockerfile
# ── Build stage ──
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore dependencies (cached layer)
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["EscrowApp.sln", "./"]
COPY ["EscrowApp/EscrowApp.csproj", "EscrowApp/"]
COPY ["EscrowApp.Domain/EscrowApp.Domain.csproj", "EscrowApp.Domain/"]
COPY ["EscrowApp.Application/EscrowApp.Application.csproj", "EscrowApp.Application/"]
COPY ["EscrowApp.Infrastructure/EscrowApp.Infrastructure.csproj", "EscrowApp.Infrastructure/"]
RUN dotnet restore "EscrowApp.sln"

# Build and publish
COPY . .
RUN dotnet publish "EscrowApp/EscrowApp.csproj" \
    -c ${BUILD_CONFIGURATION} \
    -o /app/publish \
    --no-restore \
    /p:ContinuousIntegrationBuild=true \
    /p:PublishTrimmed=false

# ── Runtime stage ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# Security: install only required CA certificates, remove cache
RUN apk add --no-cache icu-libs

# Create non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

WORKDIR /app
COPY --from=build --chown=appuser:appgroup /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

USER appuser
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "EscrowApp.dll"]
```

**Key decisions:**
- **Alpine base** — smallest image size (~110 MB vs ~220 MB for Debian)
- **`icu-libs`** — required for globalization (currency formatting in fintech)
- **`DOTNET_EnableDiagnostics=0`** — disables diagnostic pipe for reduced attack surface
- **`PublishTrimmed=false`** — Blazor Server uses reflection; trimming breaks it
- **`--chown`** — sets file ownership during COPY to avoid extra layer
- **`start-period: 15s`** — Blazor Server needs time to initialize SignalR hub
