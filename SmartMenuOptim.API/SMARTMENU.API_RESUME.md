# SmartMenuOptim.API Project Resume

## Overview
This project provides the API layer for the SmartMenuOptim solution. Targeting .NET 9, it exposes RESTful endpoints, integrates with AI and data services, and is designed for scalable, containerized deployment.

## Clean Architecture Guidance
- **API/Application Layer:**
  - **API Controllers:** Define HTTP endpoints and handle HTTP requests/responses. Example: `OrdersController` exposes endpoints for order management.
  - **Application Services:** Coordinate application logic, orchestrate domain and infrastructure operations. Example: `OrderService` processes an order by validating, saving, and sending notifications.
  - **Request/Response Models:** DTOs for input/output of API endpoints. Example: `CreateOrderRequest`, `OrderResponse`.
  - **No business/domain logic:** Should delegate business rules to the domain layer.
  - **Implements interfaces from shared/domain:** Concrete implementations for abstractions defined elsewhere.
  - **Infrastructure Integrations:** Set up dependency injection, logging, and external service clients.

Each component should focus on its responsibility, keeping the API layer thin and delegating business logic to the domain/application layers.

## Technologies & Frameworks
- .NET 9
- ASP.NET Core (Web API)
- Azure.AI.TextAnalytics (AI and NLP integration)
- Entity Framework Core (with PostgreSQL)
- Swashbuckle (OpenAPI/Swagger documentation)
- Docker (Linux containers)
- User Secrets (for local development secrets management)

## Key Features
- Exposes RESTful APIs for the solution
- Integrates with Azure AI Text Analytics for advanced text processing
- Uses Entity Framework Core with PostgreSQL for data access
- Provides interactive API documentation via Swagger/OpenAPI
- Supports containerized deployment (Docker)
- Secure management of sensitive configuration via User Secrets
- Shares models and logic with other solution components via SmartMenuOptim.Shared

## Project References
- SmartMenuOptim.Shared

## Getting Started
1. Open the solution in Visual Studio 2022.
2. Restore NuGet packages.
3. Build the solution.
4. Run or debug the project using Visual Studio or `dotnet run`.
5. Access the Swagger UI for API exploration and testing.
6. For containerized deployment, use the provided Docker support.

## Structure
- Web API entry point
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
- When working with Entity Framework Core, ensure that migrations are properly managed. Use the `dotnet ef` CLI tools to add, update, or remove migrations as needed.
- For API documentation, ensure that all public endpoints are properly annotated with XML comments. This will help generate accurate Swagger documentation.
- When making changes to the API, consider versioning strategies to maintain backward compatibility for existing clients.
- For containerized deployments, ensure that the Dockerfile is properly configured. Test the container locally before deploying to production environments.
- When integrating with Azure services, ensure that the necessary configurations (e.g., connection strings, API keys) are set up in the User Secrets or environment variables for local development.
- For any changes that affect the shared models or logic in SmartMenuOptim.Shared, ensure that those changes are reflected in both the API and any other consuming projects to maintain consistency.
- Regularly review and update dependencies to keep the project secure and up-to-date with the latest features and fixes.
- Consider implementing logging and monitoring for the API to track usage patterns, errors, and performance metrics. This can be done using built-in ASP.NET Core logging or third-party solutions like Serilog or Application Insights.
- When deploying to production, ensure that the API is properly secured. This includes implementing authentication and authorization mechanisms, such as JWT tokens or OAuth2, depending on the requirements of your application.
- For performance optimization, consider using caching strategies for frequently accessed data or computationally expensive operations. ASP.NET Core provides built-in support for in-memory caching, distributed caching, and response caching.
- When handling large datasets or complex queries, consider implementing pagination or filtering to improve API responsiveness and reduce load on the database.
- Ensure that error handling is robust. Use middleware to catch exceptions and return meaningful error responses to clients. This will improve the API's usability and help clients understand issues when they arise.


## Configuration & Secrets Management

This project uses flexible configuration loading to support local development, Docker, and cloud deployments (such as Azure App Service).

### Configuration Sources

The application loads configuration in the following order, depending on the environment:

1. **Local Development:**  
   - Loads from [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets), if available.
   - Loads from environment variables.

2. **Azure App Service:**  
   - Loads configuration exclusively from environment variables (App Settings).

3. **Docker or Other Environments:**  
   - Loads from `/app/secrets.json` (if present).
   - Loads from environment variables.

> **Note:**  
> Sensitive data such as database connection strings and API keys should **never** be committed to source control.  
> Use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for local development, and environment variables or a secure secrets store for production.

### Example `secrets.json` Format
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=SmartMenuDb;User Id=postgres;Password=admin123;TrustServerCertificate=True;"
  },
  "Azure": {
    "TextAnalytics": {
      "Endpoint": "https://your-azure-endpoint/",
      "Key": "your-azure-key"
    }
  }
}
### Database Context Configuration

The application configures the database context using the `DefaultConnection` connection string:
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
### Best Practices

- **Local development:** Use User Secrets or a local `secrets.json` (not committed).
- **Docker:** Mount a secrets file or set environment variables at runtime.
- **Azure:** Set secrets as App Settings or use Azure Key Vault.
- **Never** commit real credentials or secrets to your repository.

### Troubleshooting

If the application cannot find the necessary configuration or secrets, ensure that:
- The correct environment variables are set for your environment.
- For Docker, `/app/secrets.json` exists and is readable.
- For local development, User Secrets are initialized and populated.

---

For more information, see [Microsoft Docs: Safe storage of app secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets).

