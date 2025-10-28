# Entity Framework Core Migration Guide

This guide explains how to create and apply Entity Framework Core migrations for the `AppDbContext` in this project.

## Prerequisites
- .NET 8 SDK or later
- PostgreSQL database (connection string configured in the API project)
- EF Core CLI tools installed (`dotnet tool install --global dotnet-ef`)

## Project Structure
- **DbContext & Migrations:** `SmartMenuOptim.Shared` (this project)
- **Startup & Configuration:** `SmartMenuOptim.API` (contains connection string)

## Creating a Migration
1. Open a terminal at the solution root.
2. Run the following command (replace `MigrationName` with a descriptive name):

   ```sh
   dotnet ef migrations add MigrationName --project SmartMenuOptim.Shared --startup-project SmartMenuOptim.API
   ```
   - This creates a migration in the `SmartMenuOptim.Shared` project using the configuration from `SmartMenuOptim.API`.

## Applying Migrations (Updating the Database)
1. Run the following command:

   ```sh
   dotnet ef database update --project SmartMenuOptim.Shared --startup-project SmartMenuOptim.API
   ```
   - This updates the database schema to match the current model and migrations.

## Troubleshooting
- If you see errors about existing tables or pending model changes, ensure your database is in sync with your migrations. You may need to resolve conflicts manually or reset the database in development.
- For more info, see the [EF Core documentation](https://learn.microsoft.com/ef/core/managing-schemas/migrations/).

---
**Note:** Always back up your production database before applying migrations.
