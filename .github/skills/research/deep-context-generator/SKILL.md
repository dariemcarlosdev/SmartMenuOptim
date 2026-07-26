---
name: deep-context-generator
description: "Generate LLM-optimized codebase context for onboarding, architecture understanding, and pre-refactoring analysis"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: research
  triggers: generate context, codebase overview, onboard me, explain architecture, project summary, context dump, understand codebase, map the code
  role: analyzer
  scope: analysis
  platforms: copilot-cli, claude, gemini
  output-format: report
  related-skills: codebase-explorer, architecture-reviewer, spec-miner
---

# Deep Context Generator

An LLM-optimized codebase context generation skill that produces structured, token-efficient summaries of project architecture, dependencies, abstractions, and conventions. Designed for onboarding, pre-refactoring analysis, and feeding context to AI assistants. Extracts signal from .NET solutions using native tooling — no external dependencies required.

## When to Use This Skill

- "Help me understand this codebase" or "Onboard me"
- "Generate context for an AI assistant"
- "Summarize the architecture" or "Map the code"
- Before a major refactoring to understand impact surface
- When switching to an unfamiliar module or project
- Preparing context for a design review or tech spike

## Core Workflow

1. **Extract Project Structure** — Parse solution topology: `Get-Content *.sln | Select-String 'Project\('` for project references. Map project hierarchy with `Get-ChildItem -Recurse -Include *.csproj`. Identify layers (Domain, Application, Infrastructure, Presentation) from naming conventions and project references. Load `references/dotnet-context.md` for .NET-specific extraction patterns.
   - **Checkpoint:** Solution structure tree complete with layer classification.

2. **Discover Key Abstractions** — Find interfaces: `Select-String -Pattern '^\s*(public|internal)\s+interface\s+I' -Recurse -Include *.cs`. Find base classes, records, and value objects. Extract DI registrations from `Program.cs` and `ServiceCollectionExtensions`. Map the dependency graph from `using` statements and constructor injection.
   - **Checkpoint:** Abstraction catalog with interface→implementation mappings.

3. **Map Entry Points & Boundaries** — Identify API controllers: `Select-String -Pattern '\[ApiController\]|\[Route\(' -Recurse`. Find Blazor pages: `Select-String -Pattern '@page' -Recurse -Include *.razor`. Map MediatR handlers: `Select-String -Pattern ': IRequestHandler<' -Recurse`. Identify database context and external service integrations.
   - **Checkpoint:** All entry points, command/query handlers, and external boundaries cataloged.

4. **Extract Conventions & Configuration** — Read `AGENTS.md`, `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`. Detect patterns: naming conventions, error handling style, logging approach, authentication setup. Load `references/compression-strategies.md` for token-efficient output formatting.
   - **Checkpoint:** Convention summary written; config files cataloged.

5. **Compile Context Document** — Assemble findings into a structured, LLM-optimized context document. Apply compression: use tree notation for structure, bullet lists for abstractions, tables for mappings. Load `references/context-template.md` for the output template. Target <4000 tokens for the summary layer.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Context Template | `references/context-template.md` | Assembling the final context document |
| .NET Context Patterns | `references/dotnet-context.md` | Extracting .NET-specific structure |
| Compression Strategies | `references/compression-strategies.md` | Optimizing context for token limits |

## Quick Reference

```powershell
# Solution structure
Get-Content *.sln | Select-String 'Project\(' | ForEach-Object { $_.Line -replace '.*"([^"]+)".*', '$1' }

# Key interfaces
Select-String -Pattern '^\s*(public|internal)\s+interface\s+I' -Recurse -Include *.cs |
  ForEach-Object { "$($_.Filename):$($_.LineNumber) $($_.Line.Trim())" }

# Entry points (Controllers + Blazor pages)
Select-String -Pattern '\[ApiController\]|@page\s+"/' -Recurse -Include *.cs,*.razor

# MediatR handlers
Select-String -Pattern ': IRequestHandler<|: IRequest<' -Recurse -Include *.cs

# DI registrations
Select-String -Pattern 'services\.(AddScoped|AddTransient|AddSingleton)<' -Recurse -Include *.cs

# Package references
Select-String -Pattern '<PackageReference' -Recurse -Include *.csproj |
  ForEach-Object { $_.Line.Trim() }

# Dependency graph (using statements)
Select-String -Pattern '^using\s+' -Recurse -Include *.cs |
  Group-Object { $_.Line.Trim() } | Sort-Object Count -Descending | Select-Object -First 20
```

## Context Output Layers

| Layer | Content | Target Tokens |
|-------|---------|--------------|
| **L1: Summary** | Tech stack, architecture, project count, key patterns | ~500 |
| **L2: Structure** | Project tree, layer mapping, dependency graph | ~1000 |
| **L3: Abstractions** | Interfaces, handlers, entities, value objects | ~1500 |
| **L4: Detail** | Entry points, DI graph, conventions, config | ~2000 |

## Constraints

### MUST DO
- Produce output that is immediately usable by an LLM without further processing
- Include file paths relative to solution root
- Classify each project into an architecture layer
- Map at least the top 10 interfaces to their implementations
- Include NuGet package list with versions

### MUST NOT
- Do not include file contents verbatim — summarize and reference
- Do not include auto-generated code, `bin/`, `obj/`, or migration files in the summary
- Do not exceed 6000 tokens for the full context document
- Do not load all source files into memory — use targeted searches
- Do not omit the conventions section — it is critical for AI code generation accuracy
