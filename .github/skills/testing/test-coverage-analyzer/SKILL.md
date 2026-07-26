---
name: test-coverage-analyzer
description: "Find test coverage gaps, prioritize by risk, detect test smells, and generate missing test stubs — trigger: coverage gaps, untested code, test smells, missing tests"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: testing
  triggers: coverage gaps, untested code, test smells, missing tests, assertion quality, test health
  role: expert
  scope: analysis
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: test-generator, tdd-coach
---

# Test Coverage Analyzer

Analyze existing test suites to find coverage gaps, prioritize them by business criticality, detect test smells, and generate test stubs for highest-priority gaps.

## When to Use This Skill

- Before a release to verify critical paths have adequate test coverage
- When inheriting a codebase to understand test health baseline
- When test suite passes but you suspect assertion-free tests (false confidence)
- When prioritizing which tests to write next for maximum risk reduction
- After a production incident to check if the failing path was tested

## Core Workflow

1. **Inventory Test Suite** — Map test files to production code; count tests per class; run coverage tools
   - ✅ Checkpoint: Test-to-production class mapping complete

2. **Identify Coverage Gaps** — Find untested methods, untested branches (error handlers, validation), and untested configuration → See `references/coverage-metrics.md`
   - ✅ Checkpoint: Every public method classified as tested/untested

3. **Detect Test Smells** — Scan for assertion-free tests, brittle tests, duplicate tests, mystery guests → See `references/test-smells.md`
   - ✅ Checkpoint: All test smells cataloged with locations

4. **Audit Assertion Quality** — Check for weak assertions ("not null only"), over-assertion, and missing assertion messages → See `references/assertion-quality.md`
   - ✅ Checkpoint: Each test class rated for assertion strength

5. **Generate Priority Action Plan** — Rank gaps by business criticality; generate test stubs for top gaps → See `references/coverage-tools.md` for tooling setup

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Coverage Metrics | `references/coverage-metrics.md` | Line, branch, mutation coverage |
| Test Smells | `references/test-smells.md` | Fragile tests, test coupling |
| Assertion Quality | `references/assertion-quality.md` | Weak vs strong assertions |
| Coverage Tools | `references/coverage-tools.md` | coverlet, ReportGenerator setup |

## Quick Reference

```bash
# Run tests with coverage collection
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate HTML report
reportgenerator -reports:coverage/**/coverage.cobertura.xml \
  -targetdir:coverage/report -reporttypes:Html

# Quick coverage summary
dotnet test --collect:"XPlat Code Coverage" -- \
  DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

```csharp
// Risk-based priority for NexTruzt.io escrow platform
// CRITICAL: Payment, Auth, State transitions
// HIGH:    Business logic, API endpoints
// MEDIUM:  Validation, mapping, helpers
// LOW:     DTOs, constants, simple getters
```

## Constraints

### MUST DO
- Map tests to the production code they exercise — don't just count test files
- Prioritize gaps by business criticality, not by ease of testing
- Check assertion quality — a test without assertions is worse than no test
- Detect and report test smells — they degrade suite reliability
- Generate compilable/runnable test stubs
- Include clear priority ranking

### MUST NOT
- Treat line coverage percentage as the primary quality metric
- Recommend 100% coverage as a goal — leads to testing trivial code
- Ignore test smells — a smelly suite gives false confidence
- Prioritize utility functions over business-critical paths
- Count test methods without examining what they assert
- Recommend deleting tests without understanding why they exist

## Output Template

```markdown
# Test Coverage Analysis Report

**Project:** {name} | **Date:** {date} | **Health:** {🟢|🟡|🔴}

## Summary
- Production classes: {N} | Test classes: {N} | Ratio: {N:N}
- Line coverage: {%} | Branch coverage: {%}
- Tests with no assertions: {N} | Test smells: {N}

## Coverage Map
| Production Class | Test Class | Tested | Untested | Priority |

## Untested Critical Paths
### 🔴 CRITICAL
1. **{Class.Method}** — Impact: {desc} | Suggested tests: {list}

## Test Smells
| # | Smell | Test | Location | Fix |

## Assertion Quality
| Test Class | Total | No Assert ⚠️ | Weak | Strong ✅ |

## Action Plan
1. **[CRITICAL]** Write tests for {class} — {effort}
2. **[HIGH]** Fix {N} assertion-free tests
```
