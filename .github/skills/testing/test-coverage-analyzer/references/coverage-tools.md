# Coverage Tools — Coverlet, ReportGenerator, and Stryker Setup

## Purpose

Guide the setup and configuration of code coverage tools for .NET projects, from collection to reporting to mutation testing.

## Tool 1: Coverlet (Coverage Collection)

Coverlet is the standard cross-platform code coverage library for .NET.

### Installation

```bash
# Global tool (for CLI usage)
dotnet tool install --global coverlet.console

# Package reference (recommended — per-project)
dotnet add tests/EscrowApp.Application.Tests package coverlet.collector
```

### Running Coverage

```bash
# Collect coverage during test run
dotnet test --collect:"XPlat Code Coverage"

# Output: TestResults/{guid}/coverage.cobertura.xml

# With specific format
dotnet test --collect:"XPlat Code Coverage" -- \
  DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

# Multiple formats
dotnet test --collect:"XPlat Code Coverage" -- \
  DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=json,cobertura
```

### Configuration via runsettings

```xml
<!-- coverage.runsettings -->
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura</Format>
          <Exclude>[*Tests*]*,[*TestUtils*]*</Exclude>
          <Include>[EscrowApp.*]*</Include>
          <ExcludeByAttribute>
            GeneratedCodeAttribute,ExcludeFromCodeCoverageAttribute
          </ExcludeByAttribute>
          <SingleHit>false</SingleHit>
          <UseSourceLink>true</UseSourceLink>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

```bash
# Use runsettings file
dotnet test --settings coverage.runsettings
```

### Excluding Code from Coverage

```csharp
// Attribute-based exclusion (for code that shouldn't be covered)
[ExcludeFromCodeCoverage]
public static class DependencyInjection { }

// Use sparingly — only for:
// - Auto-generated code
// - DI registration code (tested via integration tests)
// - Simple DTOs with no logic
```

## Tool 2: ReportGenerator (HTML Reports)

### Installation

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
```

### Generating Reports

```bash
# Basic HTML report
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage/report" \
  -reporttypes:Html

# Multiple formats
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage/report" \
  -reporttypes:"Html;Cobertura;TextSummary;Badges"

# With history tracking (shows trends over time)
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage/report" \
  -historydir:"coverage/history" \
  -reporttypes:Html
```

### CI Pipeline Integration

```yaml
# GitHub Actions
- name: Run tests with coverage
  run: dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

- name: Generate coverage report
  run: |
    dotnet tool install --global dotnet-reportgenerator-globaltool
    reportgenerator \
      -reports:coverage/**/coverage.cobertura.xml \
      -targetdir:coverage/report \
      -reporttypes:Html

- name: Upload coverage report
  uses: actions/upload-artifact@v4
  with:
    name: coverage-report
    path: coverage/report
```

## Tool 3: Stryker.NET (Mutation Testing)

### Installation

```bash
dotnet tool install --global dotnet-stryker
```

### Running Mutation Tests

```bash
# From test project directory
cd tests/EscrowApp.Application.Tests

# Basic run
dotnet stryker --project EscrowApp.Application.csproj

# With configuration
dotnet stryker \
  --project EscrowApp.Application.csproj \
  --reporters "html,progress" \
  --threshold-high 80 \
  --threshold-low 60 \
  --threshold-break 50
```

### Stryker Configuration File

```json
// stryker-config.json
{
  "stryker-config": {
    "project": "EscrowApp.Application.csproj",
    "reporters": ["html", "progress", "dashboard"],
    "threshold-high": 80,
    "threshold-low": 60,
    "threshold-break": 50,
    "mutate": [
      "src/EscrowApp.Application/**/*.cs",
      "!src/EscrowApp.Application/DependencyInjection.cs"
    ],
    "ignore-mutations": [
      "string",
      "linq"
    ]
  }
}
```

### Interpreting Mutation Scores

| Score | Rating | Action |
|-------|--------|--------|
| 80%+ | ✅ Strong | Tests catch most mutations |
| 60–79% | ⚠️ Moderate | Review surviving mutants for missing tests |
| < 60% | ❌ Weak | Tests are not verifying behavior effectively |

## Complete Coverage Pipeline Script

```bash
#!/bin/bash
# coverage.sh — Run full coverage pipeline

echo "🧪 Running tests with coverage..."
dotnet test --collect:"XPlat Code Coverage" \
  --results-directory ./coverage \
  --settings coverage.runsettings

echo "📊 Generating HTML report..."
reportgenerator \
  -reports:"coverage/**/coverage.cobertura.xml" \
  -targetdir:"coverage/report" \
  -reporttypes:"Html;TextSummary;Badges"

echo "📈 Coverage Summary:"
cat coverage/report/Summary.txt

echo "🔬 Running mutation tests on Domain layer..."
cd tests/EscrowApp.Domain.Tests
dotnet stryker --project EscrowApp.Domain.csproj --reporters "html,progress"
```
