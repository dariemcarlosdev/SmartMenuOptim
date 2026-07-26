# Gitignore Verification

Checklist for verifying `.gitignore` properly excludes secret-bearing files.

## Required Exclusions

### Must Be in .gitignore

| Pattern | Why | Status |
|---------|-----|--------|
| `.env` | Environment variables with secrets | ☐ |
| `.env.*` | Environment-specific secrets (`.env.local`, `.env.production`) | ☐ |
| `*.pfx` | Certificate files with private keys | ☐ |
| `*.p12` | PKCS#12 certificate bundles | ☐ |
| `*.pem` | PEM-encoded private keys | ☐ |
| `*.key` | Private key files | ☐ |
| `secrets.json` | User secrets file | ☐ |
| `credentials.json` | Cloud provider credentials | ☐ |
| `.azure/` | Azure CLI credentials | ☐ |
| `.aws/credentials` | AWS CLI credentials | ☐ |
| `terraform.tfvars` | Terraform variables (often contains secrets) | ☐ |
| `terraform.tfstate` | Terraform state (contains resource details) | ☐ |
| `node_modules/` | NPM packages (may contain .env files) | ☐ |

### Conditional Exclusions

| Pattern | Condition | Recommendation |
|---------|-----------|----------------|
| `appsettings.Development.json` | If it contains real secrets | Use `dotnet user-secrets` instead |
| `appsettings.*.json` | If environment configs have secrets | Use env vars or Key Vault |
| `launchSettings.json` | If it contains API keys in env vars | ⚠️ Tricky — needed for dev, but check values |
| `docker-compose.override.yml` | If it contains passwords | Add to .gitignore, use `.env` file |

## Verification Process

### Step 1: Check .gitignore Exists

```bash
# Verify .gitignore exists at repo root
ls -la .gitignore

# Check for nested .gitignore files
find . -name ".gitignore" -type f
```

### Step 2: Verify Patterns Match

```bash
# Test if a pattern is ignored
git check-ignore -v .env
git check-ignore -v appsettings.Development.json
git check-ignore -v "*.pfx"
```

### Step 3: Check for Already-Committed Secrets

Even if a file is in `.gitignore` now, it may have been committed before:

```bash
# Check if secret files exist in history
git log --all --full-history -- "*.pfx"
git log --all --full-history -- ".env"
git log --all --full-history -- "appsettings.Development.json"

# Check if tracked files match ignore patterns
git ls-files -i --exclude-standard
```

### Step 4: Verify Template Files

Check that example/template config files use placeholder values:

```json
// ✅ GOOD: appsettings.json with placeholders
{
  "ConnectionStrings": {
    "Default": "" // Set via environment variable or Key Vault
  },
  "AzureAd": {
    "ClientId": "YOUR_CLIENT_ID_HERE",
    "TenantId": "YOUR_TENANT_ID_HERE"
  }
}
```

```json
// ❌ BAD: Real values in committed config
{
  "ConnectionStrings": {
    "Default": "Server=prod-db;Password=realP@ss;"
  }
}
```

## Recommended .gitignore for .NET Projects

```gitignore
# Secrets and credentials
.env
.env.*
*.pfx
*.p12
*.pem
*.key
secrets.json
credentials.json

# Cloud CLI credentials
.azure/
.aws/

# Terraform
terraform.tfvars
*.tfstate
*.tfstate.backup

# User secrets (if not using dotnet user-secrets properly)
# Usually stored outside repo in %APPDATA%/Microsoft/UserSecrets/

# IDE and build
*.user
*.suo
.vs/
bin/
obj/

# Node
node_modules/

# OS
Thumbs.db
.DS_Store
```

## History Cleanup

If secrets were found in git history:

1. **BFG Repo-Cleaner** (preferred for large repos):
```bash
java -jar bfg.jar --replace-text passwords.txt repo.git
git reflog expire --expire=now --all && git gc --prune=now --aggressive
```

2. **git filter-repo** (Python-based alternative):
```bash
git filter-repo --invert-paths --path appsettings.Development.json
```

3. **Force push after cleanup:**
```bash
git push --force --all
# Notify all team members to re-clone
```

> ⚠️ **Warning:** History cleanup requires all team members to re-clone. Coordinate carefully.
