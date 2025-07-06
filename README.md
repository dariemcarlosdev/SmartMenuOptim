# SmartMenuOptim CI/CD Pipeline - Workflow Description

This GitHub Actions workflow automates the build, test, static analysis, deployment, and notification steps for the SmartMenuOptim application.

## Trigger Events

- Runs on pushes and pull requests to:
  - `Staging`
  - `master`
  - Any branch matching `env-dev/*` or `env-staging/*`

---

## Jobs Overview

### 1. Build & Test (`build-test`)
- Sets up Ubuntu with PostgreSQL 17 as a service.
- Restores .NET 9 dependencies, installs EF Core CLI, and applies local database migrations.
- Builds the solution, runs tests, and uploads code coverage.
- Publishes only the API and Server projects (not Shared or Tests).
- Uploads build artifacts for deployment.

### 2. Static Analysis (`codeql-analysis`)
- Runs GitHub CodeQL for security and code quality analysis on C# code.

### 3. Deploy API & Server to Azure (`deploy-azure-api`, `deploy-azure-server`)
- Downloads published artifacts.
- Deploys API and Server apps separately to Azure Web Apps using publish profiles.
- Applies EF Core migrations directly to the production Azure PostgreSQL database after deployment (on select branches).

### 4. Notifications (`notify-on-success`, `notify-on-failure`)
- Sends email notifications to the team on CI/CD success or failure.

---

## Additional (Commented Out) Options

- Templates for AWS Elastic Beanstalk and Docker Hub deployment are present but currently disabled.

---

## Why Docker Is Not Used in This Workflow

Docker files and containers are not currently utilized in any jobs of this workflow. This is because the CI/CD pipeline is designed to build, test, and deploy .NET projects directly using the .NET SDK and native tooling provided by GitHub-hosted runners. The deployment process targets Azure Web Apps, which natively support .NET application deployments without requiring Docker containers. Should the deployment strategy change in the future (e.g., moving to containerized environments or orchestrators), Docker jobs and steps can be easily integrated, as commented examples are already provided in the workflow.

---

## Key Workflow Practices

- Publishes only entry-point projects (API and Server), not libraries or tests.
- Applies database migrations both locally (before tests) and in production (after deployment).
- Performs security and code quality checks before deployment.
- Sends notifications to keep the team informed of status.

---


