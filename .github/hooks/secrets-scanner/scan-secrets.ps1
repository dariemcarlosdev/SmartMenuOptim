#!/usr/bin/env pwsh
#
# Secrets Scanner — Git Pre-Commit Hook (PowerShell)
# Adapted from github/awesome-copilot (MIT License) for Windows/cross-platform.
#
# Scans staged files for hardcoded secrets, credentials, and API keys.
# Blocks the commit if critical/high severity secrets are found.
#
# Environment variables:
#   SCAN_MODE          - "warn" (log only) or "block" (exit non-zero) (default: block)
#   SKIP_SECRETS_SCAN  - "true" to disable scanning entirely
#   SECRETS_ALLOWLIST  - Comma-separated patterns to ignore

param(
    [string]$Mode = $env:SCAN_MODE,
    [string]$AllowlistRaw = $env:SECRETS_ALLOWLIST
)

if ($env:SKIP_SECRETS_SCAN -eq "true") {
    Write-Host "⏭️  Secrets scan skipped (SKIP_SECRETS_SCAN=true)"
    exit 0
}

if (-not $Mode) { $Mode = "block" }

# ---------------------------------------------------------------------------
# Secret detection patterns: Name, Severity, Regex
# Ported from github/awesome-copilot hooks/secrets-scanner
# ---------------------------------------------------------------------------
$Patterns = @(
    # Cloud provider credentials
    @{ Name = "AWS_ACCESS_KEY";        Severity = "critical"; Regex = 'AKIA[0-9A-Z]{16}' }
    @{ Name = "AWS_SECRET_KEY";        Severity = "critical"; Regex = 'aws_secret_access_key\s*[:=]\s*[''"]?[A-Za-z0-9/+=]{40}' }
    @{ Name = "GCP_SERVICE_ACCOUNT";   Severity = "critical"; Regex = '"type"\s*:\s*"service_account"' }
    @{ Name = "GCP_API_KEY";           Severity = "high";     Regex = 'AIza[0-9A-Za-z_-]{35}' }
    @{ Name = "AZURE_CLIENT_SECRET";   Severity = "critical"; Regex = 'azure[_-]?client[_-]?secret\s*[:=]\s*[''"]?[A-Za-z0-9_~.-]{34,}' }

    # GitHub tokens
    @{ Name = "GITHUB_PAT";           Severity = "critical"; Regex = 'ghp_[0-9A-Za-z]{36}' }
    @{ Name = "GITHUB_OAUTH";         Severity = "critical"; Regex = 'gho_[0-9A-Za-z]{36}' }
    @{ Name = "GITHUB_APP_TOKEN";     Severity = "critical"; Regex = 'ghs_[0-9A-Za-z]{36}' }
    @{ Name = "GITHUB_FINE_PAT";      Severity = "critical"; Regex = 'github_pat_[0-9A-Za-z_]{82}' }

    # Private keys
    @{ Name = "PRIVATE_KEY";          Severity = "critical"; Regex = '-----BEGIN (RSA |EC |OPENSSH |DSA |PGP )?PRIVATE KEY-----' }

    # Generic secrets and tokens
    @{ Name = "GENERIC_SECRET";       Severity = "high";     Regex = '(secret|token|password|passwd|pwd|api[_-]?key|apikey|access[_-]?key|auth[_-]?token|client[_-]?secret)\s*[:=]\s*[''"]?[A-Za-z0-9_/+=~.-]{8,}' }
    @{ Name = "CONNECTION_STRING";    Severity = "high";     Regex = '(mongodb(\+srv)?|postgres(ql)?|mysql|redis|amqp|mssql)://[^\s''"]{10,}' }
    @{ Name = "BEARER_TOKEN";         Severity = "medium";   Regex = '[Bb]earer\s+[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}' }

    # SaaS tokens
    @{ Name = "SLACK_TOKEN";          Severity = "high";     Regex = 'xox[baprs]-[0-9]{10,}-[0-9A-Za-z-]+' }
    @{ Name = "SLACK_WEBHOOK";        Severity = "high";     Regex = 'https://hooks\.slack\.com/services/T[0-9A-Z]{8,}/B[0-9A-Z]{8,}/[0-9A-Za-z]{24}' }
    @{ Name = "STRIPE_SECRET_KEY";    Severity = "critical"; Regex = 'sk_live_[0-9A-Za-z]{24,}' }
    @{ Name = "STRIPE_RESTRICTED";    Severity = "high";     Regex = 'rk_live_[0-9A-Za-z]{24,}' }
    @{ Name = "SENDGRID_API_KEY";     Severity = "high";     Regex = 'SG\.[0-9A-Za-z_-]{22}\.[0-9A-Za-z_-]{43}' }
    @{ Name = "TWILIO_API_KEY";       Severity = "high";     Regex = 'SK[0-9a-fA-F]{32}' }
    @{ Name = "NPM_TOKEN";           Severity = "high";     Regex = 'npm_[0-9A-Za-z]{36}' }

    # JWT (structured tokens)
    @{ Name = "JWT_TOKEN";            Severity = "medium";   Regex = 'eyJ[A-Za-z0-9_-]{10,}\.eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}' }
)

# File extensions to scan (text files only)
$TextExtensions = @(
    '.cs', '.razor', '.css', '.js', '.ts', '.json', '.xml', '.yaml', '.yml',
    '.toml', '.ini', '.cfg', '.conf', '.md', '.txt', '.sh', '.ps1', '.bat',
    '.py', '.rb', '.go', '.rs', '.java', '.html', '.sql', '.env', '.resx',
    '.csproj', '.sln', '.props', '.targets', '.config'
)

# Files to always skip
$SkipFiles = @('package-lock.json', 'yarn.lock', 'pnpm-lock.yaml', '*.lock')

# Placeholder patterns to ignore (false positives)
$PlaceholderPattern = '(example|placeholder|your[_-]|xxx|changeme|TODO|FIXME|replace[_-]?me|dummy|fake|test[_-]?key|sample)'

# ---------------------------------------------------------------------------
# Get staged files
# ---------------------------------------------------------------------------
$stagedFiles = git diff --cached --name-only --diff-filter=ACMR 2>$null
if (-not $stagedFiles) {
    Write-Host "✨ No staged files to scan"
    exit 0
}

$files = $stagedFiles -split "`n" | Where-Object { $_.Trim() -ne "" }

# Parse allowlist
$allowlist = @()
if ($AllowlistRaw) {
    $allowlist = $AllowlistRaw -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
}

# ---------------------------------------------------------------------------
# Scan
# ---------------------------------------------------------------------------
$findings = @()

foreach ($filePath in $files) {
    # Skip lock files
    $skip = $false
    foreach ($pattern in $SkipFiles) {
        if ($filePath -like $pattern) { $skip = $true; break }
    }
    if ($skip) { continue }

    # Skip non-text files
    $ext = [System.IO.Path]::GetExtension($filePath).ToLowerInvariant()
    if ($ext -and $ext -notin $TextExtensions) { continue }

    # Read staged content (not working tree — what will actually be committed)
    $content = $null
    try {
        $content = git show ":$filePath" 2>$null
    } catch { continue }
    if (-not $content) { continue }

    $lines = $content -split "`n"

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        foreach ($p in $Patterns) {
            if ($line -match $p.Regex) {
                $matchValue = $Matches[0]

                # Skip placeholders/examples
                if ($matchValue -match $PlaceholderPattern) { continue }

                # Skip allowlisted
                $isAllowed = $false
                foreach ($al in $allowlist) {
                    if ($matchValue -like "*$al*") { $isAllowed = $true; break }
                }
                if ($isAllowed) { continue }

                # Redact for safe display
                if ($matchValue.Length -le 12) {
                    $redacted = "[REDACTED]"
                } else {
                    $redacted = "$($matchValue.Substring(0,4))...$($matchValue.Substring($matchValue.Length-4))"
                }

                $findings += [PSCustomObject]@{
                    File     = $filePath
                    Line     = $i + 1
                    Pattern  = $p.Name
                    Severity = $p.Severity
                    Match    = $redacted
                }
            }
        }
    }
}

# ---------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------
Write-Host "🔍 Scanned $($files.Count) staged file(s) for secrets..."

if ($findings.Count -gt 0) {
    Write-Host ""
    Write-Host "⚠️  Found $($findings.Count) potential secret(s):" -ForegroundColor Yellow
    Write-Host ""
    Write-Host ("  {0,-45} {1,-6} {2,-28} {3}" -f "FILE", "LINE", "PATTERN", "SEVERITY")
    Write-Host ("  {0,-45} {1,-6} {2,-28} {3}" -f "----", "----", "-------", "--------")

    foreach ($f in $findings) {
        $color = switch ($f.Severity) {
            "critical" { "Red" }
            "high"     { "Yellow" }
            default    { "White" }
        }
        Write-Host ("  {0,-45} {1,-6} {2,-28} {3}" -f $f.File, $f.Line, $f.Pattern, $f.Severity) -ForegroundColor $color
    }

    Write-Host ""

    if ($Mode -eq "block") {
        $criticalOrHigh = $findings | Where-Object { $_.Severity -in @("critical", "high") }
        if ($criticalOrHigh.Count -gt 0) {
            Write-Host "🚫 Commit blocked: $($criticalOrHigh.Count) critical/high finding(s). Remove secrets before committing." -ForegroundColor Red
            Write-Host "   Set SCAN_MODE=warn to log without blocking, or add patterns to SECRETS_ALLOWLIST." -ForegroundColor DarkGray
            exit 1
        } else {
            Write-Host "💡 Medium-severity findings detected (not blocking). Review recommended." -ForegroundColor Yellow
        }
    } else {
        Write-Host "💡 Review the findings above. Set SCAN_MODE=block to prevent commits with secrets." -ForegroundColor Yellow
    }
} else {
    Write-Host "✅ No secrets detected in $($files.Count) scanned file(s)" -ForegroundColor Green
}

exit 0
