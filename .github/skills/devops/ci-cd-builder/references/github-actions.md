# GitHub Actions Reference

## Workflow Triggers

```yaml
on:
  push:
    branches: [main, develop]
    paths-ignore: ['docs/**', '*.md']
  pull_request:
    branches: [main]
    types: [opened, synchronize, reopened]
  workflow_dispatch:
    inputs:
      environment:
        description: 'Target environment'
        required: true
        type: choice
        options: [staging, production]
  schedule:
    - cron: '0 6 * * 1' # Weekly Monday 6am UTC
```

**Trigger guidance:**
- Use `push` + `pull_request` for CI; add `workflow_dispatch` for manual runs
- Use `paths` / `paths-ignore` to skip irrelevant changes (docs, markdown)
- Use `schedule` for nightly builds, dependency audits, or stale cache cleanup
- Avoid `pull_request_target` with code checkout — script injection risk

## Reusable Workflows

Define shared pipeline logic with `workflow_call`:

```yaml
# .github/workflows/reusable-dotnet-ci.yml
name: Reusable .NET CI
on:
  workflow_call:
    inputs:
      dotnet-version:
        required: true
        type: string
      configuration:
        required: false
        type: string
        default: 'Release'
    secrets:
      NUGET_AUTH_TOKEN:
        required: false

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    timeout-minutes: 15
    steps:
      - uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683 # v4
      - uses: actions/setup-dotnet@3e891b0cb619bf60e2c25674b222b8940e2c1c25 # v4
        with:
          dotnet-version: ${{ inputs.dotnet-version }}
      - run: dotnet restore
      - run: dotnet build --no-restore -c ${{ inputs.configuration }}
      - run: dotnet test --no-build -c ${{ inputs.configuration }} --logger "trx"
```

Caller workflow:

```yaml
jobs:
  ci:
    uses: ./.github/workflows/reusable-dotnet-ci.yml
    with:
      dotnet-version: '10.0.x'
    secrets: inherit
```

## Composite Actions

Extract repeated step sequences into reusable actions:

```yaml
# .github/actions/dotnet-setup/action.yml
name: Setup .NET with Cache
description: Install .NET SDK and restore NuGet cache
inputs:
  dotnet-version:
    required: true
runs:
  using: composite
  steps:
    - uses: actions/setup-dotnet@3e891b0cb619bf60e2c25674b222b8940e2c1c25 # v4
      with:
        dotnet-version: ${{ inputs.dotnet-version }}
    - uses: actions/cache@5a3ec84eff668545956fd18022155c47e93e2684 # v4
      with:
        path: ~/.nuget/packages
        key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
        restore-keys: nuget-${{ runner.os }}-
    - run: dotnet restore
      shell: bash
```

## Matrix Builds

Test across multiple .NET versions or OS targets:

```yaml
strategy:
  fail-fast: false
  matrix:
    dotnet-version: ['8.0.x', '9.0.x', '10.0.x']
    os: [ubuntu-latest, windows-latest]
runs-on: ${{ matrix.os }}
steps:
  - uses: actions/setup-dotnet@3e891b0cb619bf60e2c25674b222b8940e2c1c25 # v4
    with:
      dotnet-version: ${{ matrix.dotnet-version }}
```

Use `fail-fast: false` to run all combinations even if one fails. Use `include` / `exclude` to add or remove specific combinations.

## Concurrency Groups

Prevent parallel runs for the same branch or environment:

```yaml
concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: ${{ github.ref != 'refs/heads/main' }}
```

For deployment jobs, use environment-scoped groups:

```yaml
concurrency:
  group: deploy-${{ inputs.environment }}
  cancel-in-progress: false  # Never cancel in-progress deployments
```

## OIDC Authentication

Authenticate to Azure or AWS without long-lived secrets:

```yaml
permissions:
  id-token: write
  contents: read

steps:
  - uses: azure/login@a457da9ea143d694b1b9c7c869ebb04ebe844ef5 # v2
    with:
      client-id: ${{ secrets.AZURE_CLIENT_ID }}
      tenant-id: ${{ secrets.AZURE_TENANT_ID }}
      subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

**OIDC requirements:**
- Configure federated credentials in your cloud provider (Azure App Registration, AWS IAM)
- Set `id-token: write` permission — required for token exchange
- No client secrets needed — tokens are short-lived and scoped to the workflow run

## Caching with actions/cache

```yaml
- uses: actions/cache@5a3ec84eff668545956fd18022155c47e93e2684 # v4
  with:
    path: ~/.nuget/packages
    key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      nuget-${{ runner.os }}-
```

**Best practices:**
- Use lock file hashes as primary cache key
- Include `runner.os` for cross-platform builds
- Set `restore-keys` for partial cache hits (prefix match)
- Cache size limit is 10 GB per repository — prune stale caches periodically

## Complete .NET Workflow Example

```yaml
name: .NET CI/CD
on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]
  workflow_dispatch:

permissions:
  contents: read
  checks: write
  pull-requests: write
  id-token: write

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: ${{ github.ref != 'refs/heads/main' }}

jobs:
  build-and-test:
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

      - run: dotnet restore
      - run: dotnet build --no-restore -c Release /p:ContinuousIntegrationBuild=true

      - run: >
          dotnet test --no-build -c Release
          --logger "trx;LogFileName=results.trx"
          --collect:"XPlat Code Coverage"
          -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

      - uses: dorny/test-reporter@6e6a65b7a0bd2c9197df7d0ae36ac5cee784230c # v1
        if: always()
        with:
          name: Test Results
          path: '**/*.trx'
          reporter: dotnet-trx

      - uses: actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02 # v4
        if: always()
        with:
          name: coverage-report
          path: '**/coverage.cobertura.xml'

  deploy-staging:
    needs: build-and-test
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    runs-on: ubuntu-latest
    timeout-minutes: 10
    environment: staging
    steps:
      - uses: actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683 # v4

      - uses: azure/login@a457da9ea143d694b1b9c7c869ebb04ebe844ef5 # v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - run: |
          dotnet publish -c Release -o ./publish
          az webapp deploy --resource-group ${{ vars.RESOURCE_GROUP }} \
            --name ${{ vars.APP_NAME }} --src-path ./publish
```
