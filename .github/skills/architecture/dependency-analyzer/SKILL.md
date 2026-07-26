---
name: dependency-analyzer
description: "Analyze project dependencies for vulnerabilities, outdated packages, license risks, and unused refs — trigger: check dependencies, audit packages, CVE scan"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: architecture
  triggers: check dependencies, audit packages, CVE scan, outdated packages, license check, unused dependencies
  role: analyst
  scope: analysis
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: architecture-reviewer, design-pattern-advisor
---

# Dependency Analyzer

Analyze project dependencies for security vulnerabilities, outdated packages, license compatibility, and unused references across .NET and other ecosystems.

## When to Use This Skill

- Before a release to ensure no known CVEs ship to production
- During periodic dependency hygiene sweeps (monthly/quarterly)
- When onboarding to a project to understand dependency health
- After a security advisory for a popular package
- Before adding a new dependency to check license/security risk

## Core Workflow

1. **List All Direct Dependencies** — Parse manifests (`.csproj`, `Directory.Packages.props`, `global.json`); record package, version, and constraint type
   - ✅ Checkpoint: Complete dependency inventory with version pins

2. **Scan for CVEs** — Cross-reference against vulnerability databases → See `references/cve-scanning.md`
   - ✅ Checkpoint: All CRITICAL/HIGH CVEs flagged with fix versions

3. **Identify Outdated Packages** — Compare installed vs. latest stable → See `references/outdated-detection.md`
   - ✅ Checkpoint: Staleness categorized (patch/minor/major behind)

4. **Audit License Compatibility** — Check SPDX identifiers against project license → See `references/license-audit.md`
   - ✅ Checkpoint: All copyleft/unknown licenses flagged

5. **Plan Upgrades** — Prioritized action plan with specific commands and breaking change notes → See `references/upgrade-strategies.md`

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| CVE Scanning | `references/cve-scanning.md` | Checking for CVEs and security advisories |
| License Audit | `references/license-audit.md` | License compliance checking |
| Outdated Detection | `references/outdated-detection.md` | Finding outdated packages |
| Upgrade Strategies | `references/upgrade-strategies.md` | Planning safe upgrades |

## Quick Reference

```bash
# .NET — Check for vulnerable packages
dotnet list package --vulnerable --include-transitive

# .NET — Check for outdated packages
dotnet list package --outdated

# .NET — List all packages in solution
dotnet list EscrowApp.sln package
```

```xml
<!-- Central Package Management (Directory.Packages.props) -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="MediatR" Version="12.4.1" />
    <PackageVersion Include="FluentValidation" Version="11.11.0" />
  </ItemGroup>
</Project>
```

## Constraints

### MUST DO
- Always check for CVEs — highest-priority analysis
- Identify the ecosystem before running commands
- Report CVSS score and fix version for every CVE found
- Flag dependencies with no license or unknown license as HIGH risk
- Include transitive dependencies in CVE analysis
- Provide specific upgrade commands for each recommendation

### MUST NOT
- Skip `devDependencies` — vulnerable dev tools compromise the build pipeline
- Recommend major version upgrades without noting breaking changes
- Mark a CVE as "low risk" just because CVSS is medium — context matters
- Run `--force` auto-fix commands without explicit user approval

## Output Template

```markdown
# Dependency Analysis Report

**Project:** {name} | **Ecosystem:** {.NET} | **Date:** {date}
**Health:** {🟢|🟡|🔴} | **Dependencies:** {N direct} + {M transitive}

## Summary
- Critical CVEs: {N} | High CVEs: {N}
- Outdated (major): {N} | Unused: {N} | License Issues: {N}

## Security Vulnerabilities
| Package | Installed | CVE | Severity | CVSS | Fix Version |

## Outdated Packages
| Package | Installed | Latest | Behind By | Breaking | Priority |

## License Compatibility
| Package | License | Compatible | Risk |

## Action Plan (Prioritized)
1. **[CRITICAL]** {action with specific command}
2. **[HIGH]** {action}
```
