# Authentication & Identification Failures (OWASP A07)

Detailed detection patterns for authentication and session management vulnerabilities.

## Password Policy Issues

### Detection Patterns

```csharp
// ❌ VULNERABLE: Weak password policy
options.Password.RequiredLength = 4;
options.Password.RequireDigit = false;
options.Password.RequireNonAlphanumeric = false;
options.Password.RequireUppercase = false;
options.Password.RequireLowercase = false;

// ❌ VULNERABLE: No account lockout
options.Lockout.MaxFailedAccessAttempts = 100; // Effectively no lockout
```

### Remediation

```csharp
// ✅ SECURE: Strong password policy
options.Password.RequiredLength = 12;
options.Password.RequireDigit = true;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequireUppercase = true;
options.Password.RequireLowercase = true;
options.Password.RequiredUniqueChars = 4;

// ✅ SECURE: Account lockout
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
options.Lockout.AllowedForNewUsers = true;
```

## Password Storage

```csharp
// ❌ VULNERABLE: Weak hashing algorithms
var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
var hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
// Plain SHA-256 without salt is vulnerable to rainbow tables

// ❌ VULNERABLE: Reversible encryption for passwords
var encrypted = Encrypt(password, key); // Passwords must be hashed, not encrypted

// ✅ SECURE: ASP.NET Core Identity (uses PBKDF2 by default)
var hasher = new PasswordHasher<User>();
var hash = hasher.HashPassword(user, password);

// ✅ SECURE: BCrypt
var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

// ✅ SECURE: Argon2 (strongest option)
var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));
argon2.Salt = RandomNumberGenerator.GetBytes(16);
argon2.MemorySize = 65536; // 64 MB
argon2.Iterations = 3;
```

## JWT Token Security

### Detection Patterns

```csharp
// ❌ VULNERABLE: No audience/issuer validation
var parameters = new TokenValidationParameters
{
    ValidateAudience = false,
    ValidateIssuer = false,
    ValidateLifetime = false  // Tokens never expire!
};

// ❌ VULNERABLE: Weak signing key
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("short")); // < 256 bits

// ❌ VULNERABLE: Algorithm confusion (accepting "none")
var parameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = false // Accepts unsigned tokens!
};
```

### Remediation

```csharp
// ✅ SECURE: Full JWT validation
var parameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = "https://nexTruzt.io",
    ValidateAudience = true,
    ValidAudience = "nexTruzt-api",
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(2),
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(
        RandomNumberGenerator.GetBytes(32)) // 256-bit minimum
};

// ✅ SECURE: Short-lived tokens with refresh
options.TokenLifetime = TimeSpan.FromMinutes(15);
// Use refresh token rotation for long sessions
```

## Session Management

```csharp
// ❌ VULNERABLE: Insecure cookie settings
options.Cookie.SecurePolicy = CookieSecurePolicy.None;
options.Cookie.HttpOnly = false; // Accessible via JavaScript
options.Cookie.SameSite = SameSiteMode.None; // CSRF risk without HTTPS

// ✅ SECURE: Hardened cookie configuration
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.Cookie.HttpOnly = true;
options.Cookie.SameSite = SameSiteMode.Strict;
options.ExpireTimeSpan = TimeSpan.FromHours(2);
options.SlidingExpiration = true;
```

## Account Enumeration Prevention

```csharp
// ❌ VULNERABLE: Different responses reveal user existence
if (!userExists) return BadRequest("User not found");
if (!passwordValid) return BadRequest("Wrong password");

// ✅ SECURE: Generic error message
if (!userExists || !passwordValid)
    return BadRequest("Invalid email or password");

// ✅ SECURE: Same response time (prevent timing attacks)
if (!userExists)
{
    // Perform dummy hash to equalize response time
    BCrypt.Net.BCrypt.HashPassword("dummy", workFactor: 12);
    return BadRequest("Invalid email or password");
}
```

## Multi-Factor Authentication

```csharp
// ✅ SECURE: Enforce MFA for sensitive operations
[Authorize(Policy = "RequireMfa")]
[HttpPost("escrow/release")]
public async Task<IActionResult> ReleaseEscrow(ReleaseCommand cmd)

// Policy registration
options.AddPolicy("RequireMfa", policy =>
    policy.RequireClaim("amr", "mfa")); // Authentication Method Reference
```

## Brute Force Protection

```csharp
// ✅ SECURE: Rate limiting on auth endpoints
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

app.MapPost("/api/auth/login", LoginHandler)
    .RequireRateLimiting("auth");
```
