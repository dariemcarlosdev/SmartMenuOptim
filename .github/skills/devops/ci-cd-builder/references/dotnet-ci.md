# .NET CI Reference

## dotnet CLI Commands for CI

| Command | Purpose | CI Flags |
|---|---|---|
| `dotnet restore` | Restore NuGet packages | `--locked-mode` (enforce lock file) |
| `dotnet build` | Compile the solution | `--no-restore -c Release` |
| `dotnet test` | Run tests | `--no-build --logger trx --collect:"XPlat Code Coverage"` |
| `dotnet publish` | Produce deployable output | `-c Release -o ./publish --no-build` |
| `dotnet pack` | Create NuGet package | `-c Release -o ./nupkgs --no-build` |
| `dotnet nuget push` | Publish to feed | `--source <feed-url> --api-key <key>` |

**Key flags for CI:**
- `--no-restore` / `--no-build` — skip redundant steps when chaining commands
- `-c Release` — always build in Release configuration for CI artifacts
- `--verbosity minimal` — reduce log noise in CI output
- `/p:TreatWarningsAsErrors=true` — fail build on warnings

## NuGet Package Caching

### GitHub Actions

```yaml
- uses: actions/cache@5a3ec84eff668545956fd18022155c47e93e2684 # v4
  with:
    path: ~/.nuget/packages
    key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
    restore-keys: nuget-${{ runner.os }}-
```

### Azure Pipelines

```yaml
- task: Cache@2
  inputs:
    key: 'nuget | "$(Agent.OS)" | **/packages.lock.json'
    restoreKeys: nuget | "$(Agent.OS)"
    path: $(NUGET_PACKAGES)
```

### Lock File Strategy

Enable NuGet lock files for deterministic restores:

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
</PropertyGroup>
```

Then use `dotnet restore --locked-mode` in CI to fail if lock file is out of date. Use `packages.lock.json` hash as the cache key for better cache precision.

## Test Result Formats

### TRX (Visual Studio Test Results)

```bash
dotnet test --logger "trx;LogFileName=results.trx"
```

- Native format for .NET test frameworks (xUnit, NUnit, MSTest)
- Supported by Azure Pipelines `PublishTestResults@2` and `dorny/test-reporter`
- Contains detailed test metadata: duration, stack traces, output

### JUnit XML

```bash
dotnet test --logger "junit;LogFileName=results.xml"
```

Requires the `JunitXml.TestLogger` NuGet package:

```bash
dotnet add package JunitXml.TestLogger
```

- Universal format supported by all CI platforms
- Use when publishing to GitHub Actions, GitLab CI, or Jenkins

### Publishing Results

**GitHub Actions:**

```yaml
- uses: dorny/test-reporter@6e6a65b7a0bd2c9197df7d0ae36ac5cee784230c # v1
  if: always()
  with:
    name: Test Results
    path: '**/*.trx'
    reporter: dotnet-trx
```

**Azure Pipelines:**

```yaml
- task: PublishTestResults@2
  condition: always()
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '**/*.trx'
    mergeTestResults: true
```

## Code Coverage with Coverlet

### Collection

```bash
dotnet test --collect:"XPlat Code Coverage" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

Coverlet is included by default in .NET test project templates via `coverlet.collector`. Output is written to `TestResults/*/coverage.cobertura.xml`.

### Coverage Threshold Enforcement

```bash
# Install reportgenerator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate report and check threshold
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coveragereport" \
  -reporttypes:"HtmlInline_AzurePipelines;Cobertura;TextSummary"
```

For CI enforcement, parse the coverage percentage and fail if below threshold:

```bash
COVERAGE=$(grep -oP 'Line coverage: \K[\d.]+' coveragereport/Summary.txt)
THRESHOLD=80
if (( $(echo "$COVERAGE < $THRESHOLD" | bc -l) )); then
  echo "::error::Coverage ${COVERAGE}% is below threshold ${THRESHOLD}%"
  exit 1
fi
```

### Alternative: Coverlet MSBuild

```xml
<!-- In test project .csproj -->
<PackageReference Include="coverlet.msbuild" Version="6.*" />
```

```bash
dotnet test /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura \
  /p:Threshold=80 \
  /p:ThresholdType=line \
  /p:ThresholdStat=total
```

This approach fails the test command directly when coverage is below the threshold.

## global.json for SDK Version Pinning

Pin the .NET SDK version to ensure consistent builds across developer machines and CI:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestPatch",
    "allowPrerelease": false
  }
}
```

**`rollForward` options:**
- `disable` — exact version only (strictest)
- `latestPatch` — allow patch updates (recommended for CI)
- `latestFeature` — allow feature band updates
- `latestMajor` — allow any newer version (least strict)

Place `global.json` in the repository root. CI runners with `actions/setup-dotnet` will respect it automatically.

## Build Properties for CI

Set MSBuild properties to enable CI-specific optimizations:

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
  <Deterministic>true</Deterministic>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
</PropertyGroup>
```

Or pass via command line:

```bash
dotnet build -c Release /p:ContinuousIntegrationBuild=true
```

**What these do:**
- `ContinuousIntegrationBuild` — normalizes file paths in PDBs for reproducible builds
- `Deterministic` — ensures identical input produces identical output (byte-for-byte)
- `EmbedUntrackedSources` — embeds source files not in version control into the PDB

These are essential for **Source Link** support and NuGet package debugging.

## Complete CI Script Example

```yaml
# GitHub Actions .NET CI with coverage enforcement
name: .NET CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

permissions:
  contents: read
  checks: write

jobs:
  build-test-coverage:
    runs-on: ubuntu-latest
    timeout-minutes: 15
    steps:
      - uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683 # v4

      - uses: actions/setup-dotnet@3e891b0cb619bf60e2c25674b222b8940e2c1c25 # v4
        with:
          dotnet-version: '10.0.x'

      - uses: actions/cache@5a3ec84eff668545956fd18022155c47e93e2684 # v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
          restore-keys: nuget-${{ runner.os }}-

      - name: Restore
        run: dotnet restore --locked-mode

      - name: Build
        run: dotnet build --no-restore -c Release /p:ContinuousIntegrationBuild=true /p:TreatWarningsAsErrors=true

      - name: Test with Coverage
        run: >
          dotnet test --no-build -c Release
          --logger "trx;LogFileName=results.trx"
          --collect:"XPlat Code Coverage"
          -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

      - name: Publish Test Results
        uses: dorny/test-reporter@6e6a65b7a0bd2c9197df7d0ae36ac5cee784230c # v1
        if: always()
        with:
          name: Test Results
          path: '**/*.trx'
          reporter: dotnet-trx

      - name: Coverage Report
        run: |
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator \
            -reports:"**/coverage.cobertura.xml" \
            -targetdir:"coveragereport" \
            -reporttypes:"TextSummary"

      - name: Enforce Coverage Threshold
        run: |
          COVERAGE=$(grep -oP 'Line coverage: \K[\d.]+' coveragereport/Summary.txt)
          echo "Line coverage: ${COVERAGE}%"
          THRESHOLD=80
          if (( $(echo "$COVERAGE < $THRESHOLD" | bc -l) )); then
            echo "::error::Coverage ${COVERAGE}% is below threshold ${THRESHOLD}%"
            exit 1
          fi

      - name: Upload Coverage Artifact
        uses: actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02 # v4
        if: always()
        with:
          name: coverage-report
          path: coveragereport/
```
