---
name: readme-generator
description: "Generate comprehensive README.md files from project analysis. Triggers: readme, generate readme, project documentation"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: documentation
  triggers: readme, generate readme, project readme, documentation setup
  role: technical-writer
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: document
  related-skills: adr-creator, api-documenter, contributing-guide
---

# README Generator

Generate production-ready README.md files by analyzing .NET project structure, configuration, and source code — tailored for Clean Architecture / CQRS codebases.

## When to Use

- Bootstrapping a new .NET repository with a professional README
- Replacing placeholder or outdated documentation after a major refactor
- Onboarding new developers who need a clear project overview and quickstart
- Preparing an open-source or inner-source release with complete documentation
- Auditing documentation completeness before a production milestone
- Generating a README that accurately reflects a Clean Architecture / DDD codebase

## Core Workflow

### Phase 1 — Project Discovery

Scan the repository to build an inventory of project metadata.

1. List top-level files and directories (2 levels deep)
2. Identify solution structure — `.sln`, `.csproj`, `global.json`, `Directory.Build.props`
3. Read `global.json` → extract SDK version; read `.csproj` → extract `TargetFramework`, package refs
4. Detect infrastructure files — `Dockerfile`, `docker-compose.yml`, `.github/workflows/`, `terraform/`
5. Identify entry points — `Program.cs`, `Startup.cs`, Blazor `App.razor`

✅ **Checkpoint:** You have project name, .NET version, project type (API / Blazor / Worker), and dependency list.

### Phase 2 — Architecture Analysis

Map the codebase to its architectural layers and patterns.

1. Identify architecture pattern from directory structure:
   - `src/Domain/`, `src/Application/`, `src/Infrastructure/`, `src/Web/` → Clean Architecture
   - Look for MediatR handlers (Commands/, Queries/) → CQRS pattern
   - Look for `DbContext` subclasses → EF Core data layer
2. Detect external integrations — message brokers, caches, cloud services
3. Map data flow: API → MediatR → Handler → Repository → Database
4. Note authentication pattern — Entra ID, IdentityServer, ASP.NET Identity

✅ **Checkpoint:** You can draw an ASCII architecture diagram with real layer names.

### Phase 3 — README Generation

Assemble findings into a structured document.

1. Load [README Structure](references/readme-structure.md) → follow section order
2. Load [Badge Catalog](references/badge-catalog.md) → generate badge row from detected CI/license
3. Load [API Quickstart](references/api-quickstart.md) → build quickstart section if project exposes APIs
4. Load [Contributing Guide](references/contributing-guide.md) → generate or reference CONTRIBUTING.md
5. Fill every section with **real values** discovered in Phases 1–2
6. Validate: no placeholder text, no fabricated versions, all commands runnable

✅ **Checkpoint:** README is complete, every section uses actual project data.

## Reference Guide

| Reference | Load When | Key Topics |
|---|---|---|
| [README Structure](references/readme-structure.md) | README sections and order | Section hierarchy, .NET project specifics, Clean Architecture |
| [Badge Catalog](references/badge-catalog.md) | CI, coverage, license badges | GitHub Actions, Codecov, NuGet, shields.io patterns |
| [API Quickstart](references/api-quickstart.md) | Quick start examples | curl examples, Swagger link, authentication setup |
| [Contributing Guide](references/contributing-guide.md) | CONTRIBUTING.md template | PR process, coding standards, commit conventions |

## Quick Reference

Minimal README skeleton for a .NET Clean Architecture project:

```markdown
# {ProjectName}

{One-paragraph description.}

![Build](https://github.com/{owner}/{repo}/actions/workflows/ci.yml/badge.svg)
![Coverage](https://codecov.io/gh/{owner}/{repo}/branch/main/graph/badge.svg)
![License](https://img.shields.io/github/license/{owner}/{repo})

## Tech Stack
| Layer | Technology | Version |
|---|---|---|
| Runtime | .NET | 10.0 |
| Framework | ASP.NET Core / Blazor Server | 10.0 |
| Database | PostgreSQL | 16 |
| ORM | EF Core | 10.0 |

## Getting Started
1. `git clone {repo-url} && cd {repo}`
2. `cp .env.example .env` — configure connection string
3. `dotnet restore && dotnet ef database update`
4. `dotnet run --project src/Web`

## Architecture
{ASCII diagram}

## License
MIT — see [LICENSE](LICENSE)
```

## Constraints

### MUST DO

- Analyze the actual codebase before writing — never produce a generic template
- Use real version numbers, dependency names, and project names from config files
- Generate runnable `dotnet` commands that match the detected project structure
- Include a prerequisites section with exact SDK, database, and tool versions
- Generate badges matching the actual CI/CD platform, coverage tool, and license
- Keep content scannable — use tables, code blocks, and clear heading hierarchy
- Document the Clean Architecture layer structure when detected
- Reference actual `launchSettings.json` ports and URLs

### MUST NOT

- Fabricate dependencies, features, or versions not found in the codebase
- Include sections that don't apply (e.g., API docs for a pure Blazor app with no API)
- Generate placeholder text like "TODO", "Add description here", or "Lorem ipsum"
- Hardcode absolute file paths or user-specific environment values
- Include sensitive data — connection strings, API keys, secrets, internal URLs
- Assume package manager or toolchain — detect from lock files and config

## Output Template

```markdown
# {Project Name}

{One-paragraph description extracted from .csproj Description or inferred from code.}

{Badge row — see references/badge-catalog.md}

## Technology Stack

| Layer | Technology | Version |
|---|---|---|
| Runtime | .NET | {from global.json} |
| Framework | {ASP.NET Core / Blazor Server} | {version} |
| Database | {PostgreSQL / SQL Server} | {version} |
| ORM | Entity Framework Core | {version} |
| Patterns | CQRS + MediatR | {version} |
| CI/CD | GitHub Actions | — |

## Architecture Overview

{ASCII diagram from Phase 2 — use real layer/project names}

## Prerequisites

- .NET SDK {version} (`dotnet --version`)
- {Database} {version} (local or Docker)
- Docker & Docker Compose (optional, for containerized setup)
- Node.js {version} (if Blazor uses npm tooling)

## Getting Started

{Clone → restore → configure → migrate → run steps with real commands}

## Running Tests

{dotnet test commands referencing actual test projects}

## API Documentation

{Swagger UI URL from launchSettings.json, if applicable}

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup, coding standards, and PR process.

## License

{License type} — see [LICENSE](LICENSE) for details.
```
