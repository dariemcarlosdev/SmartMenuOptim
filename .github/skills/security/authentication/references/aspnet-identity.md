# ASP.NET Core Identity

## Setup and Configuration

### Package Installation
```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### Program.cs — Identity Registration
```csharp
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password policy
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredUniqueChars = 4;

        // Lockout policy
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.Run();
```

## Custom ApplicationUser

```csharp
using Microsoft.AspNetCore.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
```

## ApplicationDbContext

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Customize Identity table names for the escrow schema
        builder.Entity<ApplicationUser>(e => e.ToTable("Users", "identity"));
        builder.Entity<IdentityRole>(e => e.ToTable("Roles", "identity"));
        builder.Entity<IdentityUserRole<string>>(e => e.ToTable("UserRoles", "identity"));
        builder.Entity<IdentityUserClaim<string>>(e => e.ToTable("UserClaims", "identity"));
        builder.Entity<IdentityUserLogin<string>>(e => e.ToTable("UserLogins", "identity"));
        builder.Entity<IdentityUserToken<string>>(e => e.ToTable("UserTokens", "identity"));
        builder.Entity<IdentityRoleClaim<string>>(e => e.ToTable("RoleClaims", "identity"));
    }
}
```

## UserManager / SignInManager Usage

### Registration
```csharp
public sealed class RegisterUserHandler(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender)
    : IRequestHandler<RegisterUserCommand, Result>
{
    public async Task<Result> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return Result.Failure(result.Errors
                .Select(e => e.Description).ToArray());

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailSender.SendConfirmationEmailAsync(user.Email, token);

        return Result.Success();
    }
}
```

### Sign-In with Lockout
```csharp
public sealed class LoginHandler(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager)
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    public async Task<Result<AuthResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null || !user.IsActive)
            return Result<AuthResponse>.Failure("Invalid credentials.");

        var result = await signInManager.PasswordSignInAsync(
            user, request.Password,
            isPersistent: request.RememberMe,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Result<AuthResponse>.Failure(
                "Account locked. Try again in 15 minutes.");

        if (result.RequiresTwoFactor)
            return Result<AuthResponse>.TwoFactorRequired();

        if (!result.Succeeded)
            return Result<AuthResponse>.Failure("Invalid credentials.");

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return Result<AuthResponse>.Success(new AuthResponse(user.Id));
    }
}
```

## Two-Factor Authentication (2FA)

### Enable 2FA
```csharp
public async Task<Result<TwoFactorSetup>> EnableTwoFactorAsync(
    ClaimsPrincipal principal, CancellationToken ct)
{
    var user = await _userManager.GetUserAsync(principal)
        ?? throw new UnauthorizedAccessException();

    var key = await _userManager.GetAuthenticatorKeyAsync(user);

    if (string.IsNullOrEmpty(key))
    {
        await _userManager.ResetAuthenticatorKeyAsync(user);
        key = await _userManager.GetAuthenticatorKeyAsync(user);
    }

    var uri = GenerateQrCodeUri(user.Email!, key!);

    return Result<TwoFactorSetup>.Success(
        new TwoFactorSetup(key!, uri));
}
```

### Verify 2FA Token
```csharp
public async Task<Result> VerifyTwoFactorAsync(
    string userId, string code, CancellationToken ct)
{
    var user = await _userManager.FindByIdAsync(userId)
        ?? return Result.Failure("User not found.");

    var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
        code, isPersistent: false, rememberClient: false);

    if (!result.Succeeded)
        return Result.Failure("Invalid 2FA code.");

    return Result.Success();
}
```

## Email Confirmation

```csharp
public async Task<Result> ConfirmEmailAsync(
    string userId, string token, CancellationToken ct)
{
    var user = await _userManager.FindByIdAsync(userId);

    if (user is null)
        return Result.Failure("User not found.");

    var result = await _userManager.ConfirmEmailAsync(user, token);

    return result.Succeeded
        ? Result.Success()
        : Result.Failure("Email confirmation failed.");
}
```
