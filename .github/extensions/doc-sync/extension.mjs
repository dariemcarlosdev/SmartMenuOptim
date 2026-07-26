import { joinSession } from "@github/copilot-sdk/extension";
import { existsSync, statSync, readdirSync } from "node:fs";
import { join, resolve, relative } from "node:path";

// Maps source path segments to their corresponding docs/ folder
const FEATURE_MAP = [
  { pattern: /Features[/\\]Escrow[/\\]HoldFunds/i, doc: "01-Escrow-Hold-Funds" },
  { pattern: /Features[/\\]Escrow[/\\]CreateAndHoldFunds/i, doc: "01-Escrow-Hold-Funds" },
  { pattern: /Features[/\\]Escrow[/\\]ReleaseFunds/i, doc: "02-Escrow-Release-Funds" },
  { pattern: /Features[/\\]Escrow[/\\]DisputeFunds/i, doc: "03-Escrow-Dispute-Funds" },
  { pattern: /Services[/\\]Strategies/i, doc: "04-Payment-Strategies" },
  { pattern: /Services[/\\]/i, doc: "04-Payment-Strategies" },
  { pattern: /Infrastructure[/\\]Auth/i, doc: "05-Hybrid-Identity" },
  { pattern: /Events[/\\]/i, doc: "06-Event-Bus" },
  { pattern: /Resources[/\\]/i, doc: "07-Localization" },
  { pattern: /Components[/\\]Pages[/\\]/i, doc: "08-Landing-Page-UI" },
  { pattern: /Features[/\\]Escrow[/\\]Api/i, doc: "09-API-Integration" },
  { pattern: /Features[/\\]Escrow[/\\]GetTransaction/i, doc: "09-API-Integration" },
  { pattern: /Features[/\\]Escrow[/\\]ListTransactions/i, doc: "09-API-Integration" },
  { pattern: /Infrastructure[/\\]Middleware/i, doc: "09-API-Integration" },
];

const WATCHED_DIRS =
  /[/\\](Features|Services|Models|Events|Components|Infrastructure|Resources)[/\\]/i;

// Deduplication: track last reminder time per doc target
const lastReminder = new Map();
const REMINDER_COOLDOWN_MS = 60_000;

function findAppRoot(cwd) {
  const candidates = [
    join(cwd, "EscrowApp"),
    cwd,
  ];
  for (const candidate of candidates) {
    if (existsSync(join(candidate, "docs")) && existsSync(join(candidate, "EscrowApp.csproj"))) {
      return candidate;
    }
  }
  // Fallback: check if docs/ exists at cwd/EscrowApp
  if (existsSync(join(cwd, "EscrowApp", "docs"))) {
    return join(cwd, "EscrowApp");
  }
  return undefined;
}

function mapFileToDoc(filePath) {
  const normalized = filePath.replace(/\\/g, "/");
  for (const entry of FEATURE_MAP) {
    if (entry.pattern.test(normalized)) {
      return entry.doc;
    }
  }
  return "00-Architecture-Overview";
}

function getLatestMtime(dirPath) {
  let latest = 0;
  if (!existsSync(dirPath)) return latest;

  try {
    const entries = readdirSync(dirPath, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = join(dirPath, entry.name);
      try {
        if (entry.isDirectory()) {
          const childMtime = getLatestMtime(fullPath);
          if (childMtime > latest) latest = childMtime;
        } else if (entry.isFile()) {
          const mtime = statSync(fullPath).mtimeMs;
          if (mtime > latest) latest = mtime;
        }
      } catch {
        // Skip inaccessible entries
      }
    }
  } catch {
    // Skip inaccessible directories
  }
  return latest;
}

// Source directories and their doc mappings for the docs_status tool
const STATUS_MAP = [
  { label: "Escrow Hold Funds", srcDir: "Features/Escrow/HoldFunds", doc: "01-Escrow-Hold-Funds" },
  { label: "Escrow Release Funds", srcDir: "Features/Escrow/ReleaseFunds", doc: "02-Escrow-Release-Funds" },
  { label: "Escrow Dispute Funds", srcDir: "Features/Escrow/DisputeFunds", doc: "03-Escrow-Dispute-Funds" },
  { label: "Payment Strategies", srcDir: "Services/Strategies", doc: "04-Payment-Strategies" },
  { label: "Hybrid Identity", srcDir: "Infrastructure/Auth", doc: "05-Hybrid-Identity" },
  { label: "Event Bus", srcDir: "Events", doc: "06-Event-Bus" },
  { label: "Localization", srcDir: "Resources", doc: "07-Localization" },
  { label: "Landing Page UI", srcDir: "Components/Pages", doc: "08-Landing-Page-UI" },
  { label: "API Integration", srcDir: "Features/Escrow/Api", doc: "09-API-Integration" },
];

function formatTimestamp(ms) {
  if (ms === 0) return "N/A";
  return new Date(ms).toISOString().replace("T", " ").substring(0, 19);
}

const session = await joinSession({
  hooks: {
    onSessionStart: async () => {
      await session.log("Doc-Sync extension loaded");
    },

    onPostToolUse: async (input) => {
      if (input.toolName !== "edit" && input.toolName !== "create") {
        return undefined;
      }

      const filePath = typeof input.toolArgs?.path === "string"
        ? input.toolArgs.path
        : undefined;
      if (!filePath) return undefined;

      // Only watch relevant directories
      if (!WATCHED_DIRS.test(filePath)) return undefined;

      const docFolder = mapFileToDoc(filePath);

      // Deduplicate reminders
      const now = Date.now();
      const lastTime = lastReminder.get(docFolder) || 0;
      if (now - lastTime < REMINDER_COOLDOWN_MS) return undefined;
      lastReminder.set(docFolder, now);

      return {
        additionalContext: [
          `DOCS SYNC REQUIRED: You modified code related to "${docFolder}".`,
          `Per project rules, the corresponding docs/${docFolder}/README.md must be updated to reflect these changes.`,
          "Check if documentation needs updating before moving on.",
        ].join(" "),
      };
    },
  },

  tools: [
    {
      name: "docs_status",
      description:
        "Compares last-modified timestamps of source code directories vs their corresponding docs/ README.md files. Reports which docs are potentially stale.",
      parameters: {
        type: "object",
        properties: {},
      },
      handler: async () => {
        const cwd = process.cwd();
        const appRoot = findAppRoot(cwd);

        if (!appRoot) {
          return "Could not locate EscrowApp directory. Searched from: " + cwd;
        }

        const docsRoot = join(appRoot, "docs");
        const lines = [
          "# Documentation Freshness Report",
          "",
          `App root: ${appRoot}`,
          "",
          "Feature Area           | Source Last Modified    | Docs Last Modified     | Status",
          "-----------------------|------------------------|------------------------|------------------",
        ];

        for (const entry of STATUS_MAP) {
          const srcPath = join(appRoot, ...entry.srcDir.split("/"));
          const docReadme = join(docsRoot, entry.doc, "README.md");

          const srcMtime = getLatestMtime(srcPath);

          let docMtime = 0;
          try {
            if (existsSync(docReadme)) {
              docMtime = statSync(docReadme).mtimeMs;
            }
          } catch {
            // Not accessible
          }

          let status;
          if (srcMtime === 0) {
            status = "no source";
          } else if (docMtime === 0) {
            status = "MISSING DOCS";
          } else if (srcMtime > docMtime) {
            status = "potentially-stale";
          } else {
            status = "up-to-date";
          }

          const label = entry.label.padEnd(23);
          const srcTs = formatTimestamp(srcMtime).padEnd(24);
          const docTs = formatTimestamp(docMtime).padEnd(24);
          lines.push(`${label}| ${srcTs}| ${docTs}| ${status}`);
        }

        return lines.join("\n");
      },
    },
  ],
});
