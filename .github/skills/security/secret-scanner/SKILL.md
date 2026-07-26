---
name: secret-scanner
description: "Detect exposed secrets, API keys, tokens, and credentials across the codebase — triggered by 'scan for secrets', 'find exposed keys', 'credential check'"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: security
  triggers: scan for secrets, find exposed keys, credential check, secret scan, find passwords, leaked credentials, check for secrets, API key scan
  role: reviewer
  scope: analysis
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: owasp-audit, threat-modeler, code-reviewer
---

# Secret Scanner

A secret detection skill that scans source code, config files, and committed artifacts for exposed credentials, API keys, tokens, and connection strings — using pattern matching against known secret formats with contextual analysis to separate genuine exposures from false positives.

## When to Use This Skill

- "Scan for secrets in this repo"
- "Are there any exposed API keys?"
- "Check for leaked credentials"
- "Security check before making repo public"
- Before open-sourcing a private repository
- During security compliance audits

## Core Workflow

1. **Scan Source Files** — Pattern-match all files against known secret formats. Load `references/secret-patterns.md` for the full pattern catalog (AWS keys, Azure connection strings, GitHub tokens, Stripe keys, etc.).
   - **Checkpoint:** All files scanned, raw matches collected with file:line locations.

2. **Check Configuration Files** — Specifically scan `appsettings.json`, `.env`, `docker-compose.yml`, `launchSettings.json`, CI/CD pipelines, `nuget.config`. Load `references/secret-patterns.md` for Azure-specific patterns.
   - **Checkpoint:** All config files audited for inline secrets.

3. **Verify .gitignore Coverage** — Confirm secret-bearing file types (`.env`, `*.pfx`, `*.pem`, `secrets.json`) are excluded. Load `references/gitignore-verification.md` for the complete exclusion checklist.
   - **Checkpoint:** .gitignore gaps documented.

4. **Assess & Classify** — For each finding, determine confidence (High/Medium/Low) and severity (Critical/High/Medium/Low). Filter false positives (test fixtures, placeholders, GUIDs).
   - **Checkpoint:** All findings classified before report generation.

5. **Remediate** — For each confirmed finding, provide rotation steps and secure storage migration. Load `references/remediation-playbook.md` for rotation procedures and `references/vault-integration.md` for Key Vault setup.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Secret Patterns | `references/secret-patterns.md` | Scanning for secret types |
| Gitignore Verification | `references/gitignore-verification.md` | Checking file exclusions |
| Vault Integration | `references/vault-integration.md` | Key Vault, user-secrets setup |
| Remediation Playbook | `references/remediation-playbook.md` | Secret found, need to rotate |

## Quick Reference

```json
// ❌ FINDING: Hardcoded secret in appsettings.json
{
  "ConnectionStrings": {
    "Default": "Server=prod;Database=App;Password=P@ssw0rd123;"
  },
  "Stripe": { "SecretKey": "sk_live_abcdef..." }
}

// ✅ SECURE: Key Vault + Managed Identity
{
  "KeyVault": { "VaultUri": "https://myapp-kv.vault.azure.net/" }
}
```

| Confidence | Meaning |
|-----------|---------|
| **High** | Matches known format AND credential context |
| **Medium** | Matches pattern but could be placeholder/test |
| **Low** | Generic pattern — may be hash, ID, or sample |

## Constraints

### MUST DO
- Scan ALL files in scope — including binaries, configs, and scripts
- Report exact file path and line number of each finding
- Classify by confidence (High/Medium/Low) and severity
- Provide specific remediation steps for each finding
- Check `.gitignore` for secret file exclusion
- Redact actual secret values — show only first 4 chars + `...`
- Note which secrets need immediate rotation
- Verify example/template files use placeholder values

### MUST NOT
- Do not display full secret values — always redact
- Do not ignore dev/test secrets — they often work in production
- Do not skip configuration files
- Do not report known false positives (GUIDs, `test`/`example` values)
- Do not recommend insecure alternatives ("obfuscate the key")
- Do not treat this scan as sufficient — recommend CI/CD tooling too

## Output Template

```markdown
# Secret Scan Report

**Repository:** [name]  |  **Date:** YYYY-MM-DD  |  **Scanner:** AI Secret Scanner

## Executive Summary
- **Total:** N  |  Critical: N  |  High: N  |  Medium: N  |  Low: N
- **Immediate rotation required:** N secrets

## Findings
| # | Severity | Confidence | Type | File | Line | Preview | Remediation |
|---|----------|-----------|------|------|------|---------|-------------|

## .gitignore Assessment
| Pattern | Status | Recommendation |

## Recommendations
1. Immediate: Rotate Critical/High secrets
2. Short-term: Integrate gitleaks/GitHub secret scanning
3. Medium-term: Migrate to Azure Key Vault + Managed Identity
```
