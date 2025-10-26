# Global Entities

This folder contains entities that operate across all tenants and are not scoped to a specific restaurant. These entities manage system-wide concerns and cross-tenant functionality.

## Current Entities
- `AdminUser.cs`: System administrators who can manage multiple restaurants
  - Inherits from `UserBase`
  - Can own/manage multiple restaurants
  - Not restricted to a single tenant
  
- `Customer.cs`: Users who can interact with multiple restaurants
  - Inherits from `UserBase`
  - Can review dishes across different restaurants
  - Single account works across all restaurants

- `BusinessRule.cs`: System-wide configuration and rules
  - Used by AdminUsers across all restaurants
  - Contains global thresholds and settings
  - Not specific to any single restaurant

- `UserBase.cs`: Abstract base class for user authentication
  - Provides common authentication properties
  - Used by both global user types (Admin, Customer)

## Guidelines for Global Entities
1. Should inherit from `GlobalEntity` or be marked as global
2. May have relationships with multiple tenants
3. Should handle system-wide concerns
4. Must not be scoped to a single restaurant