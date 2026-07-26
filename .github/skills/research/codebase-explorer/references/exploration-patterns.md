# Exploration Patterns

Systematic patterns for exploring unfamiliar codebases efficiently.

## Top-Down Exploration

Start broad, then drill into detail. Best for greenfield analysis.

```
Step 1: Directory tree (2-3 levels)
Step 2: Configuration files (*.csproj, *.sln, global.json, Directory.Build.props)
Step 3: Entry points (Program.cs, Startup.cs)
Step 4: Core domain entities
Step 5: Data flow from API → Application → Domain → Infrastructure
```

### .NET Solution Scan

```bash
# Solution structure
dotnet sln list

# Project references per project
grep -r "ProjectReference" --include="*.csproj"

# NuGet packages
grep -r "PackageReference" --include="*.csproj" | sort

# Global configuration
cat global.json Directory.Build.props Directory.Packages.props 2>/dev/null
```

## Bottom-Up Exploration

Start from a specific feature, trace upward. Best for targeted investigation.

```
Step 1: Find the feature entry point (controller, page, handler)
Step 2: Trace dependencies downward (services, repositories)
Step 3: Map the data model (entities, DTOs, value objects)
Step 4: Identify cross-cutting concerns (middleware, filters, behaviors)
Step 5: Document the feature's architectural footprint
```

### MediatR Handler Tracing

```csharp
// Find all command/query handlers
// Pattern: IRequestHandler<TRequest, TResponse>
grep -rn "IRequestHandler<" --include="*.cs"

// Trace pipeline behaviors
grep -rn "IPipelineBehavior<" --include="*.cs"

// Map notification handlers
grep -rn "INotificationHandler<" --include="*.cs"
```

## Concentric Exploration

Start from a central module, expand outward in rings.

```
Ring 0: Target module (classes, interfaces, tests)
Ring 1: Direct dependencies (what it imports)
Ring 2: Dependents (what imports it)
Ring 3: Shared infrastructure (DI registrations, configuration)
```

## File Classification Matrix

| Directory Pattern       | Purpose              | Priority |
|------------------------|----------------------|----------|
| `Domain/Entities/`     | Core business model  | High     |
| `Application/Commands/`| Write operations     | High     |
| `Application/Queries/` | Read operations      | High     |
| `Infrastructure/`      | External integrations| Medium   |
| `Web/Controllers/`     | API surface          | Medium   |
| `Web/Pages/`           | Blazor UI            | Medium   |
| `tests/`               | Test coverage map    | Low      |
| `.github/workflows/`   | CI/CD pipeline       | Low      |

## Complexity Heuristics

Identify hot spots without reading every file:

```bash
# Largest files (complexity indicator)
find . -name "*.cs" -exec wc -l {} + | sort -rn | head -20

# Most imports (coupling indicator)
grep -c "^using " *.cs | sort -t: -k2 -rn | head -20

# Most changed files (churn indicator)
git log --format=format: --name-only --since="90 days ago" | sort | uniq -c | sort -rn | head -20
```

## Validation Checkpoint

Before finalizing exploration, verify:

- [ ] All projects in the solution have been cataloged
- [ ] Entry points are identified and documented
- [ ] Architecture style is determined with evidence
- [ ] Key patterns (DI, CQRS, Repository) are identified
- [ ] Dependency direction violations are flagged
