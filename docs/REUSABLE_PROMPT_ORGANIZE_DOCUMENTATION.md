# 📋 Reusable Prompt: Organize Documentation by Clean Architecture Concerns

> **Save this prompt for future use when organizing project documentation**

---

## 🎯 The Prompt

Copy and paste this prompt when you need to organize documentation in a Clean Architecture project:

---

```
Organize the documentation in my project following Clean Architecture principles. Please:

1. **ANALYZE** the current documentation structure:
   - List all docs in the root `docs/` folder
   - List all docs in each layer folder (`Domain/docs/`, `Application/docs/`, `Infrastructure/docs/`)
   - Identify the purpose/context of each document

2. **CREATE numbered folders** by concern in each location:
   
   **For root `docs/` (solution-wide):**
   - `01-Overview/` - Getting started, main documentation
   - `02-Architecture/` - Cross-cutting architecture docs
   - `03-API/` - API documentation
   - `04-Blazor/` or `04-UI/` - Frontend/UI docs
   - `05-Database/` - Database design docs
   - `06-Security/` - Security documentation
   - `07-Features/` - Business feature implementations
   - `08-Testing/` - Testing documentation
   
   **For `Domain/docs/`:**
   - `01-Entities/` - Base entities, global entities
   - `02-Aggregates/` - Aggregate root documentation
   - `03-ValueObjects/` - Value object documentation
   - `04-DomainServices/` - Domain service documentation
   - `05-Specifications/` - Query specifications
   - `06-Events/` - Domain events
   - `07-MultiTenancy/` - Multi-tenant patterns (if applicable)
   
   **For `Application/docs/`:**
   - `01-ApplicationServices/` - Use cases, application services
   - `02-DTOs/` - Data transfer objects
   - `03-EventHandlers/` - Domain event handlers
   - `04-Contracts/` - Service interfaces
   - `05-Integration/` - Layer integration guides
   
   **For `Infrastructure/docs/`:**
   - `01-Persistence/` - Database context, EF Core
   - `02-Repositories/` - Repository pattern
   - `03-Migrations/` - EF Core migrations
   - `04-ValueObjectMapping/` - Value object DB mapping
   - `05-Verification/` - Verification checklists
   - `06-BackgroundJobs/` - Background services

3. **MOVE documents** to appropriate locations:
   - Layer-specific docs → Move to respective layer's `docs/` folder
   - Solution-wide docs → Organize in root `docs/` folders
   - Use judgment: Repository docs → Infrastructure, Event handlers → Application, Entities → Domain

4. **CREATE README.md index files** in each docs folder with:
   - Folder structure visualization
   - Table of documents with descriptions
   - Quick start guide
   - Links to related documentation
   - Architecture overview diagram (ASCII art)

5. **CLEAN UP** old empty folders after moving files

6. **VERIFY** final structure with `tree` command

7. **UPDATE** all README files to reflect moved documents

Please follow the UPPERCASE_WITH_UNDERSCORES.md naming convention for documents.
```

---

## 📁 Expected Output Structure

After running this prompt, you should have:

```
your-project/
├── docs/                                    ← Solution-wide docs
│   ├── README.md                            ← Master index
│   ├── 01-Overview/
│   ├── 02-Architecture/
│   ├── 03-API/
│   ├── 04-Blazor/
│   ├── 05-Database/
│   ├── 06-Security/
│   ├── 07-Features/
│   └── 08-Testing/
│
├── YourProject.Domain/docs/                 ← Domain layer docs
│   ├── README.md                            ← Domain index
│   ├── LAYER_RESUME.md                      ← Layer overview
│   ├── 01-Entities/
│   ├── 02-Aggregates/
│   ├── 03-ValueObjects/
│   ├── 04-DomainServices/
│   ├── 05-Specifications/
│   ├── 06-Events/
│   └── 07-MultiTenancy/
│
├── YourProject.Application/docs/            ← Application layer docs
│   ├── README.md                            ← Application index
│   ├── 01-ApplicationServices/
│   ├── 02-DTOs/
│   ├── 03-EventHandlers/
│   ├── 04-Contracts/
│   └── 05-Integration/
│
└── YourProject.Infrastructure/docs/         ← Infrastructure layer docs
    ├── README.md                            ← Infrastructure index
    ├── LAYER_RESUME.md                      ← Layer overview
    ├── 01-Persistence/
    ├── 02-Repositories/
    ├── 03-Migrations/
    ├── 04-ValueObjectMapping/
    ├── 05-Verification/
    └── 06-BackgroundJobs/
```

---

## 🔄 Document Migration Rules

Use this table to determine where documents should go:

| Document Type | Destination |
|--------------|-------------|
| Domain events guide | `Domain/docs/06-Events/` |
| Entity relationships | `Domain/docs/01-Entities/` |
| Aggregate documentation | `Domain/docs/02-Aggregates/` |
| Value object docs | `Domain/docs/03-ValueObjects/` |
| Domain service docs | `Domain/docs/04-DomainServices/` |
| Specification patterns | `Domain/docs/05-Specifications/` |
| Multi-tenancy domain | `Domain/docs/07-MultiTenancy/` |
| Event handler docs | `Application/docs/03-EventHandlers/` |
| DTO documentation | `Application/docs/02-DTOs/` |
| Use case docs | `Application/docs/01-ApplicationServices/` |
| Layer integration | `Application/docs/05-Integration/` |
| Repository patterns | `Infrastructure/docs/02-Repositories/` |
| EF migrations | `Infrastructure/docs/03-Migrations/` |
| Database context | `Infrastructure/docs/01-Persistence/` |
| Background jobs | `Infrastructure/docs/06-BackgroundJobs/` |
| Value object mapping | `Infrastructure/docs/04-ValueObjectMapping/` |
| API documentation | `docs/03-API/` |
| Blazor/UI docs | `docs/04-Blazor/` |
| Security docs | `docs/06-Security/` |
| Feature implementations | `docs/07-Features/` |
| Testing docs | `docs/08-Testing/` |
| Architecture overview | `docs/02-Architecture/` |
| Getting started | `docs/01-Overview/` |

---

## 📝 README Template

Use this template for each docs folder:

```markdown
# [Layer Name] Documentation

> **[Description of what this layer contains]**

---

## 📁 Documentation Structure

\`\`\`
[Layer]/docs/
├── 📄 README.md                          ← You are here
├── 📁 01-[Folder]/                       ← [Description]
├── 📁 02-[Folder]/                       ← [Description]
└── ...
\`\`\`

---

## 📚 Documentation Index

### 📁 01-[FolderName]
*[Description]*

| Document | Description |
|----------|-------------|
| [DOCUMENT.md](01-Folder/DOCUMENT.md) | Description |

---

[Repeat for each folder]

---

## 🏗️ Architecture Overview

[ASCII diagram of the layer's responsibilities]

---

## 🚀 Quick Start

1. Step 1
2. Step 2
3. Step 3

---

## 📖 Related Documentation

| Layer | Location | Description |
|-------|----------|-------------|
| **Domain** | `Project.Domain/docs/` | Description |
| **Application** | `Project.Application/docs/` | Description |
| **Infrastructure** | `Project.Infrastructure/docs/` | Description |

---

*Last Updated: [Month Year]*
```

---

## ✅ Verification Checklist

After organization, verify:

- [ ] All old folders are removed
- [ ] All documents are in numbered folders
- [ ] Each docs folder has a README.md
- [ ] README indexes are updated with new paths
- [ ] No broken links in README files
- [ ] Layer-specific docs are in their layer folders
- [ ] Solution-wide docs are in root docs folder
- [ ] Naming convention is consistent (UPPERCASE_WITH_UNDERSCORES.md)

---

## 🔧 PowerShell Commands Used

```powershell
# Create new folder structure
New-Item -ItemType Directory -Path "docs\01-Overview" -Force

# Move files
Move-Item -Path "docs\old\file.md" -Destination "docs\01-Overview\file.md" -Force

# Remove empty old folders
Remove-Item -Path "docs\old-folder" -Recurse -Force

# Verify structure
tree "docs" /F
Get-ChildItem -Path "docs" -Recurse | Format-Table Name, Directory -AutoSize
```

---

## 📊 Example Results

| Location | Folders | Documents | Purpose |
|----------|---------|-----------|---------|
| `docs/` | 8 | ~25 | Solution-wide |
| `Domain/docs/` | 7 | ~20 | Domain layer |
| `Application/docs/` | 5 | ~5 | Application layer |
| `Infrastructure/docs/` | 6 | ~15 | Infrastructure layer |

---

## 🎯 Benefits of This Organization

1. **Discoverability** - Easy to find documentation
2. **Clean Architecture Alignment** - Docs match code structure
3. **Scalability** - Easy to add new docs
4. **Onboarding** - New developers can navigate easily
5. **Maintainability** - Clear ownership by layer
6. **Consistency** - Same pattern across all projects

---

*Created: February 2026*
*For: Clean Architecture / DDD Projects*
