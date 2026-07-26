# Upgrade Strategies — Planning Safe Dependency Upgrades

## Purpose

Provide a structured approach to upgrading dependencies safely, minimizing risk of breaking changes and production incidents.

## Upgrade Strategy Decision Tree

```
Is it a security fix (CVE)?
├── YES → Upgrade immediately, even if breaking
│         (security > compatibility)
└── NO  → Is it a patch version (x.y.Z)?
          ├── YES → Upgrade freely (low risk)
          └── NO  → Is it a minor version (x.Y.z)?
                    ├── YES → Review changelog, upgrade in batch
                    └── NO  → Major version (X.y.z)
                              → Plan dedicated upgrade sprint
```

## Strategy 1: Patch Upgrades (Low Risk)

```bash
# Safe to batch and apply
dotnet add package Polly --version 8.2.3      # was 8.2.0
dotnet add package Serilog --version 3.1.2    # was 3.1.0
dotnet add package MediatR --version 12.4.1   # was 12.4.0

# With Central Package Management
# Update versions in Directory.Packages.props, then:
dotnet restore
dotnet build
dotnet test
```

**Validation:** Build + full test suite must pass. No further review needed.

## Strategy 2: Minor Upgrades (Medium Risk)

```markdown
Checklist:
1. [ ] Read the changelog / release notes
2. [ ] Check for deprecated APIs you're using
3. [ ] Upgrade one package at a time
4. [ ] Build and run tests after each upgrade
5. [ ] Run integration tests if available
6. [ ] Test critical paths manually (payments, auth)
```

```bash
# Upgrade one at a time, testing between each
dotnet add package FluentValidation --version 11.11.0  # was 11.9.0
dotnet build && dotnet test

dotnet add package MediatR --version 12.4.1  # was 12.2.0
dotnet build && dotnet test
```

## Strategy 3: Major Upgrades (High Risk)

Major version upgrades require dedicated planning:

### Pre-Upgrade Checklist

```markdown
1. [ ] Read the migration guide (if available)
2. [ ] Identify all breaking changes from changelog
3. [ ] Create a dedicated branch: `upgrade/{package}-v{version}`
4. [ ] Inventory all usages of deprecated/removed APIs
5. [ ] Estimate code change effort
6. [ ] Ensure test coverage for affected areas
```

### Example: EF Core Major Upgrade (8.x → 10.x)

```bash
# Step 1: Create upgrade branch
git checkout -b upgrade/efcore-v10

# Step 2: Update all EF Core packages together
dotnet add package Microsoft.EntityFrameworkCore --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 10.0.0

# Step 3: Fix compilation errors (API changes)
# Step 4: Update DbContext configurations for new conventions
# Step 5: Run full test suite
dotnet test --verbosity normal

# Step 6: Test database migrations
dotnet ef database update --project src/EscrowApp.Infrastructure

# Step 7: Run integration tests against real database
dotnet test --filter "Category=Integration"
```

### Common .NET Major Upgrade Pitfalls

| Package | Major Change | Migration Impact |
|---------|-------------|-----------------|
| EF Core | Query translation changes | Silent behavior changes in LINQ queries |
| MediatR | Pipeline behavior API changes | Update all `IPipelineBehavior<,>` implementations |
| FluentValidation | Validator base class changes | Update custom validators |
| AutoMapper | Profile registration changes | Update mapping configurations |
| Serilog | Sink configuration API | Update `appsettings.json` and `Program.cs` |

## Strategy 4: Replacing a Dependency

When upgrading is not possible (abandoned package, license change):

```markdown
1. Identify the dependency's API surface used in your code
2. Find an alternative package with compatible license
3. Create an adapter interface in Application layer
4. Implement the adapter with the new package in Infrastructure
5. Swap the DI registration
6. Remove the old package

Example: Replacing AutoMapper with Mapster
- Create `IMappingService` interface
- Implement with Mapster in Infrastructure
- Update DI registration
- Remove AutoMapper NuGet references
```

## Rollback Strategy

Always have a rollback plan:

```bash
# If upgrade causes issues, revert the package version
git stash  # or git checkout -- Directory.Packages.props
dotnet restore

# For Central Package Management — revert Directory.Packages.props
git diff Directory.Packages.props  # review changes
git checkout -- Directory.Packages.props
dotnet restore
```

## Upgrade Frequency Recommendations

| Category | Frequency | Approach |
|----------|-----------|---------|
| Security patches | Immediately | Automated via Dependabot |
| Patch versions | Monthly | Batch in maintenance window |
| Minor versions | Quarterly | Review + batch upgrade sprint |
| Major versions | Per release cycle | Dedicated upgrade task with testing |
| .NET runtime | Annually (LTS) | Major project milestone |
