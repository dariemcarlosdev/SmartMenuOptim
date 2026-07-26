# Blazor Authorization

## AuthorizeView with Policy

Gate UI elements based on authorization policies. **Always enforce server-side — UI gating is convenience, not security.**

### Basic Policy-Based Gating
```razor
@using EscrowApp.Application.Authorization

<AuthorizeView Policy="@AuthorizationPolicies.CanReleaseFunds">
    <Authorized>
        <button class="btn btn-danger" @onclick="ReleaseFunds">
            Release Escrow Funds
        </button>
    </Authorized>
    <NotAuthorized>
        <p class="text-muted">
            You do not have permission to release funds.
        </p>
    </NotAuthorized>
</AuthorizeView>

<AuthorizeView Policy="@AuthorizationPolicies.CanViewAuditLogs">
    <Authorized>
        <a href="/admin/audit-logs" class="nav-link">Audit Logs</a>
    </Authorized>
</AuthorizeView>
```

### Role-Based Gating (Prefer Policies)
```razor
<AuthorizeView Roles="Administrator,Agent">
    <Authorized>
        <AdminPanel />
    </Authorized>
</AuthorizeView>
```

### Accessing User in AuthorizeView
```razor
<AuthorizeView>
    <Authorized>
        <span>Welcome, @context.User.Identity?.Name</span>
        <span>Role: @context.User.FindFirst("EscrowRole")?.Value</span>
    </Authorized>
    <NotAuthorized>
        <a href="/authentication/login">Sign In</a>
    </NotAuthorized>
    <Authorizing>
        <LoadingSpinner />
    </Authorizing>
</AuthorizeView>
```

## AuthorizeRouteView in App.razor

```razor
<!-- App.razor -->
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
                    <div class="d-flex justify-content-center p-5">
                        <div class="spinner-border" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                    </div>
                </Authorizing>
            </AuthorizeRouteView>
            <FocusOnNavigate RouteData="routeData" Selector="h1" />
        </Found>
        <NotFound>
            <PageTitle>Not Found</PageTitle>
            <LayoutView Layout="typeof(Layout.MainLayout)">
                <p>Page not found.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

## [Authorize] Attribute on Routable Components

### Page-Level Authorization
```razor
@page "/escrow/transactions"
@attribute [Authorize(Policy = AuthorizationPolicies.CanViewTransactions)]

<PageTitle>Escrow Transactions</PageTitle>
<h1>Transactions</h1>
```

### Code-Behind with Auth State
```csharp
// TransactionList.razor.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

[Authorize(Policy = AuthorizationPolicies.CanViewTransactions)]
public sealed partial class TransactionList : ComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthStateTask { get; set; } = default!;

    [Inject]
    private ISender Sender { get; set; } = default!;

    private List<TransactionDto> _transactions = [];
    private string _userId = string.Empty;
    private bool _canReleaseFunds;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateTask;
        var user = authState.User;

        _userId = user.GetUserId();
        _canReleaseFunds = user.IsInEscrowRole("Agent", "Admin");

        var result = await Sender.Send(
            new GetTransactionsQuery(user));

        if (result.IsSuccess)
            _transactions = result.Value;
    }
}
```

## RedirectToLogin Component

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
        Navigation.NavigateTo(
            $"authentication/login?returnUrl={returnUrl}",
            forceLoad: true);
    }
}
```

## AccessDenied Component

### AccessDenied.razor
```razor
<div class="container text-center mt-5">
    <div class="alert alert-warning" role="alert">
        <h4 class="alert-heading">Access Denied</h4>
        <p>You do not have permission to view this page.</p>
        <hr />
        <p class="mb-0">
            Contact your administrator if you believe this is an error.
        </p>
    </div>
    <a href="/" class="btn btn-primary">Return to Dashboard</a>
</div>
```

## Conditional Navigation Based on Roles

### NavMenu.razor.cs
```csharp
public sealed partial class NavMenu : ComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthStateTask { get; set; } = default!;

    private bool _isAdmin;
    private bool _isAgent;
    private bool _isAuditor;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateTask;
        var user = authState.User;

        _isAdmin = user.IsInEscrowRole("Admin");
        _isAgent = user.IsInEscrowRole("Agent", "Admin");
        _isAuditor = user.IsInEscrowRole("Auditor", "Admin");
    }
}
```

### NavMenu.razor
```razor
<nav class="nav flex-column">
    <NavLink class="nav-link" href="/" Match="NavLinkMatch.All">
        Dashboard
    </NavLink>

    <AuthorizeView Policy="@AuthorizationPolicies.CanViewTransactions">
        <Authorized>
            <NavLink class="nav-link" href="/escrow/transactions">
                Transactions
            </NavLink>
        </Authorized>
    </AuthorizeView>

    @if (_isAgent)
    {
        <NavLink class="nav-link" href="/escrow/pending-releases">
            Pending Releases
        </NavLink>
    }

    @if (_isAuditor)
    {
        <NavLink class="nav-link" href="/admin/audit-logs">
            Audit Logs
        </NavLink>
    }

    @if (_isAdmin)
    {
        <NavLink class="nav-link" href="/admin/users">
            User Management
        </NavLink>
    }
</nav>
```

## Critical Reminders

1. **UI gating is NOT security** — `<AuthorizeView>` hides elements but does not prevent access. Always enforce `[Authorize]` on the server-side endpoint or MediatR handler.
2. **Never rely on `@if (_isAdmin)`** alone to protect sensitive operations — always pair with server-side authorization.
3. **Use `[CascadingParameter] Task<AuthenticationState>`** to access the current user — never `IHttpContextAccessor` in Blazor components.
4. **Test authorization by bypassing the UI** — call the API directly without the expected role and verify you get `403`.
