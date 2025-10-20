# SmartMenuOptim.Server Project Resume

## Overview
This project serves as the server-side component of the SmartMenuOptim solution. It targets .NET 9 and is configured as a web application, providing backend services and APIs for the solution. The project is designed for deployment in containerized environments and supports secure configuration management.

## Technologies & Frameworks
- .NET 9
- ASP.NET Core (Web API)
- Docker (Linux containers)
- User Secrets (for local development secrets management)
- Microsoft.VisualStudio.Azure.Containers.Tools.Targets (container tooling)

## Key Features
- Hosts backend services and APIs for the solution
- Supports containerized deployment (Docker)
- Integrates shared logic and models from SmartMenuOptim.Shared
- Secure management of sensitive configuration via User Secrets

## Project References
- SmartMenuOptim.Shared

## Getting Started
1. Open the solution in Visual Studio 2022.
2. Restore NuGet packages.
3. Build the solution.
4. Run or debug the project using Visual Studio or `dotnet run`.
5. For containerized deployment, use the provided Docker support.

## Structure
- Web application entry point
- References shared code for consistency across the solution

## Author(s)
- [Dariem C Macias Mora]

## License
- [Specify license if applicable]

---

_This resume provides a quick reference for contributors and maintainers._


## Development Notes
- Ensure that the .NET SDK version is compatible with the project. The project targets .NET 9, so make sure you have the appropriate SDK installed.
- When adding new features or endpoints, follow the existing coding standards and patterns used in the project.
- For local development, use User Secrets to manage sensitive information such as API keys or connection strings. This avoids hardcoding sensitive data in the source code.
- As this is a Blazor Server project, ensure that the necessary configurations for SignalR and server-side rendering are properly set up in `Program.cs`.
- As this project is designed for containerized deployments, ensure that the Dockerfile is properly configured. Test the container locally before deploying to production environments.
- When integrating with Azure services, ensure that the necessary configurations (e.g., connection strings, API keys) are set up in the User Secrets or environment variables for local development.
- For any changes that affect the shared models or logic in SmartMenuOptim.Shared, ensure that those changes are reflected in both the server and any other consuming projects to maintain consistency.
- When working with Blazor components, ensure that the component lifecycle methods are properly utilized to manage state and data binding effectively.
- For any changes that affect the API endpoints or backend services, ensure that proper testing is conducted to validate functionality and performance.
- Serilog or Application Insights can be integrated for logging and monitoring to track usage patterns, errors, and performance metrics.




## Environment-Based Configuration & Secrets Management

### Overview

SmartMenuOptim.Server uses a layered configuration strategy to securely manage settings across environments (development, production) and support best practices for secret management.

### Configuration Sources (In Precedence Order)

1. **Azure App Service Application Settings**  
   Key-value pairs set in the Azure Portal override any values in config files or Key Vault.
2. **Azure Key Vault**  
   All secrets and sensitive configuration (such as API keys and endpoints) are stored here.
3. **appsettings.Production.json**  
   Used for non-secret, production-specific settings that are safe to include in source control.
4. **appsettings.Development.json**  
   Used for local developer overrides; not included in production deployments.
5. **appsettings.json**  
   Default application configuration.

### How It Works

- **Local Development:**  
  - Place non-secret configs in `appsettings.Development.json`.
  - Secrets can be stored in [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) (for dev) or in environment variables.
- **Production (Azure):**
  - Most secrets (e.g., `BackendApi:BaseUrl`) are stored in Azure Key Vault.
  - The app is configured to read the Key Vault name from the `KeyVaultName` app setting in Azure App Service.
  - The system-assigned Managed Identity of the App Service is granted access to Key Vault.
  - Non-secret settings can be set in `appsettings.Production.json` or via App Service Application Settings.

### How to Add a New Secret or Override a Setting in Production

1. **Add to Azure Key Vault:**
   - Go to your Key Vault in Azure.
   - Add a secret using double-dash notation for nested config (e.g., `BackendApi--BaseUrl` for `BackendApi:BaseUrl`).
2. **Update App Service Application Settings:**
   - In Azure Portal, go to your App Service > Configuration.
   - Add or override a setting by key name (these take precedence over Key Vault).
3. **(Optional) Add to appsettings.Production.json:**
   - Only for non-secrets or settings safe to commit.

### Best Practices

- **Never commit secrets** to source control or to any `appsettings.*.json` file.
- Prefer Azure Key Vault for all sensitive values.
- Use App Service Application Settings for quick overrides and for the `KeyVaultName` parameter.
- Use [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) locally for development secrets.

### Example: Key Vault Secret Naming Convention

| Key Vault Secret Name    | .NET Configuration Key            |
|-------------------------|-----------------------------------|
| `BackendApi--BaseUrl`   | `BackendApi:BaseUrl`              |
| `ConnectionStrings--Sql`| `ConnectionStrings:Sql`           |

### Useful References

- [Azure Key Vault .NET Integration Docs](https://learn.microsoft.com/en-us/azure/key-vault/secrets/quick-create-net)
- [ASP.NET Core Configuration Providers](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Best Practices for Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)

---

## Configuration Precedence and `BackendApi:BaseUrl` Resolution

When running your ASP.NET Core project locally with both `appsettings.Development.json` and `secrets.json` (or User Secrets), the configuration system loads values in the following order (from lowest to highest priority):

1. **appsettings.json**  
2. **appsettings.Development.json** (overrides values from `appsettings.json`)
3. **secrets.json** (or User Secrets; overrides both above for development)
4. **Environment Variables** (if set; highest priority)

### What This Means In Practice

- If `BackendApi:BaseUrl` is set in **both** `appsettings.Development.json` and `secrets.json`, **the value from `secrets.json` will be used** during local development.
- If you set `BackendApi:BaseUrl` as an **environment variable**, it will take precedence over both `appsettings.Development.json` and `secrets.json`.

### Code Example

```csharp
builder.Services.AddHttpClient("BackendAPI", (serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = config["BackendApi:BaseUrl"]; // This resolves according to the above order
    client.BaseAddress = new Uri(baseUrl);
});
```

### Recommendation

- Use `appsettings.Development.json` for settings you want all developers to share.
- Use `secrets.json` (or User Secrets) for sensitive or developer-specific overrides without checking them into source control.
- Use environment variables for deployment-specific overrides (Docker, Azure, etc).

### Example: Where should I put `BackendApi:BaseUrl`?

- **Shared for all developers:** `appsettings.Development.json`
- **Sensitive or developer-specific:** User Secrets or `secrets.json`
- **For production/cloud:** Environment variables or Azure App Settings

---

**References:**
- [Microsoft Docs: Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Microsoft Docs: Safe storage of app secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)

---

**Questions
See the comments in `Program.cs` for configuration logic, or open an issue for further clarification!ns?**  