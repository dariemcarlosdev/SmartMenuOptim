# Environment Verification — Pre-Deploy Readiness Checks

Confirm the target environment has all required connectivity, secrets, and resources before deployment. Catch failures here instead of at runtime.

## Environment Verification Service

```csharp
public sealed class EnvironmentVerifier(
    IConfiguration config, IHttpClientFactory http, ILogger<EnvironmentVerifier> log)
{
    public async Task<IReadOnlyList<(string Name, bool Ok, string Detail)>> VerifyAsync(CancellationToken ct = default)
    {
        var r = new List<(string, bool, string)>();

        r.Add(await Try("PostgreSQL", async () => {
            await using var c = new NpgsqlConnection(config.GetConnectionString("DefaultConnection"));
            await c.OpenAsync(ct); return $"{c.Host}/{c.Database}";
        }));
        r.Add(await Try("Redis", async () => {
            var redis = await ConnectionMultiplexer.ConnectAsync(config.GetConnectionString("Redis")!);
            return $"Ping: {(await redis.GetDatabase().PingAsync()).TotalMilliseconds:F0}ms";
        }));
        r.Add(await Try("PaymentGateway", async () => {
            var resp = await http.CreateClient().GetAsync($"{config["Escrow:PaymentGatewayUrl"]}/health", ct);
            resp.EnsureSuccessStatusCode(); return $"{resp.StatusCode}";
        }));

        string[] vars = ["ASPNETCORE_ENVIRONMENT", "AZURE_CLIENT_ID"];
        var missing = vars.Where(v => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(v))).ToList();
        r.Add(("EnvVars", missing.Count == 0, missing.Count == 0 ? "All set" : $"Missing: {string.Join(", ", missing)}"));

        log.LogInformation("Environment: {R}", r.All(x => x.Item2) ? "PASSED" : "FAILED");
        return r;
    }

    private static async Task<(string, bool, string)> Try(string n, Func<Task<string>> f)
    { try { return (n, true, await f()); } catch (Exception ex) { return (n, false, ex.Message); } }
}
```

## Secret & TLS Verification

```csharp
// Key Vault — check for expiring secrets
public static async Task<IReadOnlyList<string>> CheckExpiringSecrets(
    SecretClient client, int warningDays = 30, CancellationToken ct = default)
{
    var warnings = new List<string>();
    await foreach (var p in client.GetPropertiesOfSecretsAsync(ct))
        if (p.ExpiresOn.HasValue && (p.ExpiresOn.Value - DateTimeOffset.UtcNow).Days <= warningDays)
            warnings.Add($"'{p.Name}' expires {p.ExpiresOn.Value:yyyy-MM-dd}");
    return warnings;
}

// TLS certificate expiry check
public static async Task<(string Host, int DaysLeft, bool Expiring)> CheckTls(
    string hostname, int warningDays = 30)
{
    using var tcp = new TcpClient();
    await tcp.ConnectAsync(hostname, 443);
    using var ssl = new SslStream(tcp.GetStream());
    await ssl.AuthenticateAsClientAsync(hostname);
    var days = (new X509Certificate2(ssl.RemoteCertificate!).NotAfter - DateTime.UtcNow).Days;
    return (hostname, days, days <= warningDays);
}
```

## Resource Check Script

```bash
#!/bin/bash
DISK=$(df / | awk 'NR==2{print $5}' | tr -d '%')
MEM=$(free | awk 'NR==2{printf "%.0f",$3/$2*100}')
[ "$DISK" -gt 85 ] && echo "⚠️ Disk $DISK%" || echo "✅ Disk $DISK%"
[ "$MEM" -gt 90 ] && echo "⚠️ Mem $MEM%" || echo "✅ Mem $MEM%"
for EP in db.nextruzt.io:5432 redis.nextruzt.io:6379; do
  timeout 5 bash -c "echo>/dev/tcp/${EP%:*}/${EP#*:}" 2>/dev/null \
    && echo "✅ $EP" || echo "🚫 $EP"
done
```

## Preflight Checklist

- [ ] PostgreSQL, Redis, external APIs all reachable
- [ ] All required environment variables set and non-empty
- [ ] All secrets populated; none expiring within 30 days
- [ ] Feature flags match expected release state
- [ ] TLS certificates valid and not expiring within 30 days
- [ ] Disk < 85%, memory < 90%, DNS resolving correctly
