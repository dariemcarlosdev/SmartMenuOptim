# Cryptographic Failures (OWASP A02)

Detection patterns for encryption, hashing, key management, and data protection issues.

## Weak Hashing Algorithms

### Detection Patterns

```csharp
// ❌ VULNERABLE: MD5 (broken — collision attacks trivial)
var hash = MD5.Create().ComputeHash(data);

// ❌ VULNERABLE: SHA1 (deprecated — collision attacks demonstrated)
var hash = SHA1.Create().ComputeHash(data);

// ❌ VULNERABLE: Plain SHA-256 for passwords (no salt, fast = brute-forceable)
var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
```

### Remediation

```csharp
// ✅ SECURE: SHA-256/SHA-512 for data integrity (NOT passwords)
var hash = SHA256.HashData(data); // .NET 5+ static method

// ✅ SECURE: For passwords, use dedicated password hashing
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
// Or ASP.NET Core Identity's built-in hasher (PBKDF2)
var hasher = new PasswordHasher<User>();
var hash = hasher.HashPassword(user, password);
```

## Hardcoded Encryption Keys

### Detection Patterns

```csharp
// ❌ VULNERABLE: Key in source code
var key = Encoding.UTF8.GetBytes("MySecretKey12345");
var key = Convert.FromBase64String("dGhpcyBpcyBhIHNlY3JldCBrZXk=");

// ❌ VULNERABLE: Key in appsettings.json
{
    "Encryption": {
        "Key": "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnop"
    }
}

// ❌ VULNERABLE: IV reuse
var iv = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
```

### Remediation

```csharp
// ✅ SECURE: Key from Azure Key Vault
var key = await keyVaultClient.GetSecretAsync("encryption-key");

// ✅ SECURE: Data Protection API (manages keys automatically)
services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(blobUri)
    .ProtectKeysWithAzureKeyVault(keyUri, credential);

// Usage:
var protector = _dataProtectionProvider.CreateProtector("EscrowData");
var encrypted = protector.Protect(sensitiveData);
var decrypted = protector.Unprotect(encrypted);

// ✅ SECURE: Generate random IV for each encryption
using var aes = Aes.Create();
aes.GenerateIV(); // Random IV each time
```

## Insecure Random Number Generation

```csharp
// ❌ VULNERABLE: Predictable random for security-sensitive operations
var random = new Random();
var token = random.Next().ToString(); // Predictable!
var code = random.Next(100000, 999999).ToString(); // Guessable verification code

// ✅ SECURE: Cryptographic random
var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
```

## TLS/HTTPS Issues

### Detection Patterns

```csharp
// ❌ VULNERABLE: HTTPS not enforced
// Missing app.UseHttpsRedirection()
// Missing app.UseHsts()

// ❌ VULNERABLE: Allowing old TLS versions
// No TLS configuration = system default (may include TLS 1.0/1.1)

// ❌ VULNERABLE: Certificate validation disabled
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true // Accepts ANY cert!
};
```

### Remediation

```csharp
// ✅ SECURE: Enforce HTTPS and HSTS
app.UseHttpsRedirection();
app.UseHsts();

// ✅ SECURE: Configure HSTS properly
services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

// ✅ SECURE: Enforce TLS 1.2+
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(https =>
    {
        https.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });
});
```

## Connection String Security

```csharp
// ❌ VULNERABLE: Plaintext password in connection string
"Server=prod-db;Database=Escrow;User Id=admin;Password=P@ssw0rd123;"

// ❌ VULNERABLE: Unencrypted connection
"Server=prod-db;Database=Escrow;Encrypt=False;"

// ✅ SECURE: Managed Identity (no password!)
"Server=prod-db.database.windows.net;Database=Escrow;Authentication=Active Directory Managed Identity;"

// ✅ SECURE: If password required, from Key Vault + encrypted
"Server=prod-db;Database=Escrow;Encrypt=True;TrustServerCertificate=False;"
// Password injected from Key Vault at runtime
```

## Data at Rest Encryption

```csharp
// ✅ SECURE: Encrypt sensitive fields before storage
public sealed class EncryptedEscrowRepository : IEscrowRepository
{
    private readonly IDataProtector _protector;

    public async Task SaveAsync(Escrow escrow, CancellationToken ct)
    {
        var entity = new EscrowEntity
        {
            Id = escrow.Id,
            EncryptedBankAccount = _protector.Protect(escrow.BankAccount),
            Amount = escrow.Amount // Non-sensitive, no encryption needed
        };
        await _context.Escrows.AddAsync(entity, ct);
    }
}
```

## Sensitive Data in Client Storage

```csharp
// ❌ VULNERABLE: Sensitive data in localStorage (Blazor WASM)
await jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", jwt);

// ✅ SECURE: HttpOnly secure cookie (Blazor Server)
options.Cookie.HttpOnly = true;
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.Cookie.SameSite = SameSiteMode.Strict;
```

## Audit Checklist

| Check | Status |
|-------|--------|
| No MD5/SHA1 for security purposes | ☐ |
| Passwords hashed with bcrypt/Argon2/PBKDF2 | ☐ |
| No hardcoded encryption keys | ☐ |
| HTTPS enforced with HSTS | ☐ |
| TLS 1.2+ only | ☐ |
| Connection strings encrypted, no plaintext passwords | ☐ |
| Cryptographic random for tokens/codes | ☐ |
| Sensitive data encrypted at rest | ☐ |
| No sensitive data in client-side storage | ☐ |
