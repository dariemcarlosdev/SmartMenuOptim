import { joinSession } from "@github/copilot-sdk/extension";

const PROJECT_SUMMARY = `## NexTruzt.io EscrowApp — Project Summary

**Stack:** .NET 10 · Blazor Server · EF Core · PostgreSQL · MediatR · FluentValidation

### Architecture (Clean Architecture + CQRS)
\`\`\`
┌─────────────────────────────────────────────────┐
│  Components/  (Blazor UI — code-behind pattern) │
│    Pages/, Layout/, Shared/                      │
├─────────────────────────────────────────────────┤
│  Features/  (Application — CQRS handlers)       │
│    Commands/, Queries/, Validators/              │
├─────────────────────────────────────────────────┤
│  Models/ + Events/  (Domain layer)              │
│    Entities, Value Objects, Domain Events        │
├─────────────────────────────────────────────────┤
│  Data/  (Infrastructure — EF Core + PostgreSQL) │
│    DbContext, Repositories, Migrations/          │
├─────────────────────────────────────────────────┤
│  Services/ + Infrastructure/                     │
│    External integrations, Payment gateways       │
└─────────────────────────────────────────────────┘
\`\`\`

### Key Patterns
- **Payment Strategy:** IFundHoldable / IFundReleasable / IFundCancellable interfaces
- **CQRS:** MediatR command/query separation
- **Code-behind:** All Blazor components use .razor + .razor.cs (never inline @code)
- **Scoped CSS:** Every component has .razor.css
- **Validation:** FluentValidation on all commands
- **Resilience:** Polly retry + circuit breaker on external calls
- **Security:** OWASP-first, idempotency keys on payments, [Authorize] everywhere

### Key Files
- \`EscrowApp.sln\` — Solution root
- \`EscrowApp/Program.cs\` — App bootstrap + DI
- \`EscrowApp/Data/\` — EF Core DbContext + repositories
- \`EscrowApp/Models/\` — Domain entities + value objects
- \`EscrowApp/Features/\` — CQRS handlers (commands + queries)
- \`EscrowApp/Components/\` — Blazor pages + shared components
- \`EscrowApp/Services/\` — Business services + payment integration
- \`EscrowApp/docs/\` — Architecture + API documentation (keep in sync)`;

const session = await joinSession({
    hooks: {
        onSessionStart: async () => {
            await session.log("📋 Context Optimizer loaded", { ephemeral: true });
            return {
                additionalContext: "NexTruzt.io EscrowApp: .NET 10 Blazor Server fintech escrow. Clean Architecture + CQRS/MediatR. Layers: Components/ (UI) → Features/ (handlers) → Models/Events (domain) ← Data/ (EF Core/PostgreSQL). Payment strategies: IFundHoldable/IFundReleasable/IFundCancellable. Key: code-behind required, docs/ must stay in sync, OWASP security-first, idempotency keys on payments.",
            };
        },

        onUserPromptSubmitted: async (input) => {
            const prompt = input.prompt;
            if (!prompt || typeof prompt !== "string") return;

            if (prompt.length > 2000) {
                return {
                    additionalContext: "Note: The user's prompt is quite long. Be efficient with context usage — prefer concise responses and avoid repeating the prompt back. If the conversation is getting long, suggest the user use /compact to optimize the context window.",
                };
            }
        },
    },

    tools: [
        {
            name: "project_summary",
            description: "Returns a concise, structured summary of the NexTruzt.io EscrowApp project including architecture diagram, key files, design patterns, and technology stack. Use this to quickly orient yourself without reading multiple files.",
            parameters: {
                type: "object",
                properties: {},
                additionalProperties: false,
            },
            handler: async () => {
                return PROJECT_SUMMARY;
            },
        },
    ],
});
