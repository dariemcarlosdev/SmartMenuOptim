# Identity & Profile Management System Implementation Guide

## Overview
This guide outlines the implementation steps for the Smart Menu Optimization Profile Management System in a Blazor-based architecture.

## Project Structure
```
SmartMenuOptim/
??? SmartMenuOptim.API/          # Backend API
??? SmartMenuOptim.Server/       # Blazor Server
??? SmartMenuOptim.Shared/       # Shared Models & Logic
??? SmartMenuOptim.Infrastructure/# Infrastructure Layer
??? SmartMenuOptim.Tests/        # Test Projects
```

## 1. Database & Entity Configuration

### 1.1 Entity Framework Core Setup
```csharp
// AppDbContext.cs
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Profile relationships
        modelBuilder.Entity<ApplicationUser>()
            .HasOne(au => au.AdminProfile)
            .WithOne(a => a.ApplicationUser)
            .HasForeignKey<AdminUser>(a => a.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add similar configurations for StaffMember and Customer
    }
}
```

### 1.2 Migration Steps
1. Create migration:
```bash
dotnet ef migrations add UpdateProfileRelationships --project SmartMenuOptim.API
```
2. Update database:
```bash
dotnet ef database update --project SmartMenuOptim.API
```

## 2. Integration Testing

### 2.1 Test Project Setup
```csharp
// SmartMenuOptim.Tests/Profiles/ProfileTests.cs
public class ProfileTests : IClassFixture<TestDatabaseFixture>
{
    [Fact]
    public async Task AdminProfile_Creation_ShouldSyncWithApplicationUser()
    {
        // Arrange
        await using var context = new AppDbContext(_options);
        var adminService = new AdminProfileService(context);

        // Act
        var result = await adminService.CreateProfile(adminDto);

        // Assert
        Assert.NotNull(result.ApplicationUser);
        Assert.Equal(ProfileType.Admin, result.ApplicationUser.ProfileType);
    }
}
```

### 2.2 Test Categories
- Profile Creation Tests
- Profile Synchronization Tests
- Authorization Tests
- Tenant Isolation Tests

## 3. Authorization Implementation

### 3.1 Policy-Based Authorization
```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin")
              .RequireClaim("Permission", "ManageUsers"));
});
```

### 3.2 Custom Authorization Handlers
```csharp
// AdminPermissionHandler.cs
public class AdminPermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var user = context.User;
        var permissions = user.Claims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value);

        if (permissions.Contains(requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
```

### 3.3 Tenant Validation Middleware
```csharp
// TenantValidationMiddleware.cs
public class TenantValidationMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.User.FindFirst("TenantId")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            context.Response.StatusCode = 403;
            return;
        }
        // Continue pipeline
        await _next(context);
    }
}
```

## 4. Blazor Components Implementation

### 4.1 Profile Management Components
```razor
@* Components/Profile/ProfileEditor.razor *@
@inherits ComponentBase
@inject IProfileService ProfileService

<EditForm Model="@profile" OnValidSubmit="@HandleValidSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <div class="form-group">
        <label for="name">Name</label>
        <InputText id="name" @bind-Value="profile.Name" class="form-control" />
    </div>

    <button type="submit" class="btn btn-primary">Save</button>
</EditForm>

@code {
    private ProfileModel profile = new();

    private async Task HandleValidSubmit()
    {
        await ProfileService.UpdateProfile(profile);
    }
}
```

### 4.2 Authorization Components
```razor
@* Components/Authorization/PermissionView.razor *@
<AuthorizeView Policy="@Policy">
    <Authorized>
        @ChildContent
    </Authorized>
    <NotAuthorized>
        @NotAuthorizedContent
    </NotAuthorized>
</AuthorizeView>

@code {
    [Parameter] public string Policy { get; set; }
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public RenderFragment NotAuthorizedContent { get; set; }
}
```

## Missing Components Implementation

### 1. Base Interfaces
```csharp
// SmartMenuOptim.Shared/Interfaces/IProfileValidator.cs
public interface IProfileValidator
{
    Task<ValidationResult> ValidateProfile<TProfile>(TProfile profile) where TProfile : class;
    Task<ValidationResult> ValidateProfileCreation<TProfile>(ProfileCreationDto dto) where TProfile : class;
}

// SmartMenuOptim.Shared/Interfaces/IProfileRepository.cs
public interface IProfileRepository<TProfile> where TProfile : class
{
    Task<TProfile?> GetByIdAsync(string userId);
    Task<TProfile> CreateAsync(TProfile profile);
    Task<TProfile> UpdateAsync(TProfile profile);
    Task DeleteAsync(string userId);
    Task<bool> ExistsAsync(string userId);
}

// SmartMenuOptim.Shared/Interfaces/IProfileService.cs
public interface IProfileService
{
    Task<Result<TProfile>> CreateProfile<TProfile>(ProfileCreationDto dto) where TProfile : class;
    Task<Result<TProfile>> UpdateProfile<TProfile>(ProfileUpdateDto dto) where TProfile : class;
    Task<Result<bool>> DeleteProfile(string userId);
    Task<Result<TProfile>> GetProfile<TProfile>(string userId) where TProfile : class;
}
```

### 2. DTOs and Models
```csharp
// SmartMenuOptim.Shared/Models/Profile/ProfileCreationDto.cs
public record ProfileCreationDto
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
    public ProfileType ProfileType { get; init; }
    public string? PhoneNumber { get; init; }
    public Dictionary<string, string>? AdditionalData { get; init; }
}

// SmartMenuOptim.Shared/Models/Profile/ProfileUpdateDto.cs
public record ProfileUpdateDto
{
    public required string UserId { get; init; }
    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public Dictionary<string, string>? UpdatedFields { get; init; }
}

// SmartMenuOptim.Shared/Models/Profile/ProfileValidationResult.cs
public record ValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = [];
    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(params string[] errors) => 
        new() { IsValid = false, Errors = errors.ToList() };
}
```

### 3. Profile Service Implementation
```csharp
// SmartMenuOptim.Server/Services/ProfileService.cs
public class ProfileService : IProfileService
{
    private readonly IProfileRepository<TProfile> _repository;
    private readonly IProfileValidator _validator;
    private readonly ILogger<ProfileService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileService(
        IProfileRepository<TProfile> repository,
        IProfileValidator validator,
        ILogger<ProfileService> logger,
        UserManager<ApplicationUser> userManager)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<Result<TProfile>> CreateProfile<TProfile>(ProfileCreationDto dto)
        where TProfile : class
    {
        try
        {
            var validationResult = await _validator.ValidateProfileCreation<TProfile>(dto);
            if (!validationResult.IsValid)
            {
                return Result<TProfile>.Failure(
                    validationResult.Errors.FirstOrDefault() ?? "Invalid profile data");
            }

            // Create ApplicationUser
            var user = new ApplicationUser
            {
                UserName = dto.Username,
                Email = dto.Email,
                FullName = dto.FullName,
                ProfileType = dto.ProfileType
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return Result<TProfile>.Failure(
                    createResult.Errors.FirstOrDefault()?.Description ?? "Failed to create user");
            }

            // Create profile
            var profile = await _repository.CreateAsync(/* map dto to profile */);
            
            return Result<TProfile>.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile for {Username}", dto.Username);
            return Result<TProfile>.Failure("Failed to create profile");
        }
    }
}
```

### 4. Profile Repository Implementation
```csharp
// SmartMenuOptim.Infrastructure/Repositories/ProfileRepository.cs
public class ProfileRepository<TProfile> : IProfileRepository<TProfile> 
    where TProfile : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<TProfile> _profiles;

    public ProfileRepository(AppDbContext context)
    {
        _context = context;
        _profiles = context.Set<TProfile>();
    }

    public async Task<TProfile?> GetByIdAsync(string userId)
    {
        return await _profiles
            .FindAsync(userId);
    }

    public async Task<TProfile> CreateAsync(TProfile profile)
    {
        var entry = await _profiles.AddAsync(profile);
        await _context.SaveChangesAsync();
        return entry.Entity;
    }

    public async Task<TProfile> UpdateAsync(TProfile profile)
    {
        var entry = _profiles.Update(profile);
        await _context.SaveChangesAsync();
        return entry.Entity;
    }

    public async Task DeleteAsync(string userId)
    {
        var profile = await GetByIdAsync(userId);
        if (profile != null)
        {
            _profiles.Remove(profile);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string userId)
    {
        return await _profiles.AnyAsync(p => 
            EF.Property<string>(p, "ApplicationUserId") == userId);
    }
}
```

### 5. Blazor Component Implementation
```razor
@* SmartMenuOptim.Server/Components/Profile/ProfileManager.razor *@
@attribute [Authorize(Policy = "ManageProfiles")]
@inject IProfileService ProfileService
@inject IToastService ToastService

<div class="profile-manager">
    <div class="card">
        <div class="card-header">
            <h3>Profile Management</h3>
        </div>
        <div class="card-body">
            @if (_loading)
            {
                <LoadingSpinner />
            }
            else
            {
                <EditForm Model="@_profile" OnValidSubmit="HandleValidSubmit">
                    <DataAnnotationsValidator />
                    <ValidationSummary />

                    <div class="form-group">
                        <label for="fullName">Full Name</label>
                        <InputText id="fullName" @bind-Value="_profile.FullName" class="form-control" />
                    </div>

                    <div class="form-group">
                        <label for="email">Email</label>
                        <InputText id="email" @bind-Value="_profile.Email" class="form-control" />
                    </div>

                    <div class="form-group">
                        <label for="phone">Phone Number</label>
                        <InputText id="phone" @bind-Value="_profile.PhoneNumber" class="form-control" />
                    </div>

                    <button type="submit" class="btn btn-primary">Save Changes</button>
                </EditForm>
            }
        </div>
    </div>
</div>

@code {
    private ProfileUpdateDto _profile = new();
    private bool _loading = true;

    [Parameter]
    public string UserId { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadProfile();
    }

    private async Task LoadProfile()
    {
        try
        {
            var result = await ProfileService.GetProfile<ProfileDto>(UserId);
            if (result.IsSuccess)
            {
                _profile = new ProfileUpdateDto
                {
                    UserId = UserId,
                    FullName = result.Value.FullName,
                    PhoneNumber = result.Value.PhoneNumber
                };
            }
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task HandleValidSubmit()
    {
        try
        {
            _loading = true;
            var result = await ProfileService.UpdateProfile<ProfileDto>(_profile);
            if (result.IsSuccess)
            {
                ToastService.ShowSuccess("Profile updated successfully");
            }
            else
            {
                ToastService.ShowError(result.Error);
            }
        }
        finally
        {
            _loading = false;
        }
    }
}
```

## 5. Services Implementation

### 5.1 Profile Service
```csharp
// Services/ProfileService.cs
public class ProfileService : IProfileService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public async Task<Result<TProfile>> CreateProfile<TProfile>(ProfileCreationDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create profile logic
            await transaction.CommitAsync();
            return Result<TProfile>.Success(profile);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

### 5.2 Authorization Service
```csharp
// Services/AuthorizationService.cs
public class AuthorizationService : IAuthorizationService
{
    public async Task<bool> HasPermission(string userId, string permission)
    {
        // Check user permissions
        return await Task.FromResult(true);
    }
}
```

## 6. Security Considerations

### 6.1 Data Protection
- Implement profile data encryption using ASP.NET Core Data Protection APIs
- Use secure communication with HTTPS
- Implement proper CORS policies
- Use anti-forgery tokens for forms

### 6.2 Access Control
- Implement role-based access control (RBAC)
- Add tenant isolation
- Implement proper authentication with JWT or cookie authentication
- Add request rate limiting

## 7. Performance Optimization

### 7.1 Database Optimization
```sql
-- Add indexes for common queries
CREATE INDEX IX_Profiles_TenantId ON Profiles(TenantId);
CREATE INDEX IX_Profiles_Type ON Profiles(ProfileType);
```

### 7.2 Blazor Optimization
- Implement lazy loading for components
- Use virtualization for large lists
- Implement proper state management
- Use proper component lifecycle methods

## 8. Monitoring and Logging

### 8.1 Logging Configuration
```csharp
// Program.cs
builder.Services.AddLogging(logging =>
{
    logging.AddSentry(options =>
    {
        options.Dsn = configuration["Sentry:Dsn"];
        options.TracesSampleRate = 1.0;
    });
});
```

### 8.2 Health Checks
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();
```

## 9. Deployment Considerations

### 9.1 CI/CD Pipeline
- Set up GitHub Actions for automated testing
- Configure deployment environments
- Implement database migration automation
- Set up monitoring and alerting

### 9.2 Environment Configuration
- Use proper configuration management
- Implement secrets management
- Set up proper logging levels
- Configure proper CORS policies

## Implementation Checklist

### Phase 1: Foundation
- [ ] Database schema updates
- [ ] Entity Framework configuration
- [ ] Basic CRUD operations
- [ ] Authentication setup

### Phase 2: Core Features
- [ ] Profile management implementation
- [ ] Authorization system
- [ ] Tenant validation
- [ ] Basic UI components

### Phase 3: Enhancement
- [ ] Advanced features
- [ ] Performance optimization
- [ ] Security hardening
- [ ] Monitoring setup

### Phase 4: Testing & Deployment
- [ ] Integration tests
- [ ] UI tests
- [ ] Load testing
- [ ] Production deployment

## Additional Resources

- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Blazor Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [Identity Server Documentation](https://identityserver4.readthedocs.io/)
- [ASP.NET Core Security Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/)

## Support and Maintenance

### Issue Handling
1. Create detailed bug reports
2. Follow proper git branching strategy
3. Implement proper versioning
4. Document all changes

### Monitoring
1. Set up application insights
2. Configure proper logging
3. Set up alerting
4. Monitor performance metrics

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 1.0.0   | TBD  | Initial implementation |
| 1.1.0   | TBD  | Authorization enhancement |
| 1.2.0   | TBD  | Performance optimization |