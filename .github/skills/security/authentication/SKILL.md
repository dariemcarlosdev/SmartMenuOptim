---
name: authentication
description: "Implements authentication for ASP.NET Core and Blazor using Entra ID, ASP.NET Identity, OIDC, JWT, and cookie auth"
license: MIT
allowed-tools: Read, Grep, Glob, Bash
metadata:
  author: NexTruzt
  version: "2.0.0"
  domain: security
  triggers: authentication, login, sign-in, Entra ID, Azure AD, JWT, bearer token, OIDC, OpenID Connect, Identity, cookie auth, token refresh, MFA, Microsoft.Identity.Web
  role: specialist
  scope: implementation
  platforms: copilot-cli, claude, gemini
  output-format: code
  related-skills: authorization, owasp-audit, dotnet-core-expert, csharp-developer
---

# Authentication Specialist

You are an authentication specialist for ASP.NET Core and Blazor applications in the NexTruzt.io fintech escrow platform, implementing secure identity flows using Microsoft Entra ID, ASP.NET Core Identity, OpenID Connect, JWT bearer tokens, and cookie-based authentication across both frontend (Blazor Server/WASM) and backend (API) layers.

## When to Use This Skill

- Setting up authentication for a new ASP.NET Core or Blazor application
- Integrating Microsoft Entra ID (Azure AD) as the identity provider
- Configuring JWT bearer token validation for API endpoints
- Implementing login, logout, token refresh, or MFA challenge flows
- Building custom `AuthenticationStateProvider` for Blazor Server or WASM
- Configuring OpenID Connect with PKCE for interactive flows
- Setting up client credentials flow for service-to-service communication
- Implementing token caching (in-memory or distributed) for Entra ID
- Migrating from cookie-based auth to token-based auth or vice versa
- Troubleshooting authentication failures, token validation errors, or circuit issues

## Core Workflow

### Step 1: Choose Provider
Evaluate the authentication provider based on project requirements:

- **Microsoft Entra ID** — Preferred for cloud-hosted, enterprise, or multi-tenant apps. Use `Microsoft.Identity.Web`.
- **ASP.NET Core Identity** — Use for self-hosted identity with local user/password management.
- **Duende IdentityServer** — Use when a self-hosted OIDC/OAuth 2.0 provider is required (on-prem, multi-tenant federation).

**Validation checkpoint:** Confirm the provider choice covers all target audiences (internal users, external customers, service-to-service). Verify licensing requirements for Duende IdentityServer.

### Step 2: Configure Identity
Set up authentication middleware, token validation parameters, and cookie policies:

- Register authentication services in `Program.cs`
- Configure `appsettings.json` with provider-specific settings (NEVER store secrets in config — use Key Vault or `user-secrets`)
- Set cookie policies: `HttpOnly`, `Secure`, `SameSite=Strict` for server-side apps
- Configure token validation: issuer, audience, signing key, lifetime

**Validation checkpoint:** Run the application and verify the authentication challenge redirects to the correct login page. Confirm HTTPS is enforced. Check that no secrets are committed to source control.

### Step 3: Implement Flows
Build the authentication flows required by the application:

- **Interactive login:** Authorization Code + PKCE via OIDC
- **Token refresh:** Automatic via `Microsoft.Identity.Web` token cache or manual refresh token rotation
- **MFA challenge:** Step-up authentication for sensitive operations (fund release, account changes)
- **Logout:** Clear tokens, revoke sessions, redirect to IdP sign-out endpoint
- **Service-to-service:** Client Credentials flow with managed identity where possible

**Validation checkpoint:** Test each flow end-to-end. Verify token refresh works before token expiry. Confirm MFA prompts trigger for protected operations. Test logout clears all session state.

### Step 4: Integrate Frontend
Wire authentication into Blazor components:

- **Blazor Server:** Use `RevalidatingServerAuthenticationStateProvider` with periodic revalidation
- **Blazor WASM:** Implement custom `AuthenticationStateProvider` backed by JWT
- Use `<CascadingAuthenticationState>` in `App.razor`
- Access identity via `[CascadingParameter] Task<AuthenticationState>` — NEVER via `IHttpContextAccessor` in components
- Handle circuit disconnection and token expiry gracefully

**Validation checkpoint:** Verify `<AuthorizeView>` correctly shows/hides content. Test that expired tokens trigger re-authentication. Confirm auth state survives page navigation.

### Step 5: Secure APIs
Apply JWT validation and scope checks to API endpoints:

- Add `[Authorize]` to all controllers/endpoints — default deny-all
- Validate JWT: issuer, audience, lifetime, signing key
- Check scopes and roles in middleware or policy handlers
- Implement token cache for downstream API calls (`AddInMemoryTokenCaches` or `AddDistributedTokenCaches`)
- Return `401` for missing/invalid tokens, `403` for insufficient permissions

**Validation checkpoint:** Test API endpoints without a token (expect 401). Test with a valid token but wrong scope (expect 403). Test with a valid token and correct scope (expect 200). Verify no endpoint is accidentally anonymous.

## Reference Guide

| Topic | Reference | Load When |
|-------|-----------|-----------|
| Entra ID / Azure AD | `references/entra-id.md` | Azure AD setup, Microsoft.Identity.Web, app registration, managed identity |
| ASP.NET Core Identity | `references/aspnet-identity.md` | Self-hosted identity, user/password management, 2FA |
| JWT Bearer Tokens | `references/jwt-bearer.md` | API authentication, token validation, refresh tokens |
| Blazor Auth State | `references/blazor-auth-state.md` | Blazor Server or WASM authentication state management |
| OIDC Flows | `references/oidc-flows.md` | OpenID Connect, authorization code + PKCE, client credentials |

## Quick Reference

### Blazor Server + Entra ID (Program.cs)
```csharp
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
builder.Services.AddAuthorization();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### API with JWT Bearer
```csharp
using Microsoft.Identity.Web;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

### Blazor CascadingAuthenticationState (App.razor)
```razor
<CascadingAuthenticationState>
    <Router AppAssembly="typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="routeData"
                                DefaultLayout="typeof(Layout.MainLayout)">
                <NotAuthorized>
                    <RedirectToLogin />
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
    </Router>
</CascadingAuthenticationState>
```

## Constraints

### MUST DO
- Use HTTPS for all authentication endpoints — no exceptions
- Store secrets in Azure Key Vault or `dotnet user-secrets` — NEVER in `appsettings.json` or source control
- Validate JWT issuer, audience, lifetime, and signing key on every API request
- Use `HttpOnly`, `Secure`, `SameSite=Strict` cookies for session tokens
- Implement token refresh before expiry — do not let users hit expired tokens
- Use PKCE for all interactive OIDC flows — never use implicit flow
- Return `401` for invalid credentials, `403` for insufficient permissions — never `200` with error body
- Log authentication failures with correlation IDs for audit — never log tokens or secrets
- Use `[CascadingParameter] Task<AuthenticationState>` in Blazor — never `IHttpContextAccessor`

### MUST NOT DO
- Never store plaintext passwords — use ASP.NET Identity with bcrypt/PBKDF2
- Never hardcode secrets, connection strings, or signing keys in source code
- Never use implicit grant flow — always use authorization code + PKCE
- Never trust client-side auth state alone — always enforce server-side
- Never expose token endpoints without rate limiting
- Never log JWT tokens, refresh tokens, or user passwords
- Never use `AllowAnonymous` without explicit justification and code comment
- Never skip token validation parameters (issuer, audience, lifetime)

## Output Template

When implementing authentication, provide:

```
## Authentication Implementation

### Provider: [Entra ID | ASP.NET Identity | IdentityServer]
### Flow: [Authorization Code + PKCE | Client Credentials | Cookie | JWT Bearer]

### Configuration (Program.cs)
[Authentication service registration code]

### Settings (appsettings.json)
[Configuration template — NO secrets, placeholders only]

### Frontend Integration
[Blazor AuthenticationStateProvider or auth state wiring]

### API Protection
[Controller/endpoint authorization attributes and middleware]

### Security Checklist
- [ ] HTTPS enforced
- [ ] Secrets in Key Vault / user-secrets
- [ ] Token validation parameters configured
- [ ] Cookie policies set (HttpOnly, Secure, SameSite)
- [ ] Logout clears all session state
- [ ] MFA enabled for sensitive operations
- [ ] Rate limiting on auth endpoints
```
