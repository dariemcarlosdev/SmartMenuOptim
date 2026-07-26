# Outdated Detection — Finding Stale Packages

## Purpose

Compare installed package versions against the latest stable releases to identify dependencies that may be missing security patches, performance improvements, or bug fixes.

## .NET Outdated Detection

### Primary Command

```bash
# Check all projects in solution
dotnet list EscrowApp.sln package --outdated

# Include transitive dependencies
dotnet list EscrowApp.sln package --outdated --include-transitive

# Check specific project
dotnet list src/EscrowApp.Web/EscrowApp.Web.csproj package --outdated

# Output format:
# Project 'EscrowApp.Web' has the following updates to its packages
#    [net10.0]:
#    Top-level Package          Requested   Resolved   Latest
#    > MediatR                  12.2.0      12.2.0     12.4.1
#    > FluentValidation         11.9.0      11.9.0     11.11.0
```

### With Central Package Management

When using `Directory.Packages.props`, check from the solution root:

```bash
# All version pins are in Directory.Packages.props
dotnet list package --outdated --source https://api.nuget.org/v3/index.json
```

## Staleness Categories

| Category | Definition | Priority | Example |
|----------|-----------|----------|---------|
| **Patch behind** | Same major.minor, newer patch | P3 — Low | 12.2.0 → 12.2.3 |
| **Minor behind** | Same major, newer minor | P2 — Medium | 12.2.0 → 12.4.1 |
| **Major behind (1)** | One major version behind | P2 — Medium | 7.x → 8.x |
| **Major behind (2+)** | Two or more major versions behind | P1 — High | 5.x → 8.x |
| **End of life** | No longer receiving security patches | P1 — High | .NET 7 → .NET 10 |

## Risk Assessment for Outdated Packages

Not all outdated packages are equal. Prioritize by:

```markdown
| Factor | Higher Priority | Lower Priority |
|--------|----------------|----------------|
| Security surface | Auth, crypto, HTTP | Logging, formatting |
| Change frequency | Package updates weekly | Stable, rarely updated |
| Breaking changes | Major version with API changes | Patch with just bug fixes |
| Transitive impact | Many packages depend on it | Leaf dependency |
```

### NexTruzt.io Priority Packages

These packages should always be on the latest stable version:

```markdown
| Package | Reason | Update Frequency |
|---------|--------|-----------------|
| Microsoft.AspNetCore.* | Security patches | Every .NET release |
| Microsoft.Identity.Web | Auth security | Monthly |
| Microsoft.EntityFrameworkCore | Data access security | Every .NET release |
| System.Text.Json | Deserialization DoS | As needed |
| Polly | Resilience patterns | Quarterly |
```

## Automated Monitoring

### GitHub Dependabot Configuration

```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 10
    target-branch: "develop"
    labels:
      - "dependencies"
      - "nuget"
    ignore:
      # Skip major version updates for EF Core (requires migration planning)
      - dependency-name: "Microsoft.EntityFrameworkCore*"
        update-types: ["version-update:semver-major"]
```

### NuGet Audit in .NET 10

.NET 8+ includes built-in NuGet audit on restore:

```xml
<!-- In Directory.Build.props -->
<PropertyGroup>
  <NuGetAudit>true</NuGetAudit>
  <NuGetAuditLevel>moderate</NuGetAuditLevel>
  <NuGetAuditMode>all</NuGetAuditMode> <!-- direct + transitive -->
</PropertyGroup>
```

This runs automatically on `dotnet restore` and fails the build if vulnerable packages are found.

## Report Format

```markdown
## Outdated Packages Report

| Package | Installed | Latest | Behind | Breaking | Priority |
|---------|-----------|--------|--------|----------|----------|
| MediatR | 12.2.0 | 12.4.1 | Minor | No | P2 |
| EF Core | 8.0.0 | 10.0.0 | 2 Major | Yes | P1 |
| Polly | 8.2.0 | 8.5.1 | Minor | No | P2 |
| Serilog | 3.1.0 | 4.2.0 | Major | Yes | P2 |

### Upgrade Plan
1. **[P1]** Upgrade EF Core 8→10 (requires migration — schedule sprint)
2. **[P2]** Upgrade MediatR 12.2→12.4 (non-breaking, safe)
3. **[P2]** Upgrade Polly 8.2→8.5 (non-breaking, safe)
4. **[P2]** Upgrade Serilog 3→4 (breaking — review changelog)
```
