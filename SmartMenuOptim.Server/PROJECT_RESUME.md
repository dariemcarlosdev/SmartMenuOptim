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