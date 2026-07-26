# Blazor Authentication State

## Blazor Server — RevalidatingServerAuthenticationStateProvider

```csharp
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

public sealed class EscrowAuthenticationStateProvider
    : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IdentityOptions _options;

    public EscrowAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        IOptions<IdentityOptions> optionsAccessor)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _options = optionsAccessor.Value;
    }

    // Revalidate auth state every 30 minutes
    protected override TimeSpan RevalidationInterval
        => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        return await ValidateSecurityStampAsync(
            userManager, authenticationState.User);
    }

    private async Task<bool> ValidateSecurityStampAsync(
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);

        if (user is null || !user.IsActive)
            return false;

        if (!userManager.SupportsUserSecurityStamp)
            return true;

        var principalStamp = principal.FindFirstValue(
            _options.ClaimsIdentity.SecurityStampClaimType);
        var userStamp = await userManager.GetSecurityStampAsync(user);

        return principalStamp == userStamp;
    }
}
```

### Register in Program.cs
```csharp
builder.Services
    .AddScoped<AuthenticationStateProvider, EscrowAuthenticationStateProvider>();
```

## Blazor WASM — Custom AuthenticationStateProvider

```csharp
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtAuthenticationStateProvider(
        ILocalStorageService localStorage,
        HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrWhiteSpace(token) || IsTokenExpired(token))
        {
            return new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity()));
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");

        return new AuthenticationState(
            new ClaimsPrincipal(identity));
    }

    public async Task MarkUserAsAuthenticatedAsync(string token)
    {
        await _localStorage.SetItemAsync("authToken", token);

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(anonymous)));
    }

    private bool IsTokenExpired(string token)
    {
        var jwt = _tokenHandler.ReadJwtToken(token);
        return jwt.ValidTo < DateTime.UtcNow;
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        return token.Claims;
    }
}
```

### Register WASM Provider
```csharp
// Program.cs (Blazor WASM)
builder.Services.AddScoped<AuthenticationStateProvider,
    JwtAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();
```

## App.razor — CascadingAuthenticationState

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="routeData"
                                DefaultLayout="typeof(Layout.MainLayout)">
                <NotAuthorized>
                    @if (context.User.Identity?.IsAuthenticated != true)
                    {
                        <RedirectToLogin />
                    }
                    else
                    {
                        <AccessDenied />
                    }
                </NotAuthorized>
                <Authorizing>
                    <LoadingSpinner />
                </Authorizing>
            </AuthorizeRouteView>
            <FocusOnNavigate RouteData="routeData" Selector="h1" />
        </Found>
        <NotFound>
            <PageTitle>Not Found</PageTitle>
            <p>Sorry, the page you requested was not found.</p>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

## RedirectToLogin Component

### RedirectToLogin.razor
```razor
@inject NavigationManager Navigation

@code {
    // No inline code — see code-behind
}
```

### RedirectToLogin.razor.cs
```csharp
using Microsoft.AspNetCore.Components;

public sealed partial class RedirectToLogin : ComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        var returnUrl = Uri.EscapeDataString(
            Navigation.ToBaseRelativePath(Navigation.Uri));
        Navigation.NavigateTo($"authentication/login?returnUrl={returnUrl}",
            forceLoad: true);
    }
}
```

## Accessing Auth State in Components

### Code-Behind Pattern
```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

public sealed partial class EscrowDashboard : ComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthStateTask { get; set; } = default!;

    private ClaimsPrincipal? _user;
    private string _userName = string.Empty;
    private string _userId = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateTask;
        _user = authState.User;

        if (_user.Identity?.IsAuthenticated == true)
        {
            _userName = _user.FindFirst("name")?.Value
                ?? _user.Identity.Name ?? "Unknown";
            _userId = _user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? string.Empty;
        }
    }
}
```

## Handling Token Expiry in Blazor Server Circuits

```csharp
using Microsoft.AspNetCore.Components.Server.Circuits;

public sealed class TokenExpiryCircuitHandler : CircuitHandler
{
    private readonly ILogger<TokenExpiryCircuitHandler> _logger;

    public TokenExpiryCircuitHandler(
        ILogger<TokenExpiryCircuitHandler> logger)
    {
        _logger = logger;
    }

    public override Task OnCircuitOpenedAsync(
        Circuit circuit, CancellationToken ct)
    {
        _logger.LogInformation(
            "Circuit {CircuitId} opened", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(
        Circuit circuit, CancellationToken ct)
    {
        _logger.LogWarning(
            "Circuit {CircuitId} connection lost", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(
        Circuit circuit, CancellationToken ct)
    {
        _logger.LogInformation(
            "Circuit {CircuitId} reconnected", circuit.Id);
        return Task.CompletedTask;
    }
}
```

### Register Circuit Handler
```csharp
builder.Services.AddScoped<CircuitHandler, TokenExpiryCircuitHandler>();
```
