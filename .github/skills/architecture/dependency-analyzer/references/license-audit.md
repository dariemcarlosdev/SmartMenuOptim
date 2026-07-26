# License Audit — Compliance Checking

## Purpose

Identify the license of each dependency and verify compatibility with the project's license to avoid legal risk.

## License Categories

### Permissive Licenses (Generally Safe)

| License | SPDX ID | Obligations | Risk |
|---------|---------|-------------|------|
| MIT | MIT | Include license text | LOW |
| Apache 2.0 | Apache-2.0 | Include license + NOTICE | LOW |
| BSD 2-Clause | BSD-2-Clause | Include license text | LOW |
| BSD 3-Clause | BSD-3-Clause | Include license text | LOW |
| ISC | ISC | Include license text | LOW |

### Weak Copyleft (Use Carefully)

| License | SPDX ID | Obligations | Risk |
|---------|---------|-------------|------|
| LGPL 2.1 | LGPL-2.1-only | Link dynamically; provide source for modifications | MEDIUM |
| LGPL 3.0 | LGPL-3.0-only | Link dynamically; provide source for modifications | MEDIUM |
| MPL 2.0 | MPL-2.0 | File-level copyleft; modified files must stay MPL | MEDIUM |
| EPL 2.0 | EPL-2.0 | Module-level copyleft | MEDIUM |

**Guidance:** Safe for NuGet packages consumed as libraries. Flag if source is being modified and redistributed.

### Strong Copyleft (Requires Legal Review)

| License | SPDX ID | Obligations | Risk |
|---------|---------|-------------|------|
| GPL 2.0 | GPL-2.0-only | Entire derivative work must be GPL | HIGH |
| GPL 3.0 | GPL-3.0-only | Entire derivative work must be GPL | HIGH |
| AGPL 3.0 | AGPL-3.0-only | Network use triggers copyleft | CRITICAL |

**Guidance:** GPL/AGPL packages are generally **incompatible** with proprietary/commercial software like NexTruzt.io. Flag for legal review immediately.

### No License / Unknown

| Status | Risk | Action |
|--------|------|--------|
| No LICENSE file | HIGH | No legal permission to use — remove or request license |
| Custom/proprietary | HIGH | Requires legal review |
| Dual-licensed | MEDIUM | Verify which license applies to your usage |

## .NET License Detection

### Using dotnet CLI

```bash
# List all packages with license info (requires dotnet-project-licenses tool)
dotnet tool install --global dotnet-project-licenses
dotnet-project-licenses --input EscrowApp.sln

# Manual check: inspect NuGet package metadata
dotnet nuget locals global-packages --list
# Then check {package}/{version}/{package}.nuspec for <license> element
```

### Using NuGet.org API

```bash
# Check a specific package license
curl -s "https://api.nuget.org/v3/registration5-gz-semver2/{package}/index.json" | \
  jq '.items[].items[].catalogEntry | {id, version, licenseExpression}'
```

### Common NuGet Package Licenses

```markdown
| Package | License | Risk |
|---------|---------|------|
| MediatR | Apache-2.0 | LOW ✅ |
| FluentValidation | Apache-2.0 | LOW ✅ |
| Polly | BSD-3-Clause | LOW ✅ |
| Serilog | Apache-2.0 | LOW ✅ |
| AutoMapper | MIT | LOW ✅ |
| Newtonsoft.Json | MIT | LOW ✅ |
| EF Core | MIT | LOW ✅ |
| Moq | BSD-3-Clause | LOW ✅ |
| StackExchange.Redis | MIT | LOW ✅ |
```

## NexTruzt.io License Policy

As a **proprietary fintech platform**, NexTruzt.io has these license constraints:

```markdown
✅ ALLOWED: MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, ISC, MS-PL
⚠️ REVIEW: LGPL, MPL, EPL (safe as NuGet reference, review if modified)
❌ BLOCKED: GPL, AGPL (incompatible with proprietary distribution)
❌ BLOCKED: No license / Unknown (no legal permission)
❌ BLOCKED: SSPL (Server Side Public License — used by some databases)
```

## Audit Report Format

```markdown
## License Compatibility Report

| Package | Version | License | Status | Notes |
|---------|---------|---------|--------|-------|
| MediatR | 12.4.1 | Apache-2.0 | ✅ Compliant | |
| SomeLib | 3.1.0 | GPL-3.0 | ❌ BLOCKED | Remove or find alternative |
| OtherLib | 1.0.0 | Unknown | ⚠️ REVIEW | No LICENSE file in repo |

### Action Items
1. **[CRITICAL]** Remove GPL-licensed `SomeLib` — alternatives: {list}
2. **[HIGH]** Contact `OtherLib` maintainer to clarify license
```
