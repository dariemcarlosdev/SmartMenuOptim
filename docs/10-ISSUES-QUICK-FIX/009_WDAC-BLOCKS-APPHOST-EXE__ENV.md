# 009 — "For your protection, admin is not allowing this app" (WDAC blocks apphost .exe)

**Layer**: Environment / Setup — SmartMenuOptim.API + SmartMenuOptim.Server
**Feature**: Run / debug (F5, CLI)
**Severity**: High (app won't launch from the built .exe)
**Status**: ✅ Fixed
**Date Found**: 2026-07-26
**Date Fixed**: 2026-07-26
**Branch**: `env-dev/feature/WIP-refactor-Clean-Architecture-DDD`

---

## Summary

Running the app failed with a Windows block message: *"For your protection, your administrator is not allowing access to this app"*, pointing at the built executables:

- `SmartMenuOptim.API\bin\Debug\net8.0\SmartMenuOptim.API.exe`
- `SmartMenuOptim.Server\bin\Debug\net9.0\SmartMenuOptim.Server.exe`

Right-click → **Properties → Unblock** did **not** help — the checkbox kept coming back / had no effect.

---

## Error Message

```
For your protection, your administrator is not allowing access to this app.
```

Reproduced from PowerShell:

```
APPHOST BLOCKED: This command cannot be run due to the error: Access is denied.
```

---

## Root Cause

**Not** a Mark-of-the-Web / download-zone problem (that is what the "Unblock" checkbox clears). Verified there was **no** `Zone.Identifier` stream on the `.exe`, so Unblock had nothing to remove — that is why it never worked.

The real cause is **WDAC (Windows Defender Application Control)** — a corporate code-integrity policy pushed by IT. This is an Enterprise-managed machine.

```
Get-CimInstance Win32_DeviceGuard -Namespace root\Microsoft\Windows\DeviceGuard
  CodeIntegrityPolicyEnforcementStatus = 2   # 0=Off, 1=Audit, 2=Enforced
```

Enforced WDAC denies execution of **unsigned** binaries from user-writable paths. The .NET build produces an unsigned **apphost** (`Project.exe`), which WDAC blocks. `Get-AuthenticodeSignature` / `Import-Module Microsoft.PowerShell.Security` were also blocked, confirming Constrained Language Mode under WDAC.

Why the CLI already worked: `dotnet run` executes the app as a **managed DLL** under `dotnet.exe`, which is **Microsoft-signed** and allowed by WDAC. VS **F5** failed because launch profiles use `commandName: "Project"`, which VS runs via the apphost `.exe`.

(Controlled Folder Access was also on but in Audit mode — `EnableControlledFolderAccess = 2` — not the culprit.)

---

## Fix Applied

Stop emitting the blocked apphost `.exe`. Then both CLI and VS launch through the signed `dotnet.exe` host running the `.dll`.

Added to both web project `.csproj` `PropertyGroup`:

```xml
<!-- WDAC (corporate code-integrity policy) blocks unsigned apphost .exe. Launch via signed dotnet.exe host instead. -->
<UseAppHost>false</UseAppHost>
```

Files:
- `SmartMenuOptim.API/SmartMenuOptim.API.csproj`
- `SmartMenuOptim.Server/SmartMenuOptim.Server.csproj`

Then clean + rebuild:

```bash
rm -f SmartMenuOptim.API/bin/Debug/net8.0/SmartMenuOptim.API.exe \
      SmartMenuOptim.Server/bin/Debug/net9.0/SmartMenuOptim.Server.exe
dotnet build SmartMenuOptim.sln
```

With `UseAppHost=false`, the build emits no `.exe`; VS `commandName: "Project"` falls back to `dotnet Project.dll`.

---

## Verification

```
# Old apphost reproduced the block:
APPHOST BLOCKED: ... Access is denied.

# Managed DLL under signed dotnet host — NOT blocked:
Start-Process dotnet -ArgumentList SmartMenuOptim.API.dll   # launched, exited on app config, no WDAC deny

# After fix:
dotnet build SmartMenuOptim.sln   -> 0 Errors
ls .../bin/Debug/net8.0/*.exe     -> no .exe (good)
ls .../bin/Debug/net9.0/*.exe     -> no .exe (good)
```

Run methods after fix:
- CLI: `dotnet run --project SmartMenuOptim.API --launch-profile https.Development`
- VS **F5**: now launches `dotnet Foo.dll` instead of the exe. Works.

---

## Prevention / Notes

- **Do not** rely on Properties → Unblock for this — it only clears Mark-of-the-Web, not WDAC. Check `CodeIntegrityPolicyEnforcementStatus` to confirm WDAC.
- **Self-contained / standalone publish still emits an apphost** and will be blocked by WDAC. Use framework-dependent publish (`dotnet Foo.dll`), or get the binary signed.
- Permanently allowing unsigned exes requires IT to amend the WDAC policy — outside developer control.

Env: Windows 11 Enterprise (WDAC Enforced), .NET 8 (API) / .NET 9 (Server).
