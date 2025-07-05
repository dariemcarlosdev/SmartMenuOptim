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

**Questions?**  
See the comments in `Program.cs` for configuration logic, or open an issue for further clarification!
