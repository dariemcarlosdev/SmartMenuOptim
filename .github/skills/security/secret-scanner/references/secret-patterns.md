# Secret Patterns Catalog

Comprehensive regex patterns and contextual indicators for detecting secrets in source code.

## High-Confidence Patterns

These have distinct formats and rarely produce false positives.

| Secret Type | Regex Pattern | Example (Redacted) |
|---|---|---|
| **AWS Access Key ID** | `AKIA[0-9A-Z]{16}` | `AKIA1234...` |
| **AWS Secret Access Key** | 40-char base64 near `aws_secret_access_key` | `wJalrXUt...` |
| **GitHub PAT** | `ghp_[A-Za-z0-9]{36}` | `ghp_xxxx...` |
| **GitHub OAuth** | `gho_[A-Za-z0-9]{36}` | `gho_xxxx...` |
| **GitHub Fine-Grained** | `github_pat_[A-Za-z0-9_]{82}` | `github_pat_xx...` |
| **Stripe Secret Key** | `sk_live_[A-Za-z0-9]{24,}` | `sk_live_...` |
| **Stripe Publishable** | `pk_live_[A-Za-z0-9]{24,}` | `pk_live_...` |
| **Slack Bot Token** | `xoxb-[0-9]{10,}-[A-Za-z0-9]{24,}` | `xoxb-123...` |
| **Slack Webhook** | `hooks\.slack\.com/services/T[A-Z0-9]+/B[A-Z0-9]+/[A-Za-z0-9]+` | `hooks.slack.com/...` |
| **SendGrid API Key** | `SG\.[A-Za-z0-9_-]{22}\.[A-Za-z0-9_-]{43}` | `SG.xxxx...` |
| **Twilio API Key** | `SK[0-9a-f]{32}` | `SK1234...` |
| **Google API Key** | `AIza[0-9A-Za-z_-]{35}` | `AIzaSy...` |
| **npm Access Token** | `npm_[A-Za-z0-9]{36}` | `npm_xxxx...` |

## Azure-Specific Patterns

| Secret Type | Detection Context |
|---|---|
| **Storage Connection String** | `DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...` |
| **SQL Connection String** | `Server=...;Database=...;Password=...` or `Pwd=...` |
| **Service Bus Connection** | `Endpoint=sb://...;SharedAccessKey=...` |
| **Cosmos DB Key** | `AccountEndpoint=...;AccountKey=...` (base64 key) |
| **AD Client Secret** | Value near `ClientSecret`, `client_secret`, or in `AzureAd` config section |
| **Key Vault Reference** | `@Microsoft.KeyVault(...)` — this is SECURE (verify it's used for secrets) |
| **Managed Identity** | `Authentication=Active Directory Managed Identity` — SECURE pattern |

## General Credential Patterns

| Secret Type | Detection Approach |
|---|---|
| **Private Keys** | `-----BEGIN (RSA\|EC\|OPENSSH) PRIVATE KEY-----` |
| **Certificates** | `.pfx`, `.p12` files committed to repo |
| **JWT Signing Keys** | Long base64 near `JwtSecret`, `SigningKey`, `TokenKey` |
| **Database Passwords** | `Password=`, `Pwd=`, `password:` in connection strings |
| **Basic Auth** | `username:password@` in URLs, `Authorization: Basic` with hardcoded value |
| **OAuth Secrets** | `client_secret`, `ClientSecret` with inline values |
| **SMTP Credentials** | `SmtpPassword`, mail passwords in config |
| **Encryption Keys** | Hex/base64 near `EncryptionKey`, `AesKey`, `Secret` |
| **Webhook Secrets** | `webhook_secret`, `signing_secret` with inline values |

## Configuration File Scan Priority

| File Pattern | What to Look For |
|---|---|
| `appsettings.json` / `appsettings.*.json` | Connection strings, API keys, secret config sections |
| `web.config` / `app.config` | Connection strings, appSettings with credentials |
| `.env` / `.env.*` | Environment variables with secrets |
| `docker-compose.yml` / `Dockerfile` | `ENV` directives with secrets, hardcoded passwords |
| `launchSettings.json` | Environment variables with secrets |
| `*.yaml` / `*.yml` (CI/CD) | Pipeline secrets, Kubernetes secrets |
| `terraform.tfvars` / `*.tf` | Cloud credentials |
| `nuget.config` | Package source credentials |
| `package.json` | Private registry tokens in scripts |

## False Positive Indicators

Skip these patterns to avoid noise:
- Values containing `example`, `test`, `placeholder`, `changeme`, `xxx`, `TODO`
- Standard GUIDs: `[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}`
- Git commit SHAs: 40-character hex strings in git contexts
- Package hashes in lock files (`packages.lock.json`, `yarn.lock`)
- Base64-encoded empty or trivial strings
- Values in test fixture files within `**/tests/**` or `**/*Test*` paths (lower confidence, still report)

## Grep Commands for .NET Projects

```bash
# AWS keys
grep -rn "AKIA[0-9A-Z]\{16\}" --include="*.cs" --include="*.json" --include="*.yml"

# Connection strings with passwords
grep -rn "Password=" --include="*.json" --include="*.config" --include="*.cs"

# Private keys
grep -rn "BEGIN.*PRIVATE KEY" --include="*.pem" --include="*.key" --include="*.cs"

# Stripe keys
grep -rn "sk_live_\|pk_live_" --include="*.cs" --include="*.json"

# Azure connection strings
grep -rn "AccountKey=\|SharedAccessKey=" --include="*.json" --include="*.cs"
```
