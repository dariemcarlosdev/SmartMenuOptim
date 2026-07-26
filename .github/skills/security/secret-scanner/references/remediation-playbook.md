# Secret Remediation Playbook

Step-by-step procedures for when a secret is found exposed in source control.

## Immediate Response (First 15 Minutes)

### 1. Assess Severity

| Question | If Yes |
|----------|--------|
| Is this a production credential? | **Critical** — rotate immediately |
| Can it access customer data? | **Critical** — rotate + audit access logs |
| Is the repo public? | **Critical** — assume compromised NOW |
| Is it a test/dev credential? | **High** — rotate soon (may share infra with prod) |
| Is it a placeholder/example? | **Low** — verify it's not real, then dismiss |

### 2. Rotate the Secret

**Do not wait** — rotate before removing from code. The secret is compromised the moment it enters git history.

#### AWS Access Keys
```bash
# 1. Create new key
aws iam create-access-key --user-name <user>
# 2. Update the application to use new key
# 3. Deactivate old key (don't delete yet — verify app works)
aws iam update-access-key --access-key-id AKIA... --status Inactive
# 4. After 24h verification, delete old key
aws iam delete-access-key --access-key-id AKIA...
```

#### Azure AD Client Secret
```bash
# 1. Create new client secret in App Registration
az ad app credential reset --id <app-id>
# 2. Update Key Vault or app configuration
az keyvault secret set --vault-name nextruzt-kv --name "AzureAd--ClientSecret" --value "<new-secret>"
# 3. Remove old credential from App Registration
```

#### Database Password
```sql
-- 1. Create new login/password
ALTER LOGIN [app_user] WITH PASSWORD = '<new-strong-password>';
-- 2. Update connection string in Key Vault
-- 3. Restart application to pick up new connection string
-- 4. Verify connectivity
```

#### Stripe API Key
1. Go to Stripe Dashboard → Developers → API Keys
2. Roll the secret key (generates new, invalidates old)
3. Update Key Vault: `az keyvault secret set --vault-name nextruzt-kv --name "Stripe--SecretKey" --value "sk_live_new..."`

#### GitHub Token
1. Go to Settings → Developer Settings → Personal Access Tokens
2. Delete the exposed token
3. Generate new token with minimum required scopes
4. Update wherever the token was used

### 3. Audit Access

After rotating, check if the secret was used maliciously:

```bash
# Check AWS CloudTrail for unauthorized access
aws cloudtrail lookup-events --lookup-attributes AttributeKey=AccessKeyId,AttributeValue=AKIA...

# Check Azure sign-in logs
az monitor activity-log list --caller <app-id> --start-time 2024-01-01
```

## Remove from Source Control

### Option A: BFG Repo-Cleaner (Recommended)

```bash
# 1. Create a file with secrets to remove
echo "sk_live_actualkey123" > secrets-to-remove.txt
echo "P@ssw0rd123" >> secrets-to-remove.txt

# 2. Run BFG
java -jar bfg.jar --replace-text secrets-to-remove.txt repo.git

# 3. Clean up
cd repo.git
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# 4. Force push
git push --force --all
git push --force --tags
```

### Option B: git filter-repo

```bash
# Remove entire file from history
git filter-repo --invert-paths --path appsettings.Development.json

# Replace text in all files across history
git filter-repo --replace-text <(echo "regex:sk_live_[A-Za-z0-9]+==>REMOVED")
```

> ⚠️ After history rewrite, ALL team members must re-clone the repository.

## Move to Secure Storage

### Decision Matrix

| Scenario | Recommended Storage |
|----------|-------------------|
| Production Azure app | Azure Key Vault + Managed Identity |
| Local development | `dotnet user-secrets` |
| CI/CD pipeline | GitHub Secrets / Azure DevOps variables |
| Docker containers | `.env` file (in .gitignore) + orchestrator secrets |
| Kubernetes | Sealed Secrets or External Secrets Operator |

## Prevent Reoccurrence

### 1. Pre-commit Hooks

```bash
# Install gitleaks
brew install gitleaks  # macOS
# Or download from https://github.com/gitleaks/gitleaks

# Add pre-commit hook
cat > .git/hooks/pre-commit << 'EOF'
#!/bin/sh
gitleaks protect --staged --verbose
EOF
chmod +x .git/hooks/pre-commit
```

### 2. GitHub Secret Scanning

Enable in repo settings: Settings → Code security → Secret scanning → Enable

### 3. CI/CD Scanning

```yaml
# .github/workflows/secret-scan.yml
name: Secret Scan
on: [push, pull_request]
jobs:
  scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - uses: gitleaks/gitleaks-action@v2
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

## Incident Report Template

```markdown
## Secret Exposure Incident

**Date discovered:** YYYY-MM-DD HH:MM
**Secret type:** [AWS Key / DB Password / API Key / etc.]
**Exposure scope:** [Public repo / Private repo / Branch only]
**Duration exposed:** [First committed date] to [Discovery date]

### Actions Taken
1. [ ] Secret rotated at [time]
2. [ ] Old secret revoked
3. [ ] Access logs audited — [findings]
4. [ ] Secret removed from git history
5. [ ] Moved to secure storage ([Key Vault / user-secrets / etc.])
6. [ ] Pre-commit hook installed
7. [ ] Team notified to re-clone

### Root Cause
[How did the secret get committed?]

### Prevention
[What changes prevent this from happening again?]
```
