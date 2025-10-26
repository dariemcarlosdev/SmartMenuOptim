# Base Entities

This folder contains base/fundamental entities that are neither tenant-specific nor global in nature. These entities typically represent:

- Value Objects
- Data Transfer Objects (DTOs)
- Shared Models
- Common Response Types

## Current Entities
- `InsightResponse.cs`: A DTO for recommendation responses
  - Reason: Acts as a pure data container without tenant or global concerns
  - Contains only basic properties (`ConfidenceScore`, `Recommendation`)
  - No dependencies on tenant or global contexts

## Candidates for Base Folder
Entities that should be moved or created here include:
- Response/Request DTOs
- Shared enums
- Configuration objects
- Any models that don't need tenant isolation or global accessibility

## Guidelines for Base Entities
1. Should not inherit from `TenantEntityBase` or `GlobalEntity`
2. Should not contain navigation properties to tenant-specific entities
3. Should be simple, self-contained data structures
4. Should be usable across both tenant and global contexts