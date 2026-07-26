---
name: deployment-preflight
description: "Pre-deployment verification covering build, tests, migrations, security, and rollback readiness. Triggers: deploy, release, preflight, go/no-go"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: devops
  triggers: deploy, release, preflight, go/no-go, deployment checklist, release readiness
  role: release-engineer
  scope: analysis
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: ci-cd-builder, schema-reviewer
---

# Deployment Preflight — Pre-deploy verification for NexTruzt.io escrow platform

## When to Use

- Before deploying to staging or production (any environment with real traffic)
- As a CI/CD release gate or manual go/no-go decision point
- When the release includes database migrations or schema changes
- After a hotfix to verify no regressions before emergency deploy
- When onboarding a new deployment target or infrastructure change

## Core Workflow

### Step 1 — Build & Artifact Verification

Run `dotnet build --no-incremental -c Release /p:TreatWarningsAsErrors=true` and `dotnet restore --locked-mode`.

- [ ] Zero errors, zero warnings; Release mode with optimizations
- [ ] All packages restored from approved feeds; artifact contents verified

✅ **Checkpoint:** Build artifact hash matches CI pipeline output.

### Step 2 — Test Suite Verification

Run `dotnet test --no-build -c Release --collect:"XPlat Code Coverage"`.

- [ ] Unit + integration tests: 100% pass rate, coverage ≥ threshold
- [ ] No unexplained skipped tests; smoke test suite ready for post-deploy

✅ **Checkpoint:** All test suites green; coverage report archived.

### Step 3 — Database Migration Safety

Validate EF Core migrations are backward-compatible and rollback-safe. See [Database Migration Check](references/database-migration-check.md).

- [ ] Migration applies cleanly on disposable clone; backward compatible
- [ ] `Down()` rollback tested; no unsafe DROP operations without compat period
- [ ] Estimated runtime within maintenance window

✅ **Checkpoint:** Migration + rollback verified on staging clone.

### Step 4 — Configuration & Environment Readiness

See [Configuration Validation](references/configuration-validation.md), [Environment Verification](references/environment-verification.md), and [Health Checks](references/health-checks.md).

- [ ] All required config keys set; no placeholders (`TODO`, `CHANGEME`)
- [ ] Secrets valid and not expired; feature flags correct for this release
- [ ] All dependencies reachable (DB, cache, APIs); health endpoints responding

✅ **Checkpoint:** All endpoints reachable; config audit clean.

### Step 5 — Security & Compliance

Run `dotnet list package --vulnerable --include-transitive`.

- [ ] No critical/high CVEs; SAST clean; container image scanned
- [ ] No leaked secrets; API contract has no breaking changes
- [ ] License compliance verified for production dependencies

✅ **Checkpoint:** Security scan reports archived; zero blockers.

### Step 6 — Rollback Readiness

- [ ] Runbook documented; previous artifact tagged and accessible
- [ ] DB rollback tested; rollback time within SLA
- [ ] Trigger criteria and escalation contacts confirmed

✅ **Checkpoint:** Rollback rehearsed on staging; runbook reviewed.

## Reference Guide

| Reference | Load When | Key Topics |
|---|---|---|
| [Health Checks](references/health-checks.md) | ASP.NET health check patterns | Liveness, readiness, startup probes, custom checks |
| [Configuration Validation](references/configuration-validation.md) | Config validation at startup | Options validation, required settings, environment checks |
| [Database Migration Check](references/database-migration-check.md) | Pending migration detection | EF Core migration status, backward compat, rollback |
| [Environment Verification](references/environment-verification.md) | Environment readiness checks | Connectivity, secrets, feature flags, dependencies |

## Quick Reference

```
PREFLIGHT: Build ✓ Tests ✓ Migration ✓ Config ✓ Security ✓ API ✓ Rollback ✓ Health ✓ → GO/NO-GO
```

## Constraints

**MUST DO:** Run every check; mark each PASS/FAIL/SKIP (justify skips); block on any critical FAIL; verify migration backward compat; test rollback in non-prod; include go/no-go recommendation; adapt to deployment type.

**MUST NOT:** Approve when critical checks fail; skip security scans for "minor" changes; assume config is correct without verification; approve unsafe DROP migrations without compat period; skip rollback rehearsal.

## Output Template

```markdown
## Preflight Report — {Project} v{version}
**Target**: {Staging|Production} | **Type**: {Release|Hotfix|Config|Infra} | **Date**: {date} | **Engineer**: {name}

| # | Check | Status | Details |
|---|---|---|---|
| 1 | Build (Release) | ✅ PASS | 0 errors, 0 warnings |
| 2 | Unit tests | ✅ PASS | {n}/{n}, {n}% coverage |
| 3 | Integration tests | ✅ PASS | {n}/{n} passed |
| 4 | Migration | ✅ PASS | Applied + rollback OK |
| 5 | Config & secrets | ✅ PASS | {n}/{n} vars, no placeholders |
| 6 | Security scan | ✅ PASS | 0 critical CVEs |
| 7 | API compat | ✅ PASS | 0 breaking changes |
| 8 | Health checks | ✅ PASS | All probes responding |
| 9 | Rollback ready | ✅ PASS | Rehearsed in {n} min |

**Blockers**: {None or list with required action}

| Recommendation | 🟢 GO / 🔴 NO-GO | Risk: Low/Med/High |
|---|---|---|
| **Justification** | {explanation} | Approved by: {name} |
```
