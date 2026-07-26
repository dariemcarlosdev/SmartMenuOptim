import { joinSession } from "@github/copilot-sdk/extension";
import { execFile } from "node:child_process";
import { readdirSync } from "node:fs";
import { readdir } from "node:fs/promises";
import { resolve, extname } from "node:path";

const SOLUTION_NAME = "EscrowApp.sln";
const BUILD_TIMEOUT = 120_000;
const TEST_TIMEOUT = 120_000;
const MAX_BUFFER = 1024 * 1024 * 5; // 5MB

const modifiedFiles = new Set();
let lastReminderTime = 0;
const REMINDER_COOLDOWN = 30_000; // Only remind every 30 seconds

function findSolutionRoot() {
    // Walk up from cwd to find the .sln file
    let dir = process.cwd();
    const root = resolve(dir, "/");
    while (dir !== root) {
        try {
            const files = readdirSync(dir);
            if (files.includes(SOLUTION_NAME)) return dir;
        } catch {
            // Skip
        }
        dir = resolve(dir, "..");
    }
    return process.cwd();
}

function runDotnetCommand(args, timeoutMs) {
    const solutionRoot = findSolutionRoot();
    const solutionPath = resolve(solutionRoot, SOLUTION_NAME);

    return new Promise((res) => {
        const child = execFile("dotnet", [...args, solutionPath], {
            cwd: solutionRoot,
            timeout: timeoutMs,
            maxBuffer: MAX_BUFFER,
            windowsHide: true,
        }, (err, stdout, stderr) => {
            const output = (stdout || "") + (stderr || "");
            if (err) {
                if (err.killed) {
                    res({ success: false, output: `Command timed out after ${timeoutMs / 1000}s.\n${output}` });
                } else {
                    res({ success: false, output });
                }
            } else {
                res({ success: true, output });
            }
        });
    });
}

function isWatchedFile(filePath) {
    if (!filePath || typeof filePath !== "string") return false;
    const ext = extname(filePath).toLowerCase();
    return ext === ".cs" || ext === ".csproj" || ext === ".razor";
}

async function hasTestProjects(solutionRoot) {
    try {
        const entries = await readdir(solutionRoot, { withFileTypes: true });
        for (const entry of entries) {
            if (entry.isDirectory() && entry.name.toLowerCase().includes("test")) {
                return true;
            }
        }
        // Also check if dotnet test would find anything by looking for test csproj references
        return false;
    } catch {
        return false;
    }
}

const session = await joinSession({
    hooks: {
        onPostToolUse: async (input) => {
            const toolName = input.toolName;
            if (toolName !== "create" && toolName !== "edit") return;

            const args = input.toolArgs;
            if (!args || typeof args !== "object") return;

            const filePath = args.path;
            if (!isWatchedFile(filePath)) return;

            modifiedFiles.add(filePath);

            const now = Date.now();
            if (now - lastReminderTime < REMINDER_COOLDOWN) return;
            lastReminderTime = now;

            const fileList = [...modifiedFiles].map(f => `  - ${f}`).join("\n");
            return {
                additionalContext: `🏗️ Build Guardian: ${modifiedFiles.size} file(s) modified since last build check:\n${fileList}\nRemember to verify the build compiles after these changes. Use the dotnet_build_check tool to validate.`,
            };
        },
    },

    tools: [
        {
            name: "dotnet_build_check",
            description: "Runs 'dotnet build' on the EscrowApp.sln solution and returns a structured result. Returns 'Build succeeded' on success or detailed error messages on failure.",
            parameters: {
                type: "object",
                properties: {},
                additionalProperties: false,
            },
            handler: async () => {
                await session.log("🏗️ Running dotnet build...", { ephemeral: true });

                const result = await runDotnetCommand(["build", "--no-restore", "--verbosity", "minimal"], BUILD_TIMEOUT);

                // Clear tracked files on successful build
                if (result.success) {
                    const count = modifiedFiles.size;
                    modifiedFiles.clear();
                    await session.log("✅ Build succeeded", { ephemeral: true });
                    return `Build succeeded. ${count > 0 ? `(${count} pending file change(s) verified)` : ""}`;
                }

                await session.log("❌ Build failed", { level: "warning", ephemeral: true });

                // Extract error lines for concise output
                const lines = result.output.split("\n");
                const errors = lines.filter(l => /:\s*error\s+\w+/i.test(l));
                const warnings = lines.filter(l => /:\s*warning\s+\w+/i.test(l));

                let output = "Build FAILED.\n\n";
                if (errors.length > 0) {
                    output += `### Errors (${errors.length})\n`;
                    output += errors.slice(0, 20).join("\n");
                    if (errors.length > 20) output += `\n... and ${errors.length - 20} more errors`;
                    output += "\n\n";
                }
                if (warnings.length > 0) {
                    output += `### Warnings (${warnings.length})\n`;
                    output += warnings.slice(0, 10).join("\n");
                    if (warnings.length > 10) output += `\n... and ${warnings.length - 10} more warnings`;
                }
                if (errors.length === 0 && warnings.length === 0) {
                    output += result.output.substring(0, 2000);
                }

                return { textResultForLlm: output, resultType: "failure" };
            },
        },
        {
            name: "dotnet_test_check",
            description: "Runs 'dotnet test' on the EscrowApp.sln solution. Returns test results summary on success or failing test details on failure. Reports if no test projects exist.",
            parameters: {
                type: "object",
                properties: {},
                additionalProperties: false,
            },
            handler: async () => {
                const solutionRoot = findSolutionRoot();

                // Check for test projects first
                const hasTests = await hasTestProjects(solutionRoot);
                if (!hasTests) {
                    await session.log("ℹ️ No test projects detected", { ephemeral: true });
                }

                await session.log("🧪 Running dotnet test...", { ephemeral: true });

                const result = await runDotnetCommand(["test", "--no-build", "--verbosity", "minimal"], TEST_TIMEOUT);

                if (result.success) {
                    // Extract test count from output
                    const totalMatch = result.output.match(/Passed!\s*-\s*Failed:\s*(\d+),\s*Passed:\s*(\d+)/i)
                        || result.output.match(/Total tests:\s*(\d+)/i);

                    let summary = "All tests passed";
                    if (totalMatch) {
                        summary += ` (${totalMatch[0].trim()})`;
                    }

                    // Check for "no test" scenarios
                    if (/No test is available/i.test(result.output) || /No test matches/i.test(result.output)) {
                        await session.log("ℹ️ No tests found in solution", { ephemeral: true });
                        return "No test projects or test methods found in the solution. Consider adding a test project (e.g., EscrowApp.Tests) with xUnit or NUnit.";
                    }

                    await session.log("✅ Tests passed", { ephemeral: true });
                    return summary;
                }

                await session.log("❌ Tests failed", { level: "warning", ephemeral: true });

                const lines = result.output.split("\n");
                const failedTests = lines.filter(l => /Failed\s+\w+/i.test(l) || /✗|×/.test(l));
                const errorLines = lines.filter(l => /:\s*error\s+/i.test(l));

                let output = "Tests FAILED.\n\n";
                if (failedTests.length > 0) {
                    output += `### Failed Tests (${failedTests.length})\n`;
                    output += failedTests.slice(0, 20).join("\n");
                    output += "\n\n";
                }
                if (errorLines.length > 0) {
                    output += `### Errors\n`;
                    output += errorLines.slice(0, 10).join("\n");
                }
                if (failedTests.length === 0 && errorLines.length === 0) {
                    output += result.output.substring(0, 2000);
                }

                return { textResultForLlm: output, resultType: "failure" };
            },
        },
    ],
});

await session.log("🏗️ Build Guardian loaded");
