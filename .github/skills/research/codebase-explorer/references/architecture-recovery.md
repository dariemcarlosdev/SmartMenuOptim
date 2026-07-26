# Architecture Recovery

Techniques for recovering architectural intent from existing code.

## Architecture Style Detection

### Clean Architecture / Onion

**Indicators:**
- Projects named `*.Domain`, `*.Application`, `*.Infrastructure`, `*.Web`
- Domain project has zero external package references
- Application references Domain only
- Infrastructure references Application (and Domain transitively)
- Interfaces defined in Domain, implementations in Infrastructure

```xml
<!-- Verify dependency direction in .csproj files -->
<!-- Domain.csproj — should have NO ProjectReference -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <!-- Only domain-level packages like FluentValidation.Abstractions -->
  </ItemGroup>
</Project>

<!-- Application.csproj — references Domain only -->
<ItemGroup>
  <ProjectReference Include="..\Domain\Domain.csproj" />
</ItemGroup>
```

### Vertical Slice

**Indicators:**
- Feature folders containing handler + model + validator together
- Minimal cross-feature dependencies
- MediatR handlers grouped by feature, not by type

```
Features/
├── CreateEscrow/
│   ├── CreateEscrowCommand.cs
│   ├── CreateEscrowHandler.cs
│   ├── CreateEscrowValidator.cs
│   └── CreateEscrowResponse.cs
├── ReleaseEscrow/
│   ├── ReleaseEscrowCommand.cs
│   └── ReleaseEscrowHandler.cs
```

### N-Tier / Layered

**Indicators:**
- Controllers → Services → Repositories pattern
- Service classes with business logic (not in domain entities)
- Repository interfaces and implementations in same project
- No clear domain model separation

## DI Registration Analysis

DI registrations reveal the runtime architecture:

```csharp
// Scan Program.cs or DI extension methods
grep -rn "services.Add" --include="*.cs"
grep -rn "builder.Services" --include="*.cs"

// Map interface → implementation bindings
// Pattern: services.AddScoped<IService, ServiceImpl>()
grep -rn "AddScoped\|AddTransient\|AddSingleton" --include="*.cs"
```

### What DI Tells You

| Registration Pattern | Architectural Signal |
|---------------------|---------------------|
| `AddScoped<IRepo, EfRepo>` | Repository pattern, EF Core data access |
| `AddMediatR(cfg => ...)` | CQRS/Mediator pattern |
| `AddDbContext<AppDbContext>` | EF Core, identifies the data layer |
| `AddAuthentication().AddMicrosoftIdentityWebApp()` | Entra ID auth |
| `AddScoped<IPipelineBehavior<,>, ValidationBehavior<,>>` | MediatR pipeline with FluentValidation |

## Layer Violation Detection

```bash
# Domain should NOT reference Infrastructure
grep -rn "using.*Infrastructure" Domain/ --include="*.cs"

# Domain should NOT reference ASP.NET
grep -rn "using Microsoft.AspNetCore" Domain/ --include="*.cs"

# Application should NOT reference EF Core directly
grep -rn "using Microsoft.EntityFrameworkCore" Application/ --include="*.cs"
```

## Architecture Documentation Template

```
Architecture: {Clean Architecture | Vertical Slice | N-Tier}
Evidence: {list of structural indicators found}

Layer Map:
  Presentation  → {project(s)} → {responsibility}
  Application   → {project(s)} → {responsibility}
  Domain        → {project(s)} → {responsibility}
  Infrastructure→ {project(s)} → {responsibility}

Dependency Direction: {correct / N violations found}
Violations: {list violations with file paths}
```

## Pattern Confidence Levels

| Confidence | Meaning | Criteria |
|-----------|---------|----------|
| **Confirmed** | Pattern clearly implemented | Multiple files, consistent naming, DI registered |
| **Likely** | Strong indicators present | Naming conventions match but incomplete implementation |
| **Suspected** | Partial evidence | Some files suggest pattern but inconsistent |
| **Absent** | Not found | No evidence in codebase |
