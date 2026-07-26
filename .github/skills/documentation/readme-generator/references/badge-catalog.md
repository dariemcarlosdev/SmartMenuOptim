# Badge Catalog Reference

Badge templates, shields.io patterns, and placement best practices for .NET project READMEs.

## Badge Placement

Place badges immediately after the project description, before the first `##` heading. Use a single line with spaces between badges for a clean layout.

```markdown
# Project Name

Short project description.

[![Build Status](build-url)](link) [![Coverage](cov-url)](link) [![License](lic-url)](link) [![.NET](dotnet-url)](link)
```

## Core Badges

### GitHub Actions Build Status

```markdown
[![Build Status](https://github.com/{owner}/{repo}/actions/workflows/{workflow-file}/badge.svg?branch=main)](https://github.com/{owner}/{repo}/actions/workflows/{workflow-file})
```

**Detection:** Look for `.github/workflows/*.yml` files. Use the primary CI workflow filename (commonly `ci.yml`, `build.yml`, or `dotnet.yml`).

**Example:**
```markdown
[![Build Status](https://github.com/NexTruzt/EscrowApp/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/NexTruzt/EscrowApp/actions/workflows/ci.yml)
```

### Code Coverage (Codecov)

```markdown
[![codecov](https://codecov.io/gh/{owner}/{repo}/branch/main/graph/badge.svg?token={token})](https://codecov.io/gh/{owner}/{repo})
```

**Detection:** Look for Codecov configuration in CI workflow (`codecov/codecov-action`) or a `codecov.yml` file. If no token is public, use the tokenless format:

```markdown
[![codecov](https://codecov.io/gh/{owner}/{repo}/branch/main/graph/badge.svg)](https://codecov.io/gh/{owner}/{repo})
```

### Code Coverage (Coverlet + shields.io)

When using Coverlet without Codecov, generate a static or dynamic badge:

```markdown
[![Coverage](https://img.shields.io/badge/coverage-85%25-brightgreen)](link-to-report)
```

Color thresholds:
- `≥ 90%` → `brightgreen`
- `≥ 75%` → `green`
- `≥ 60%` → `yellowgreen`
- `≥ 40%` → `yellow`
- `< 40%` → `red`

### License Badge

```markdown
[![License](https://img.shields.io/github/license/{owner}/{repo})](LICENSE)
```

**Detection:** Check for `LICENSE`, `LICENSE.md`, or `LICENSE.txt` at the repo root. Read the file to determine the license type for the alt text.

**Static alternative** (when repo is private or license is known):

```markdown
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)
[![License: Proprietary](https://img.shields.io/badge/License-Proprietary-red.svg)](LICENSE)
```

### .NET Version Badge

```markdown
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
```

**Detection:** Read `TargetFramework` from `.csproj` (e.g., `net10.0` → `10.0`) or `global.json` SDK version.

**Version mapping:**
| TargetFramework | Badge Label |
|-----------------|-------------|
| `net10.0` | `.NET 10.0` |
| `net9.0` | `.NET 9.0` |
| `net8.0` | `.NET 8.0` |

### NuGet Package Version (for library projects)

```markdown
[![NuGet](https://img.shields.io/nuget/v/{PackageId})](https://www.nuget.org/packages/{PackageId})
```

**Detection:** Only include for projects with `<IsPackable>true</IsPackable>` or a `.nuspec` file. Read `<PackageId>` from `.csproj`.

**With downloads count:**
```markdown
[![NuGet](https://img.shields.io/nuget/v/{PackageId})](https://www.nuget.org/packages/{PackageId}) [![NuGet Downloads](https://img.shields.io/nuget/dt/{PackageId})](https://www.nuget.org/packages/{PackageId})
```

## Platform & Tool Badges

### PostgreSQL

```markdown
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org)
```

### Docker

```markdown
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)](docker-compose.yml)
```

### Blazor

```markdown
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
```

### Entity Framework Core

```markdown
[![EF Core](https://img.shields.io/badge/EF_Core-10.0-512BD4?logo=dotnet)](https://learn.microsoft.com/ef/core/)
```

## Custom Badge Creation (shields.io)

### Static Badge Format

```
https://img.shields.io/badge/{label}-{message}-{color}?logo={logo}&logoColor={logoColor}
```

| Parameter | Description | Example |
|-----------|-------------|---------|
| `label` | Left side text | `build`, `.NET`, `coverage` |
| `message` | Right side text | `passing`, `10.0`, `85%25` |
| `color` | Right side color | `brightgreen`, `blue`, `512BD4` |
| `logo` | Simple Icons name | `dotnet`, `postgresql`, `docker` |
| `logoColor` | Logo color | `white`, `000000` |

**URL-encoding:** Use `%20` for spaces, `%25` for `%`, `--` for `-` in text.

### Dynamic Badge (from endpoint)

```
https://img.shields.io/endpoint?url={json-endpoint}
```

JSON endpoint must return:
```json
{
  "schemaVersion": 1,
  "label": "coverage",
  "message": "85%",
  "color": "brightgreen"
}
```

## Full Badge Row Example

Complete badge row for a .NET 10 Clean Architecture project:

```markdown
[![Build Status](https://github.com/NexTruzt/EscrowApp/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/NexTruzt/EscrowApp/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/NexTruzt/EscrowApp/branch/main/graph/badge.svg)](https://codecov.io/gh/NexTruzt/EscrowApp)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
```

## Best Practices

- **Order:** Build → Coverage → License → Platform → Tools
- **Consistency:** Use the same badge service (shields.io) for all static badges
- **Accuracy:** Never include a badge for a service that isn't configured (e.g., no Codecov badge without Codecov integration)
- **Links:** Every badge should link to a relevant page (CI dashboard, coverage report, license file)
- **Branch:** Always specify `?branch=main` on CI badges to show the default branch status
- **Private repos:** Use static badges when dynamic badges require public repo access
