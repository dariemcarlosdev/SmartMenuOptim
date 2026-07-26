import { joinSession } from "@github/copilot-sdk/extension";
import { readFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

// Resolve paths relative to this extension, not cwd
const __dirname = dirname(fileURLToPath(import.meta.url));
const SKILLS_DIR = join(__dirname, "skills");

// Manifest: single source of truth for all skills
const MANIFEST = {
    brainstorming: {
        title: "Brainstorming Ideas Into Designs",
        file: "brainstorming.md",
        description: "Socratic design refinement — explore intent, propose approaches, get approval before code",
        related: ["writing-plans"],
        recommended_next: "writing-plans",
    },
    "writing-plans": {
        title: "Writing Implementation Plans",
        file: "writing-plans.md",
        description: "Break specs into bite-sized TDD tasks with exact file paths, code, and verification steps",
        related: ["brainstorming", "executing-plans", "subagent-driven-development"],
        recommended_next: "executing-plans",
    },
    "executing-plans": {
        title: "Executing Plans",
        file: "executing-plans.md",
        description: "Load plan, review critically, execute tasks sequentially with verification",
        related: ["writing-plans", "subagent-driven-development", "verification-before-completion"],
        recommended_next: "verification-before-completion",
    },
    tdd: {
        title: "Test-Driven Development",
        file: "test-driven-development.md",
        description: "RED-GREEN-REFACTOR — write failing test, minimal code to pass, then clean up",
        related: ["systematic-debugging", "verification-before-completion"],
        recommended_next: null,
    },
    "systematic-debugging": {
        title: "Systematic Debugging",
        file: "systematic-debugging.md",
        description: "4-phase root cause analysis — investigate before fixing, never guess",
        related: ["tdd", "verification-before-completion"],
        recommended_next: "verification-before-completion",
    },
    "subagent-driven-development": {
        title: "Subagent-Driven Development",
        file: "subagent-driven-development.md",
        description: "Dispatch fresh agent per task with two-stage review (spec + quality)",
        related: ["writing-plans", "executing-plans", "requesting-code-review"],
        recommended_next: "requesting-code-review",
    },
    "verification-before-completion": {
        title: "Verification Before Completion",
        file: "verification-before-completion.md",
        description: "Evidence before claims — run verification, read output, THEN report status",
        related: ["tdd", "systematic-debugging"],
        recommended_next: null,
    },
    "requesting-code-review": {
        title: "Requesting Code Review",
        file: "requesting-code-review.md",
        description: "Dispatch critic agent to review changes against spec and quality standards",
        related: ["subagent-driven-development", "verification-before-completion"],
        recommended_next: null,
    },
};

const ALLOWED_SKILLS = new Set(Object.keys(MANIFEST));

function loadSkill(skillId) {
    if (!ALLOWED_SKILLS.has(skillId)) {
        return `Unknown skill: "${skillId}". Use superpowers_catalog to see available skills.`;
    }
    const entry = MANIFEST[skillId];
    const filePath = join(SKILLS_DIR, entry.file);
    try {
        const content = readFileSync(filePath, "utf-8");
        let result = content;
        if (entry.related.length > 0) {
            result += `\n\n---\n**Related skills:** ${entry.related.join(", ")}`;
        }
        if (entry.recommended_next) {
            result += `\n**Recommended next:** superpowers_skill(skill: "${entry.recommended_next}")`;
        }
        return result;
    } catch {
        return `Error: Could not load skill file "${entry.file}". Ensure the extension is properly installed.`;
    }
}

function buildCatalog() {
    const lines = [
        "# Superpowers Skills Catalog",
        "",
        "On-demand workflow skills ported from obra/superpowers (MIT). Call `superpowers_skill` with a skill ID to load.",
        "",
        "| Skill ID | Title | Description |",
        "|----------|-------|-------------|",
    ];
    for (const [id, entry] of Object.entries(MANIFEST)) {
        lines.push(`| \`${id}\` | ${entry.title} | ${entry.description} |`);
    }
    lines.push("");
    lines.push("## Typical Flow");
    lines.push("```");
    lines.push("brainstorming → writing-plans → executing-plans / subagent-driven-development");
    lines.push("                                    ↕                       ↕");
    lines.push("                               tdd + systematic-debugging + verification-before-completion");
    lines.push("                                                            ↕");
    lines.push("                                                   requesting-code-review");
    lines.push("```");
    lines.push("");
    lines.push("*Attribution: Based on obra/superpowers (MIT License) — adapted for Copilot CLI*");
    return lines.join("\n");
}

const session = await joinSession({
    tools: [
        {
            name: "superpowers_catalog",
            description:
                "List all available Superpowers workflow skills with descriptions and recommended flow. Zero-cost overview — no skill content loaded.",
            parameters: {
                type: "object",
                properties: {},
                additionalProperties: false,
            },
            handler: async () => buildCatalog(),
        },
        {
            name: "superpowers_skill",
            description:
                "Load a specific Superpowers workflow skill on-demand. Returns the full skill methodology for the agent to follow. Use superpowers_catalog first to see available skills.",
            parameters: {
                type: "object",
                properties: {
                    skill: {
                        type: "string",
                        description: "The skill ID to load",
                        enum: Object.keys(MANIFEST),
                    },
                },
                required: ["skill"],
                additionalProperties: false,
            },
            handler: async (params) => loadSkill(params.skill),
        },
    ],
});
