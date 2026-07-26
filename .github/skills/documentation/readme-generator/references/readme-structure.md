# README Structure Reference

Recommended section order, content guidelines, and templates for .NET / Clean Architecture project READMEs.

## Section Order

Generate sections in this exact order. Omit sections marked *(conditional)* when they don't apply.

| # | Section | Required | Notes |
|---|---------|----------|-------|
| 1 | Title + Description | ✅ | Project name as H1, one-paragraph summary |
| 2 | Badges | ✅ | Build, coverage, license — see badge-catalog.md |
| 3 | Technology Stack | ✅ | Table of layers → technologies → versions |
| 4 | Architecture Overview | ✅ | ASCII diagram with real project/layer names |
| 5 | Prerequisites | ✅ | SDK, database, tools with exact versions |
| 6 | Getting Started | ✅ | Clone → restore → configure → migrate → run |
| 7 | Running Tests | ✅ | Commands for unit, integration, and E2E tests |
| 8 | API Documentation | Conditional | Only for projects exposing HTTP APIs |
| 9 | Usage Examples | Conditional | CLI tools, libraries, or SDK projects |
| 10 | Configuration | Conditional | Environment variables, appsettings overrides |
| 11 | Deployment | Conditional | Docker, Azure, AWS, or Kubernetes instructions |
| 12 | Contributing | ✅ | Link to CONTRIBUTING.md or inline guide |
| 13 | License | ✅ | License type + link to LICENSE file |

## Section Guidelines

### 1. Title + Description

```markdown
# NexTruzt Escrow Platform

A fintech escrow platform built with .NET 10, Blazor Server, and Clean Architecture.
Provides secure transaction escrow, multi-party workflows, and real-time status tracking
for B2B payment operations.
```

- Extract the description from `.csproj` `<Description>` or `<PackageDescription>` first
- Fall back to inferring from namespace names, controller routes, and domain entities
- Keep to 2–3 sentences maximum

### 2. Technology Stack Table

```markdown
## Technology Stack

| Layer          | Technology              | Version |
|----------------|-------------------------|---------|
| Runtime        | .NET                    | 10.0    |
| Framework      | ASP.NET Core            | 10.0    |
| UI             | Blazor Server           | 10.0    |
| Database       | PostgreSQL              | 16      |
| ORM            | Entity Framework Core   | 10.0    |
| Patterns       | CQRS + MediatR          | 12.x    |
| Validation     | FluentValidation        | 11.x   |
| Auth           | Microsoft Entra ID      | —       |
| CI/CD          | GitHub Actions          | —       |
| Containerization | Docker + Compose      | —       |
```

- Read versions from `global.json`, `.csproj` `<PackageReference>`, and `docker-compose.yml`
- Only include rows for technologies actually present in the project

### 3. Architecture Overview

Use an ASCII diagram that maps to the actual project directory structure.

```
┌─────────────────────────────────────────────┐
│              src/Web (Blazor Server)         │
│   Pages, Components, wwwroot, Program.cs    │
├─────────────────────────────────────────────┤
│           src/Application (CQRS)            │
│  Commands/, Queries/, DTOs/, Behaviors/     │
│  MediatR Handlers, FluentValidation         │
├─────────────────────────────────────────────┤
│               src/Domain                    │
│  Entities/, ValueObjects/, Enums/,          │
│  Aggregates/, DomainEvents/                 │
├─────────────────────────────────────────────┤
│            src/Infrastructure               │
│  Persistence/ (EF Core, Migrations)         │
│  ExternalServices/, Identity/               │
└─────────────────────────────────────────────┘
         │                    │
         ▼                    ▼
   ┌───────────┐      ┌──────────────┐
   │ PostgreSQL │      │  Entra ID /  │
   │            │      │  External    │
   └───────────┘      └──────────────┘
```

- Replace generic labels with actual project names from the `.sln`
- Show external dependencies (database, identity provider, message broker) below the stack

### 4. Clean Architecture Project Structure

Document the directory layout using the actual solution structure:

```markdown
## Project Structure

```
EscrowApp/
├── src/
│   ├── EscrowApp.Domain/           # Entities, value objects, domain events
│   ├── EscrowApp.Application/      # CQRS handlers, DTOs, validators
│   ├── EscrowApp.Infrastructure/   # EF Core, external services, identity
│   └── EscrowApp.Web/              # Blazor Server, pages, components
├── tests/
│   ├── EscrowApp.Domain.Tests/
│   ├── EscrowApp.Application.Tests/
│   └── EscrowApp.Integration.Tests/
├── docker-compose.yml
├── global.json
└── EscrowApp.sln
```
```

### 5. Prerequisites

Always specify exact minimum versions and how to verify them:

```markdown
## Prerequisites

| Tool | Minimum Version | Verify Command |
|------|-----------------|----------------|
| .NET SDK | 10.0 | `dotnet --version` |
| PostgreSQL | 16 | `psql --version` |
| Docker | 24.0 | `docker --version` |
| Docker Compose | 2.20 | `docker compose version` |
| Node.js | 20 LTS | `node --version` (if used) |
```

### 6. Getting Started

Provide copy-pasteable commands in a numbered sequence:

```markdown
## Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/{owner}/{repo}.git
   cd {repo}
   ```

2. **Install .NET SDK** (if not already installed)
   ```bash
   # Verify with: dotnet --version
   # Download from: https://dotnet.microsoft.com/download
   ```

3. **Start infrastructure** (PostgreSQL via Docker)
   ```bash
   docker compose up -d postgres
   ```

4. **Configure environment**
   ```bash
   cp .env.example .env
   # Edit .env with your database connection string and auth settings
   ```

5. **Restore and build**
   ```bash
   dotnet restore
   dotnet build
   ```

6. **Apply database migrations**
   ```bash
   dotnet ef database update --project src/EscrowApp.Infrastructure
   ```

7. **Run the application**
   ```bash
   dotnet run --project src/EscrowApp.Web
   # Navigate to https://localhost:5001
   ```
```

## Conditional Sections

### API Documentation (include when project exposes HTTP endpoints)

```markdown
## API Documentation

Interactive API documentation is available via Swagger UI when running in Development mode:

- **Swagger UI:** https://localhost:5001/swagger
- **OpenAPI spec:** https://localhost:5001/swagger/v1/swagger.json
```

### Configuration (include when project uses environment variables)

```markdown
## Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | — | ✅ |
| `AzureAd__TenantId` | Entra ID tenant | — | ✅ |
| `AzureAd__ClientId` | Entra ID app registration | — | ✅ |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` | ❌ |
```

### Deployment (include when Docker/IaC files are present)

```markdown
## Deployment

### Docker

```bash
docker compose up --build
```

### Azure (if applicable)

```bash
az webapp deploy --resource-group {rg} --name {app} --src-path ./publish
```
```
