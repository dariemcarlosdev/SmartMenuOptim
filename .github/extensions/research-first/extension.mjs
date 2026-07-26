import { joinSession } from "@github/copilot-sdk/extension";
import { readdirSync, readFileSync, existsSync, statSync } from "node:fs";
import { join, resolve } from "node:path";

const IMPLEMENTATION_KEYWORDS =
  /\b(create|implement|add|build|write|refactor|change|modify|update)\b/i;

const RESEARCH_KEYWORDS =
  /\b(explore|search|find|understand|analyze|review|read|explain)\b|what is|how does/i;

function findDocsRoot(cwd) {
  const candidates = [
    join(cwd, "EscrowApp", "docs"),
    join(cwd, "docs"),
  ];
  for (const candidate of candidates) {
    if (existsSync(candidate) && statSync(candidate).isDirectory()) {
      return candidate;
    }
  }
  return undefined;
}

function listDocFolders(docsRoot) {
  const entries = readdirSync(docsRoot, { withFileTypes: true });
  return entries
    .filter((e) => e.isDirectory() && /^\d{2}-/.test(e.name))
    .map((e) => {
      const readmePath = join(docsRoot, e.name, "README.md");
      const hasReadme = existsSync(readmePath);
      return { folder: e.name, readmePath, hasReadme };
    })
    .sort((a, b) => a.folder.localeCompare(b.folder));
}

function searchDocs(docsRoot, term) {
  const folders = listDocFolders(docsRoot);
  const matches = [];
  const lowerTerm = term.toLowerCase();

  for (const entry of folders) {
    if (!entry.hasReadme) continue;
    try {
      const content = readFileSync(entry.readmePath, "utf-8");
      const lines = content.split("\n");
      for (let i = 0; i < lines.length; i++) {
        if (lines[i].toLowerCase().includes(lowerTerm)) {
          matches.push({
            doc: entry.folder,
            line: i + 1,
            text: lines[i].trim().substring(0, 200),
          });
        }
      }
    } catch {
      // Skip unreadable files
    }
  }
  return matches;
}

const session = await joinSession({
  hooks: {
    onSessionStart: async () => {
      await session.log("Research-First extension loaded");
    },

    onUserPromptSubmitted: async (input) => {
      if (!input.prompt) return undefined;

      // Skip injection if prompt already contains research intent
      if (RESEARCH_KEYWORDS.test(input.prompt)) return undefined;

      // Inject only when implementation intent is detected
      if (IMPLEMENTATION_KEYWORDS.test(input.prompt)) {
        return {
          additionalContext: [
            "RESEARCH-FIRST PRINCIPLE: Before making changes, explore the existing codebase to understand current patterns.",
            "Check docs/ for feature documentation (use the check_docs tool if needed).",
            "Understand the layer this change belongs to (Domain/Application/Infrastructure/Presentation).",
            "Verify existing tests and patterns before creating new code.",
          ].join(" "),
        };
      }

      return undefined;
    },
  },

  tools: [
    {
      name: "check_docs",
      description:
        "Lists available feature documentation in EscrowApp/docs/ and optionally searches README.md files for a term.",
      parameters: {
        type: "object",
        properties: {
          search_term: {
            type: "string",
            description:
              "Optional keyword to search for within README.md files.",
          },
        },
      },
      handler: async (args, invocation) => {
        const cwd = process.cwd();
        const docsRoot = findDocsRoot(cwd);

        if (!docsRoot) {
          return "Could not locate EscrowApp/docs/ directory. Searched from: " + cwd;
        }

        const folders = listDocFolders(docsRoot);
        const lines = ["# Available Feature Documentation", ""];
        lines.push(`Location: ${docsRoot}`, "");

        for (const entry of folders) {
          const status = entry.hasReadme ? "✓ README.md" : "✗ no README.md";
          lines.push(`  ${entry.folder}  [${status}]`);
        }

        if (args.search_term) {
          const matches = searchDocs(docsRoot, args.search_term);
          lines.push("", `# Search results for "${args.search_term}"`, "");

          if (matches.length === 0) {
            lines.push("  No matches found.");
          } else {
            for (const m of matches) {
              lines.push(`  [${m.doc}] line ${m.line}: ${m.text}`);
            }
          }
        }

        return lines.join("\n");
      },
    },
  ],
});
