# Injection Prevention (OWASP A03)

Detailed detection patterns and remediation for SQL injection, XSS, command injection, and related attacks.

## SQL Injection

### Detection Patterns

```csharp
// ❌ VULNERABLE: String concatenation in SQL
var sql = $"SELECT * FROM Users WHERE Email = '{email}'";
await context.Database.ExecuteSqlRawAsync(sql);

// ❌ VULNERABLE: String.Format in SQL
var sql = string.Format("SELECT * FROM Escrows WHERE Status = '{0}'", status);

// ❌ VULNERABLE: Stored procedure with concatenation
var sql = $"EXEC sp_GetEscrow @Id = {id}"; // Injection if id is user-controlled string
```

### Remediation

```csharp
// ✅ SECURE: EF Core LINQ (auto-parameterized)
var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

// ✅ SECURE: Parameterized raw SQL
await context.Database.ExecuteSqlRawAsync(
    "SELECT * FROM Users WHERE Email = {0}", email);

// ✅ SECURE: ExecuteSqlInterpolated (parameterizes interpolation)
await context.Database.ExecuteSqlInterpolatedAsync(
    $"SELECT * FROM Users WHERE Email = {email}");

// ✅ SECURE: Dapper with parameters
var user = await connection.QueryFirstOrDefaultAsync<User>(
    "SELECT * FROM Users WHERE Email = @Email",
    new { Email = email });

// ✅ SECURE: Stored procedure via EF Core
var escrows = await context.Escrows
    .FromSqlRaw("EXEC sp_GetEscrowsByStatus @p0", status)
    .ToListAsync(ct);
```

## Cross-Site Scripting (XSS)

### Detection Patterns

```csharp
// ❌ VULNERABLE: Raw HTML rendering in MVC
@Html.Raw(userComment)

// ❌ VULNERABLE: JavaScript injection
<script>var data = '@Model.UserInput';</script>

// ❌ VULNERABLE: innerHTML in JS interop
await jsRuntime.InvokeVoidAsync("setContent", userHtml);
// Where JS does: element.innerHTML = content;
```

### Remediation

```csharp
// ✅ SECURE: Blazor auto-encodes by default
<p>@userComment</p>  // Blazor automatically HTML-encodes

// ✅ SECURE: If raw HTML needed, sanitize first
@((MarkupString)HtmlSanitizer.Sanitize(userComment))

// ✅ SECURE: Use textContent in JS interop
await jsRuntime.InvokeVoidAsync("setTextContent", userText);
// Where JS does: element.textContent = content;

// ✅ SECURE: CSP header to block inline scripts
ctx.Response.Headers.Append("Content-Security-Policy",
    "default-src 'self'; script-src 'self'");
```

## Command Injection

### Detection Patterns

```csharp
// ❌ VULNERABLE: User input in process arguments
var process = Process.Start("cmd.exe", $"/c ping {userHost}");
// Attacker: userHost = "localhost & del /F /Q C:\\*"

// ❌ VULNERABLE: Shell execution with user input
Process.Start(new ProcessStartInfo
{
    FileName = "bash",
    Arguments = $"-c \"echo {userInput}\""
});
```

### Remediation

```csharp
// ✅ SECURE: Validate and sanitize input
if (!IPAddress.TryParse(userHost, out _) && !Uri.CheckHostName(userHost).Equals(UriHostNameType.Dns))
    return BadRequest("Invalid host");

// ✅ SECURE: Use API instead of shell commands
var ping = new Ping();
var reply = await ping.SendPingAsync(validatedHost, timeout: 5000);

// ✅ SECURE: If shell is required, use argument array (no shell interpretation)
Process.Start(new ProcessStartInfo
{
    FileName = "/usr/bin/ping",
    ArgumentList = { "-c", "4", validatedHost },
    UseShellExecute = false
});
```

## Path Injection / Directory Traversal

### Detection Patterns

```csharp
// ❌ VULNERABLE: User input in file path
var filePath = Path.Combine("uploads", userFileName);
var content = await System.IO.File.ReadAllTextAsync(filePath);
// Attacker: userFileName = "../../appsettings.json"
```

### Remediation

```csharp
// ✅ SECURE: Validate path stays within allowed directory
var basePath = Path.GetFullPath("uploads");
var fullPath = Path.GetFullPath(Path.Combine(basePath, userFileName));

if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
    return BadRequest("Invalid file path");

// ✅ SECURE: Strip path separators from filename
var safeName = Path.GetFileName(userFileName); // Removes directory components
var fullPath = Path.Combine(basePath, safeName);
```

## LDAP Injection

```csharp
// ❌ VULNERABLE: User input in LDAP filter
var filter = $"(&(uid={username})(userPassword={password}))";

// ✅ SECURE: Escape special LDAP characters
var safeUsername = LdapEncoder.Encode(username);
var filter = $"(&(uid={safeUsername}))";
// Then verify password through LDAP bind, not filter
```

## Header Injection

```csharp
// ❌ VULNERABLE: User input in response headers
Response.Headers.Append("X-Custom", userInput);
// Attacker: userInput = "value\r\nSet-Cookie: admin=true"

// ✅ SECURE: Validate/sanitize header values
var safeValue = userInput.Replace("\r", "").Replace("\n", "");
Response.Headers.Append("X-Custom", safeValue);
```
