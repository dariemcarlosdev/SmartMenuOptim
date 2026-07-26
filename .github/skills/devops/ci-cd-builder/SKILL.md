---
name: ci-cd-builder
description: "Create and optimize CI/CD pipelines with multi-stage builds, caching, testing, and deployment. Triggers: CI/CD, pipeline, GitHub Actions, workflow"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: devops
  triggers: CI/CD, pipeline, GitHub Actions, workflow, build automation, deploy
  role: devops-engineer
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: code
  related-skills: deployment-preflight, docker-builds
---

# CI/CD Pipeline Builder — Build, test, package, and deploy .NET applications with hardened pipelines.

## When to Use

- Creating a CI/CD pipeline for a new .NET / Blazor Server project
- Optimizing slow pipelines with caching, parallelism, or matrix builds
- Adding deployment stages with environment protection and OIDC auth
- Migrating between CI/CD platforms (GitHub Actions ↔ Azure Pipelines)
- Integrating security scanning, code coverage, or quality gates
- Setting up multi-stage Docker builds for containerized deployments

## Core Workflow

### 1 — Gather Context

Before generating a pipeline, determine: language/runtime, package manager, test framework, deployment target, branch strategy, environment count, and secret requirements.

### 2 — Design Pipeline Stages

```
┌─────────┐    ┌──────────┐    ┌───────────┐    ┌──────────┐    ┌──────────┐
│  Build   │───▶│   Test   │───▶│  Analyze  │───▶│  Package  │───▶│  Deploy  │
└─────────┘    └──────────┘    └───────────┘    └──────────┘    └──────────┘
```

- **Build** — Checkout, setup SDK (pinned version), restore with cache, compile, upload artifacts
- **Test** — Unit tests with coverage, integration tests with service containers, publish TRX/JUnit XML
  - ✅ Coverage threshold met
- **Analyze** — Static analysis (CodeQL/SonarQube), dependency audit, SARIF upload
  - ✅ No critical/high vulnerabilities
- **Package** — Docker image or NuGet package, tag with commit SHA, push to registry
  - ✅ Image scanned with Trivy
- **Deploy** — OIDC auth, deploy to environment, smoke test, rollback on failure
  - ✅ Health check passes post-deploy

### 3 — Configure Caching

| Stack | Cache Key | Cache Path |
|---|---|---|
| .NET / NuGet | `hashFiles('**/*.csproj')` | `~/.nuget/packages` |
| Node.js / npm | `hashFiles('**/package-lock.json')` | `~/.npm` |
| Docker layers | Dockerfile + context hash | Docker buildx cache |

Use lock file hashes as cache keys. Include OS in key for cross-platform builds. Set fallback restore keys for partial hits.

### 4 — Wire Up Environments

| Environment | Trigger | Approval | Strategy |
|---|---|---|---|
| Development | Push to `develop` | None | Direct deploy |
| Staging | Push to `main` | Optional | Blue/green |
| Production | Tag or manual dispatch | Required reviewers | Blue/green + rollback |

## Reference Guide

| Reference | Load When | Key Topics |
|---|---|---|
| [GitHub Actions](references/github-actions.md) | Workflow syntax, reusable workflows | YAML syntax, composite actions, matrix builds, OIDC |
| [Azure Pipelines](references/azure-pipelines.md) | Azure DevOps pipeline patterns | Stages, variable groups, service connections, templates |
| [.NET CI](references/dotnet-ci.md) | .NET build/test/publish in CI | dotnet CLI, NuGet cache, test reporting, coverage |
| [Docker Builds](references/docker-builds.md) | Multi-stage Docker builds | Dockerfile patterns, layer caching, security scanning |

## Quick Reference — .NET GitHub Actions Workflow

```yaml
name: CI
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:

permissions:
  contents: read
  checks: write

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
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release --logger "trx" --collect:"XPlat Code Coverage"
      - uses: dorny/test-reporter@6e6a65b7a0bd2c9197df7d0ae36ac5cee784230c # v1
        if: always()
        with:
          name: Test Results
          path: '**/*.trx'
          reporter: dotnet-trx
```

## Constraints

### MUST DO

- Pin all action versions to full SHA — never use tags alone
- Use exact SDK versions via `global.json` — never `latest`
- Set `permissions` block with least-privilege scope on every workflow
- Use OIDC for cloud deployments — no long-lived service account keys
- Add `concurrency` groups to prevent parallel deploys to the same environment
- Include `timeout-minutes` on every job
- Separate secrets per environment using GitHub Environments
- Produce test results in publishable format (TRX or JUnit XML)
- Include `workflow_dispatch` trigger for on-demand runs
- Scan container images before pushing to registry

### MUST NOT

- Store secrets in workflow files or repository code
- Use `actions/checkout@main` or unpinned action references
- Skip test stages on deployment branches
- Deploy to production without staging verification
- Use `pull_request_target` with code checkout (script injection risk)
- Grant `write-all` permissions — use minimum required
- Hardcode environment-specific values — use variables or secrets

## Output Template

```yaml
# .github/workflows/ci-cd.yml
name: CI/CD Pipeline
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

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: ${{ github.ref != 'refs/heads/main' }}

jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 15
    steps:
      # Checkout, setup SDK, restore with cache, build, upload artifacts

  test:
    needs: build
    runs-on: ubuntu-latest
    timeout-minutes: 15
    steps:
      # Download artifacts, run tests, publish results, upload coverage

  analyze:
    needs: build
    runs-on: ubuntu-latest
    timeout-minutes: 10
    steps:
      # CodeQL analysis, dependency audit, SARIF upload

  deploy-staging:
    needs: [test, analyze]
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    timeout-minutes: 10
    environment: staging
    steps:
      # OIDC auth, deploy, smoke test

  deploy-production:
    needs: deploy-staging
    runs-on: ubuntu-latest
    timeout-minutes: 10
    environment: production
    steps:
      # OIDC auth, deploy, health check, rollback on failure
```
