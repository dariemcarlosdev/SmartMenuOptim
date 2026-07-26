# Configuration Validation — Fail-Fast Startup Verification

Configuration validation ensures the app fails fast at startup when settings are missing or invalid — not at runtime when a user hits an unconfigured code path.

## Options Validation with DataAnnotations

```csharp
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required] public string ConnectionString { get; init; } = string.Empty;
    [Range(1, 300)] public int CommandTimeoutSeconds { get; init; } = 30;
    [Range(1, 200)] public int MaxPoolSize { get; init; } = 100;
    public bool EnableSensitiveDataLogging { get; init; }
}

// Program.cs — fail-fast with ValidateOnStart
builder.Services
    .AddOptionsWithValidateOnStart<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .ValidateDataAnnotations()
    .Validate(opts =>
    {
        if (builder.Environment.IsProduction())
        {
            if (opts.EnableSensitiveDataLogging) return false;
            if (opts.ConnectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }, "Production must not enable sensitive logging or use localhost.");
```

`ValidateOnStart()` throws `OptionsValidationException` during startup if validation fails — before accepting traffic.

## Required Sections Detection

```csharp
public static class ConfigurationGuard
{
    private static readonly string[] RequiredSections =
        ["Database", "Escrow", "AzureAd", "Logging", "Redis"];

    public static void ValidateRequiredSections(IConfiguration configuration)
    {
        var missing = RequiredSections
            .Where(s => !configuration.GetSection(s).Exists()).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Missing config sections: {string.Join(", ", missing)}");
    }
}
// Call in Program.cs before builder.Build()
ConfigurationGuard.ValidateRequiredSections(builder.Configuration);
```

## Common Misconfigurations

| Misconfiguration | Risk | Detection |
|---|---|---|
| Placeholder values (`TODO`, `CHANGEME`) | Runtime failure | Regex scan on config values |
| `localhost` in production connection strings | Wrong database | Environment-conditional validation |
| `EnableSensitiveDataLogging` in prod | PII in logs | Options validation rule |
| Missing `ASPNETCORE_ENVIRONMENT` | Wrong config loaded | Startup guard |
| Expired secrets / certificates | Auth failures | Expiry check on startup |
| Debug log level in production | Perf + disk cost | Validate LogLevel ≥ Warning |

## Placeholder Scanner

```csharp
public static class ConfigurationScanner
{
    private static readonly string[] Patterns =
        ["TODO", "CHANGEME", "PLACEHOLDER", "xxx", "your-", "replace-me"];

    public static IReadOnlyList<string> FindPlaceholders(IConfiguration config)
    {
        var findings = new List<string>();
        foreach (var kvp in config.AsEnumerable().Where(x => x.Value is not null))
            foreach (var p in Patterns)
                if (kvp.Value!.Contains(p, StringComparison.OrdinalIgnoreCase))
                    findings.Add($"{kvp.Key} contains '{p}'");
        return findings;
    }
}
```

## Preflight Checklist

- [ ] All `IOptions<T>` use `ValidateOnStart()` — no lazy validation
- [ ] Required sections present (`Database`, `Escrow`, `AzureAd`, etc.)
- [ ] No placeholder values in any configuration key
- [ ] Production config has no `localhost` or `Debug` log levels
- [ ] Secrets populated and non-empty in target environment
- [ ] Feature flags match expected state for this release
