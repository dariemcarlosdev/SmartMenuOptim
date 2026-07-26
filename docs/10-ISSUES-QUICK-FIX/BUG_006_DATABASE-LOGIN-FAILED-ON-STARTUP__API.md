# 006 — "Database Login Failed on Startup" (Postgres password rejected)

**Layer**: SmartMenuOptim.API — startup / DbSeeder
**Feature**: Database connection / seeding
**Severity**: Critical (API won't start)
**Status**: ✅ Fixed
**Date Found**: 2026-07-16
**Date Fixed**: 2026-07-16
**Branch**: `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`

---

## Summary

API crashed on startup while seeding the database. The local PostgreSQL server rejected the `postgres` login — the password stored in user-secrets did not match the password on the local Postgres server.

---

## Error Message

```
Npgsql.PostgresException
  Message=28P01: password authentication failed for user "postgres"
   at SmartMenuOptim.API.Data.DbSeeder.SeedAsync() ... DbSeeder.cs:line 42
   at SmartMenuOptim.API.Extensions.WebApplicationExtensions.InitializeDataBaseAsync() ... line 143
   at SmartMenuOptim.API.Program.Main() ... Program.cs:line 82
```

`28P01` = PostgreSQL error code for **invalid password**.

---

## Root Cause

The API loads its connection string from **user-secrets** in dev (not `appsettings.json`). `Program.cs` clears all config sources, then adds user-secrets when `IsDevelopment()` and not running in Docker.

- UserSecretsId: `20449e9f-a64f-40fe-818e-e11a92349ba7`
- Secret connection string password: `copoadmin123`
- Actual local Postgres server password: **different** (never set to `copoadmin123`)

So the client sent `copoadmin123`, the server rejected it.

---

## Fix Applied

Reset the local Postgres `postgres` password to match the secret, using temporary trust auth (no code or secret change).

1. Backup + edit `C:\Program Files\PostgreSQL\17\data\pg_hba.conf` — set the two localhost lines from `scram-sha-256` to `trust`:
   ```
   host    all             all             127.0.0.1/32            trust
   host    all             all             ::1/128                 trust
   ```
2. Restart service (admin PowerShell — non-elevated fails "Operation not permitted"):
   ```powershell
   Restart-Service postgresql-x64-17 -Force
   ```
3. Reset password:
   ```sql
   ALTER USER postgres WITH PASSWORD 'copoadmin123';
   ```
4. Revert `pg_hba.conf` back to `scram-sha-256`, restart again.

---

## App Database Credentials (local dev)

| Field | Value |
|-------|-------|
| Host | `localhost` |
| Port | `5432` |
| Database | `SmartMenuDb` |
| Username | `postgres` |
| Password | `copoadmin123` |

Connection string (user-secrets `ConnectionStrings:DefaultConnection`):
```
Server=localhost;Port=5432;Database=SmartMenuDb;User Id=postgres;Password=copoadmin123;TrustServerCertificate=True;
```

> Neon cloud connection string sits commented as an alternate in secrets.json.

---

## Verification

```
PGPASSWORD='copoadmin123' psql -U postgres -h localhost -p 5432 -d postgres -c "SELECT 1;"
 -> auth ok
```

`pg_hba.conf` restored to secure `scram-sha-256`. API starts, DbSeeder connects, `28P01` gone.

---

## Prevention

If `28P01` returns, either the local Postgres password changed or the secret changed. Realign the two — reset the server password (steps above) **or** update the secret:

```bash
cd SmartMenuOptim.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=5432;Database=SmartMenuDb;User Id=postgres;Password=<real>;TrustServerCertificate=True;"
```

Env: PostgreSQL 17.5, Windows service `postgresql-x64-17`.
