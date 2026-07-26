---
name: owasp-audit
description: "Full OWASP Top 10 (2021) security audit with severity ratings and remediation — triggered by 'security audit', 'OWASP check', 'vulnerability scan'"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: security
  triggers: security audit, OWASP check, vulnerability scan, security review, find vulnerabilities, check security, pentest, security assessment
  role: reviewer
  scope: analysis
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: secret-scanner, threat-modeler, code-reviewer
---

# OWASP Security Audit

A comprehensive security audit skill based on the OWASP Top 10 (2021). Evaluates a codebase against all ten risk categories, rates findings by severity, and provides remediation with code examples.

## When to Use This Skill

- "Run a security audit on this codebase"
- "Check for OWASP vulnerabilities"
- "Security review before release"
- "Is this code secure?"
- Before deploying to production
- After adding auth, authorization, or data handling features

## Core Workflow

1. **Map Attack Surface** — Catalog entry points (API endpoints, forms, uploads, webhooks), data flows, trust boundaries, external integrations, and data sensitivity classification.
   - **Checkpoint:** Attack surface summary documented before scanning begins.

2. **Audit OWASP Categories** — Systematically evaluate all 10 categories. Load `references/injection-prevention.md` for A03 (Injection/XSS), `references/broken-auth.md` for A07 (Auth failures), `references/access-control.md` for A01 (Access Control), `references/crypto-failures.md` for A02 (Crypto).
   - **Checkpoint:** Every OWASP category evaluated — mark "N/A" with justification if not applicable.

3. **Rate Severity** — Classify each finding: Critical (exploitable now, data breach/RCE risk), High (moderate effort, significant impact), Medium (specific conditions needed), Low (defense-in-depth).
   - **Checkpoint:** All findings have severity + file path + line number before report.

4. **Generate Remediation** — For each finding: what it is, where it exists, why it matters, how to fix it (with code), and how to verify the fix.

5. **Prioritize** — Order by severity × exploitability. Quick wins (high impact + low effort) first.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Injection Prevention | `references/injection-prevention.md` | SQL injection, XSS, command injection |
| Authentication Issues | `references/broken-auth.md` | Authentication/authorization failures |
| Access Control | `references/access-control.md` | Broken access control (A01) |
| Cryptographic Failures | `references/crypto-failures.md` | Crypto failures, data exposure |

## Quick Reference

```csharp
// A01: Broken Access Control — VULNERABLE
[HttpGet("orders/{id}")]
public async Task<Order> GetOrder(int id) =>
    await _repo.GetByIdAsync(id); // Any user can access any order!

// A01: SECURE — Resource-based authorization
[HttpGet("orders/{id}"), Authorize]
public async Task<IActionResult> GetOrder(int id)
{
    var order = await _repo.GetByIdAsync(id);
    var auth = await _authService.AuthorizeAsync(User, order, "OwnerPolicy");
    return auth.Succeeded ? Ok(order) : Forbid();
}
```

| Severity | Criteria | Example |
|----------|----------|---------|
| **Critical** | Exploitable now, data breach/RCE | SQL injection, exposed credentials |
| **High** | Moderate effort, significant impact | XSS, IDOR, weak passwords |
| **Medium** | Specific conditions, moderate impact | Missing headers, verbose errors |
| **Low** | Defense-in-depth improvement | Missing rate limiting on low-risk endpoints |

## Constraints

### MUST DO
- Audit ALL ten OWASP categories — do not skip any
- Provide specific file paths and line numbers for every finding
- Rate every finding with a severity level
- Include concrete remediation code examples
- Check both application code and configuration files
- Verify auth on every endpoint
- Check for secrets in source code and config files
- Note positive security practices already in place

### MUST NOT
- Do not report theoretical vulnerabilities without code evidence
- Do not suggest security measures that break functionality
- Do not ignore framework protections (Blazor auto-XSS encoding)
- Do not overlook config files (.json, .yaml, .env)
- Do not produce a report without remediation for each finding
- Do not log or display actual secret values — redact them

## Output Template

```markdown
# OWASP Top 10 Security Audit Report

**Application:** [name]  |  **Date:** YYYY-MM-DD  |  **Auditor:** AI Security Auditor

## Executive Summary
- **Total:** N  |  Critical: N  |  High: N  |  Medium: N  |  Low: N
- **Risk posture:** [Critical/High/Medium/Low]
- **Top priority:** [Most urgent finding]

## Attack Surface Summary
| Category | Details |

## Findings by OWASP Category
### A01 — Broken Access Control
| # | Severity | Finding | File | Line | Remediation |
(Repeat for A02-A10)

## Positive Security Observations
## Remediation Priority (ranked by severity × effort)
```
