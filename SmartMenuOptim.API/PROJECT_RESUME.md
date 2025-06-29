# SmartMenuOptim.API Project Resume

## Overview
This project provides the API layer for the SmartMenuOptim solution. Targeting .NET 9, it exposes RESTful endpoints, integrates with AI and data services, and is designed for scalable, containerized deployment.

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