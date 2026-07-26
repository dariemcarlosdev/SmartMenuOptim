import { joinSession } from "@github/copilot-sdk/extension";
import { readFileSync } from "node:fs";
import { readFile, readdir, stat } from "node:fs/promises";
import { resolve, extname, join, relative } from "node:path";

// --- Security pattern definitions ---

const CONNECTION_STRING_PATTERNS = [
    { regex: /["'](?:Server|Data Source)\s*=[^"']+(?:Password|Pwd)\s*=[^"']+["']/gi, id: "hardcoded-connstr", category: "Sensitive Data Exposure", severity: "HIGH" },
    { regex: /["']mongodb(?:\+srv)?:\/\/[^"']+["']/gi, id: "hardcoded-mongodb", category: "Sensitive Data Exposure", severity: "HIGH" },
    { regex: /["'](?:Host|Server)\s*=\s*[^"']+;.*(?:Password|Pwd)\s*=[^"']+["']/gi, id: "hardcoded-pg-connstr", category: "Sensitive Data Exposure", severity: "HIGH" },
];

const SECRET_PATTERNS = [
    { regex: /["']sk_(?:live|test)_[A-Za-z0-9]{20,}["']/g, id: "stripe-key", category: "Sensitive Data Exposure", severity: "CRITICAL" },
    { regex: /["'](?:Bearer\s+)[A-Za-z0-9\-._~+/]+=*["']/g, id: "bearer-token", category: "Sensitive Data Exposure", severity: "HIGH" },
    { regex: /(?:api[_-]?key|apikey|secret[_-]?key|client[_-]?secret)\s*[:=]\s*["'][A-Za-z0-9\-._]{16,}["']/gi, id: "api-key-assignment", category: "Sensitive Data Exposure", severity: "HIGH" },
    { regex: /["'](?:ghp|gho|ghu|ghs|ghr)_[A-Za-z0-9]{30,}["']/g, id: "github-token", category: "Sensitive Data Exposure", severity: "CRITICAL" },
    { regex: /["']AKIA[A-Z0-9]{16}["']/g, id: "aws-access-key", category: "Sensitive Data Exposure", severity: "CRITICAL" },
];

const SQL_INJECTION_PATTERNS = [
    { regex: /string\.Format\s*\(\s*["'].*(?:SELECT|INSERT|UPDATE|DELETE|DROP|ALTER)\b/gi, id: "sql-string-format", category: "Injection", severity: "HIGH" },
    { regex: /\$"[^"]*(?:SELECT|INSERT|UPDATE|DELETE|DROP|ALTER)\b[^"]*\{/gi, id: "sql-interpolation", category: "Injection", severity: "HIGH" },
    { regex: /(?:["'].*(?:SELECT|INSERT|UPDATE|DELETE)\b.*["'])\s*\+\s*(?:\w+)/gi, id: "sql-concat", category: "Injection", severity: "MEDIUM" },
    { regex: /ExecuteSqlRaw\s*\(\s*\$"/gi, id: "ef-raw-sql-interpolated", category: "Injection", severity: "HIGH" },
    { regex: /FromSqlRaw\s*\(\s*\$"/gi, id: "ef-fromsql-interpolated", category: "Injection", severity: "HIGH" },
];

const XSS_PATTERNS = [
    { regex: /MarkupString\s*\(\s*(?!\s*["']<)/g, id: "markup-string-dynamic", category: "XSS", severity: "MEDIUM" },
    { regex: /\bHtml\.Raw\s*\(/g, id: "html-raw", category: "XSS", severity: "MEDIUM" },
];

const AUTH_PATTERNS = [
    { regex: /\[AllowAnonymous\]/g, id: "allow-anonymous", category: "Broken Access Control", severity: "INFO" },
    { regex: /(?:password|pwd)\s*[:=]\s*["'][^"']+["']/gi, id: "hardcoded-password", category: "Broken Authentication", severity: "HIGH" },
];

const MASS_ASSIGNMENT_PATTERNS = [
    { regex: /\[Bind\s*\(\s*\)\s*\]/g, id: "empty-bind", category: "Mass Assignment", severity: "MEDIUM" },
    { regex: /TryUpdateModelAsync\s*<\s*\w+\s*>\s*\([^)]*\)/g, id: "tryupdatemodel", category: "Mass Assignment", severity: "INFO" },
];

const ALL_PATTERNS = [
    ...CONNECTION_STRING_PATTERNS,
    ...SECRET_PATTERNS,
    ...SQL_INJECTION_PATTERNS,
    ...XSS_PATTERNS,
    ...AUTH_PATTERNS,
    ...MASS_ASSIGNMENT_PATTERNS,
];

const SCAN_EXTENSIONS = new Set([".cs", ".razor", ".json", ".csproj", ".config"]);
const SKIP_DIRS = new Set([".git", "bin", "obj", "node_modules", ".vs", "wwwroot"]);

function scanContent(content, source) {
    const findings = [];
    for (const pattern of ALL_PATTERNS) {
        const regex = new RegExp(pattern.regex.source, pattern.regex.flags);
        let match;
        while ((match = regex.exec(content)) !== null) {
            const lineNum = content.substring(0, match.index).split("\n").length;
            findings.push({
                id: pattern.id,
                category: pattern.category,
                severity: pattern.severity,
                line: lineNum,
                match: match[0].substring(0, 80),
                source,
            });
        }
    }
    return findings;
}

function formatFindings(findings) {
    if (findings.length === 0) return "✅ No security issues found.";
    const grouped = {};
    for (const f of findings) {
        if (!grouped[f.category]) grouped[f.category] = [];
        grouped[f.category].push(f);
    }
    let out = `⚠️ Found ${findings.length} potential security issue(s):\n`;
    for (const [cat, items] of Object.entries(grouped)) {
        out += `\n### ${cat}\n`;
        for (const item of items) {
            out += `- [${item.severity}] ${item.id} at ${item.source}:${item.line} — \`${item.match}\`\n`;
        }
    }
    return out;
}

function isTargetFile(filePath) {
    if (!filePath || typeof filePath !== "string") return false;
    const ext = extname(filePath).toLowerCase();
    return ext === ".cs" || ext === ".razor";
}

function isTargetFileExtended(filePath) {
    if (!filePath || typeof filePath !== "string") return false;
    const ext = extname(filePath).toLowerCase();
    return SCAN_EXTENSIONS.has(ext);
}

async function walkDirectory(dir, rootDir) {
    const files = [];
    try {
        const entries = await readdir(dir, { withFileTypes: true });
        for (const entry of entries) {
            if (SKIP_DIRS.has(entry.name)) continue;
            const fullPath = join(dir, entry.name);
            if (entry.isDirectory()) {
                files.push(...await walkDirectory(fullPath, rootDir));
            } else if (entry.isFile()) {
                const ext = extname(entry.name).toLowerCase();
                if (ext === ".cs" || ext === ".json") {
                    files.push(fullPath);
                }
            }
        }
    } catch {
        // Skip inaccessible directories
    }
    return files;
}

const session = await joinSession({
    hooks: {
        onPreToolUse: async (input) => {
            const toolName = input.toolName;
            if (toolName !== "create" && toolName !== "edit") return;

            const args = input.toolArgs;
            if (!args || typeof args !== "object") return;

            const filePath = args.path;
            if (!isTargetFileExtended(filePath)) return;

            // Scan the content being written
            const content = toolName === "create" ? args.file_text : args.new_str;
            if (!content || typeof content !== "string") return;

            const findings = scanContent(content, `${toolName}:${filePath}`);
            if (findings.length === 0) return;

            const highSeverity = findings.filter(f => f.severity === "CRITICAL" || f.severity === "HIGH");
            if (highSeverity.length > 0) {
                await session.log(`🔒 Security scan found ${highSeverity.length} HIGH/CRITICAL issue(s) in pending ${toolName}`, { level: "warning" });
            }

            return {
                additionalContext: `🔒 SECURITY SCANNER WARNING — The ${toolName} operation on "${filePath}" contains potential security issues:\n${formatFindings(findings)}\nPlease address these before proceeding. Use parameterized queries, IOptions<T> for config, and Azure Key Vault / user-secrets for sensitive values.`,
            };
        },

        onPostToolUse: async (input) => {
            const toolName = input.toolName;
            if (toolName !== "create" && toolName !== "edit") return;

            const args = input.toolArgs;
            if (!args || typeof args !== "object") return;

            const filePath = args.path;
            if (!filePath || typeof filePath !== "string") return;
            if (!isTargetFile(filePath)) return;

            const lower = filePath.toLowerCase();
            const isSensitive = lower.includes("auth") || lower.includes("payment") || lower.includes("program.cs")
                || lower.includes("startup") || lower.includes("security") || lower.includes("credential")
                || lower.includes("stripe") || lower.includes("escrow") || lower.includes("fund");

            if (!isSensitive) return;

            return {
                additionalContext: "🔒 OWASP Compliance Reminder: This file is in a security-sensitive area. Verify: (1) No hardcoded secrets — use IOptions<T> + Azure Key Vault, (2) Input validation with FluentValidation, (3) Parameterized queries only, (4) [Authorize] on all endpoints, (5) DTOs for mass-assignment protection, (6) CancellationToken propagation.",
            };
        },

        onUserPromptSubmitted: async (input) => {
            const prompt = input.prompt;
            if (!prompt || typeof prompt !== "string") return;

            if (/\b(?:payment|stripe|pay(?:out)?|escrow|fund|refund)\b/i.test(prompt)) {
                return {
                    additionalContext: "🔒 FINTECH SECURITY CONTEXT: This involves payment/financial operations. Requirements: (1) Idempotency keys on all payment mutations, (2) PCI-DSS: never log or store raw card numbers, (3) Use Stripe SDK — never call API directly with raw HTTP, (4) Audit trail for all financial state transitions, (5) Use decimal (not float/double) for monetary amounts, (6) Implement retry with Polly + circuit breaker for payment gateway calls.",
                };
            }

            if (/\b(?:auth|login|credential|token|session|identity|password|jwt|oauth|oidc)\b/i.test(prompt)) {
                return {
                    additionalContext: "🔒 AUTH SECURITY CONTEXT: (1) Use Microsoft.Identity.Web or Duende IdentityServer — never roll custom auth, (2) Policy-based authorization with [Authorize(Policy = \"...\")], (3) Never store tokens in localStorage, (4) Implement token refresh, (5) Hash passwords with bcrypt/scrypt via ASP.NET Identity, (6) Enforce MFA for admin operations, (7) Log auth failures with correlation IDs.",
                };
            }
        },
    },

    tools: [
        {
            name: "owasp_security_scan",
            description: "Scans a file for OWASP Top 10 security issues including injection, broken auth, sensitive data exposure, XSS, security misconfiguration, and mass assignment. Returns structured findings with severity levels.",
            parameters: {
                type: "object",
                properties: {
                    filePath: { type: "string", description: "Absolute path to the file to scan" },
                },
                required: ["filePath"],
                additionalProperties: false,
            },
            handler: async (args) => {
                const filePath = args.filePath;
                if (!filePath) return "Error: filePath is required.";

                const resolved = resolve(filePath);
                try {
                    const content = readFileSync(resolved, "utf-8");
                    const findings = scanContent(content, relative(process.cwd(), resolved));

                    let result = `## OWASP Security Scan: ${relative(process.cwd(), resolved)}\n`;
                    result += `Scanned ${content.split("\n").length} lines against ${ALL_PATTERNS.length} patterns.\n\n`;
                    result += formatFindings(findings);
                    return result;
                } catch (err) {
                    return `Error reading file: ${err.message}`;
                }
            },
        },
        {
            name: "check_secrets",
            description: "Recursively scans .cs and .json files in a directory for hardcoded secrets, API keys, connection strings, and credentials. Reports findings with file path and line number.",
            parameters: {
                type: "object",
                properties: {
                    directory: { type: "string", description: "Directory to scan (defaults to current working directory)" },
                },
                additionalProperties: false,
            },
            handler: async (args) => {
                const rootDir = resolve(process.cwd());
                const targetDir = args.directory ? resolve(args.directory) : rootDir;

                // Scope check: must be within the repo root
                if (!targetDir.startsWith(rootDir)) {
                    return "Error: Directory must be within the project root.";
                }

                await session.log("🔍 Scanning for secrets...", { ephemeral: true });

                try {
                    const files = await walkDirectory(targetDir, rootDir);
                    const allFindings = [];

                    for (const filePath of files) {
                        try {
                            const content = await readFile(filePath, "utf-8");
                            const relPath = relative(rootDir, filePath);
                            const findings = scanContent(content, relPath);
                            // Only report secret-related findings
                            const secretFindings = findings.filter(f =>
                                f.category === "Sensitive Data Exposure" || f.category === "Broken Authentication"
                            );
                            allFindings.push(...secretFindings);
                        } catch {
                            // Skip unreadable files
                        }
                    }

                    let result = `## Secret Scan Results\n`;
                    result += `Scanned ${files.length} files in ${relative(rootDir, targetDir) || "."}\n\n`;

                    if (allFindings.length === 0) {
                        result += "✅ No hardcoded secrets detected.";
                    } else {
                        result += `⚠️ Found ${allFindings.length} potential secret(s):\n\n`;
                        for (const f of allFindings) {
                            result += `- [${f.severity}] **${f.source}:${f.line}** — ${f.id}: \`${f.match}\`\n`;
                        }
                        result += "\n**Recommendation:** Move secrets to Azure Key Vault, `dotnet user-secrets`, or environment variables. Use `IOptions<T>` pattern for configuration.";
                    }

                    await session.log(`Secret scan complete: ${allFindings.length} finding(s) in ${files.length} files`, { ephemeral: true });
                    return result;
                } catch (err) {
                    return `Error scanning directory: ${err.message}`;
                }
            },
        },
    ],
});

await session.log("🔒 OWASP Security Scanner loaded");
