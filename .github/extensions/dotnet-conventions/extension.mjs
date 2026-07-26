import { joinSession } from "@github/copilot-sdk/extension";
import { readFileSync, readdirSync, existsSync, statSync } from "node:fs";
import { join, basename, extname, resolve } from "node:path";

const EXCLUDED_DIRS = new Set(["bin", "obj", "node_modules", ".git", "Migrations"]);
const EXCLUDED_FILES = /\.(g|Designer|AssemblyInfo)\.cs$/i;
const TOPLEVEL_EXCEPTIONS = new Set(["Program.cs"]);

function checkCsConventions(filePath, content) {
  const findings = [];
  const fileName = basename(filePath);

  // Skip known exceptions
  if (TOPLEVEL_EXCEPTIONS.has(fileName)) return findings;

  // 1. File-scoped namespace check: detect block-scoped `namespace X\n{` or `namespace X {`
  const blockNamespace = /^namespace\s+[\w.]+\s*\r?\n?\s*\{/m;
  if (blockNamespace.test(content)) {
    findings.push("Uses block-scoped namespace. Convert to file-scoped namespace (namespace X;).");
  }

  // 2. .razor.cs must declare partial class
  if (filePath.endsWith(".razor.cs")) {
    const hasPartial = /\bpartial\s+class\b/i.test(content);
    if (!hasPartial) {
      findings.push("Code-behind file (.razor.cs) must declare a partial class.");
    }
  }

  // 3. Nullable reference types (check for #nullable enable or nullable annotation)
  // Only flag if there's a namespace (real source file, not top-level Program.cs)
  if (/\bnamespace\b/.test(content) && !/#nullable\s+enable/.test(content)) {
    // Not necessarily a violation if enabled in .csproj, note as advisory
    findings.push("Advisory: No #nullable enable directive found. Ensure <Nullable>enable</Nullable> is set in .csproj.");
  }

  // 4. Class name should match file name (for non-razor.cs files)
  if (!filePath.endsWith(".razor.cs")) {
    const expectedName = fileName.replace(/\.cs$/, "");
    const classDecl = /\bclass\s+(\w+)/.exec(content);
    if (classDecl && classDecl[1] !== expectedName) {
      findings.push(`Class name "${classDecl[1]}" does not match file name "${fileName}".`);
    }
  }

  return findings;
}

function checkRazorConventions(filePath, content) {
  const findings = [];

  // 1. @code blocks — should use code-behind pattern
  if (/@code\s*\{/i.test(content)) {
    findings.push("Contains @code block. Use code-behind pattern (.razor + .razor.cs) instead.");
  }

  // 2. Inline styles
  if (/\bstyle\s*=\s*"/i.test(content)) {
    findings.push("Contains inline style attribute. Use scoped CSS (.razor.css) instead.");
  }

  return findings;
}

function checkFile(filePath) {
  try {
    const content = readFileSync(filePath, "utf-8");
    const ext = extname(filePath).toLowerCase();

    if (ext === ".cs") {
      return checkCsConventions(filePath, content);
    }
    if (ext === ".razor") {
      return checkRazorConventions(filePath, content);
    }
  } catch {
    return [`Could not read file: ${filePath}`];
  }
  return [];
}

function walkDirectory(dirPath, results) {
  if (!existsSync(dirPath)) return;

  try {
    const entries = readdirSync(dirPath, { withFileTypes: true });
    for (const entry of entries) {
      if (EXCLUDED_DIRS.has(entry.name)) continue;

      const fullPath = join(dirPath, entry.name);
      if (entry.isDirectory()) {
        walkDirectory(fullPath, results);
      } else if (entry.isFile()) {
        if (EXCLUDED_FILES.test(entry.name)) continue;
        const ext = extname(entry.name).toLowerCase();
        if (ext === ".cs" || ext === ".razor") {
          const findings = checkFile(fullPath);
          if (findings.length > 0) {
            results.push({ file: fullPath, findings });
          }
        }
      }
    }
  } catch {
    // Skip inaccessible directories
  }
}

const session = await joinSession({
  hooks: {
    onSessionStart: async () => {
      await session.log("DotNet Conventions extension loaded");
    },

    onPostToolUse: async (input) => {
      if (input.toolName !== "edit" && input.toolName !== "create") {
        return undefined;
      }

      const filePath = typeof input.toolArgs?.path === "string"
        ? input.toolArgs.path
        : undefined;
      if (!filePath) return undefined;

      const ext = extname(filePath).toLowerCase();
      if (ext !== ".cs" && ext !== ".razor") return undefined;

      // Skip excluded files
      const fileName = basename(filePath);
      if (TOPLEVEL_EXCEPTIONS.has(fileName)) return undefined;
      if (EXCLUDED_FILES.test(fileName)) return undefined;

      try {
        const findings = checkFile(filePath);
        if (findings.length === 0) return undefined;

        return {
          additionalContext: [
            `CONVENTION VIOLATIONS in ${fileName}:`,
            ...findings.map((f, i) => `  ${i + 1}. ${f}`),
            "",
            "Please fix these violations to comply with project .NET conventions.",
          ].join("\n"),
        };
      } catch {
        // If file can't be read, skip silently
        return undefined;
      }
    },
  },

  tools: [
    {
      name: "check_conventions",
      description:
        "Checks .NET coding conventions on a file or directory. Scans .cs and .razor files for: file-scoped namespaces, code-behind pattern, partial class declarations, inline styles, and class naming.",
      parameters: {
        type: "object",
        properties: {
          path: {
            type: "string",
            description: "Absolute path to a file or directory to check.",
          },
        },
        required: ["path"],
      },
      handler: async (args) => {
        const targetPath = args.path;

        if (!existsSync(targetPath)) {
          return `Path does not exist: ${targetPath}`;
        }

        const stat = statSync(targetPath);
        const results = [];

        if (stat.isFile()) {
          const ext = extname(targetPath).toLowerCase();
          if (ext !== ".cs" && ext !== ".razor") {
            return `Not a .cs or .razor file: ${targetPath}`;
          }
          const findings = checkFile(targetPath);
          if (findings.length > 0) {
            results.push({ file: targetPath, findings });
          }
        } else if (stat.isDirectory()) {
          walkDirectory(targetPath, results);
        }

        if (results.length === 0) {
          return "✓ No convention violations found.";
        }

        const lines = [
          "# Convention Check Results",
          "",
          `Files with violations: ${results.length}`,
          "",
        ];

        for (const r of results) {
          lines.push(`## ${r.file}`);
          for (const f of r.findings) {
            lines.push(`  - ${f}`);
          }
          lines.push("");
        }

        return lines.join("\n");
      },
    },
  ],
});
