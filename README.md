# Smart Menu Optimizer

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![ASP.NET Core Web API](https://img.shields.io/badge/API-ASP.NET_Core-5C2D91?logo=dotnet)](https://learn.microsoft.com/aspnet/core/web-api/)
[![Azure AI](https://img.shields.io/badge/Azure-AI_Services-0078D4?logo=microsoftazure)](https://azure.microsoft.com/products/ai-services/)
[![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL-4169E1?logo=postgresql)](https://www.postgresql.org/)

**What this app does:** Smart Menu Optimizer is an Azure-native, AI-powered SaaS platform that helps restaurant managers make data-driven menu decisions by analyzing sales trends and customer feedback, then suggesting top-performing dishes and menu adjustments.

---

## Features

### App Features

Domain-facing capabilities — what restaurant teams can do with the platform.

| Feature | Description | Docs |
|---------|-------------|------|
| **AI-driven menu optimization** | Analyze historical sales and customer sentiment to recommend dish promotions, pricing tweaks, and menu adjustments that improve revenue and guest satisfaction. | [README.md](README.md#ai-features) |
| **Interactive dashboards** | Real-time KPI and trend dashboards for managers and staff, including actionable summaries and visual analytics. | [README.md](README.md#demo-highlights) |
| **Sales trend prediction** | Forecast upcoming dish-level performance and identify likely growth or decline patterns before service periods. | [README.md](README.md#ai-features) |
| **Natural language insights** | Ask business questions in plain language (for example: “What should I promote next week?”) and receive AI-generated recommendations. | [README.md](README.md#ai-features) |
| **Tenant-aware SaaS experience** | Multi-restaurant/tenant data isolation with tenant context propagated across API, analytics, and storage layers. | [README.md](README.md#initial-project-structure) |
| **Sentiment-informed decisions** | Use review sentiment analysis to detect dish satisfaction trends and support menu improvement decisions. | [README.md](README.md#ai-features) |

### Technical Features

Cross-cutting, technology-facing concerns applied across the App Features above.

| Feature | Description | Technical docs |
|---------|-------------|----------------|
| **Clean modular architecture** | Separation of concerns across Web Client, API, AI/Analytics, Messaging, Infrastructure, Security, and Monitoring layers. | [README.md](README.md#initial-project-structure) |
| **Event-driven asynchronous workflows** | Background processing for analytics and notifications using messaging/event-based patterns and Azure integrations. | [README.md](README.md#high-level-smart-menu-optimizer-system-design) |
| **Identity and access control** | Enterprise-ready authentication/authorization options (Azure AD/Entra ID, OAuth2/OIDC, JWT, ASP.NET Core Identity, RBAC, policy-based auth). | [README.md](README.md#technology-stack) |
| **Cloud-native observability** | Telemetry, health monitoring, distributed tracing, and alerting using Azure Application Insights/Azure Monitor. | [README.md](README.md#monitoring--logging) |
| **CI/CD-ready delivery** | GitHub Actions pipeline model for build, test, package, deploy, and release governance. | [README.md](README.md#cicd-pipeline) |
| **Extensible AI service model** | Supports Azure Cognitive Services and optional custom ML pipelines for tenant-specific forecasting/recommendation evolution. | [README.md](README.md#ai-features) |

---

## Project Goals

| Goal | Description |
|------|-------------|
| **AI-Driven Menu Optimization** | Use AI/ML to analyze sales and feedback, then surface recommendations that maximize revenue and customer satisfaction. |
| **Scalable Backend** | Deliver reliable multi-tenant APIs for menu, order, and analytics operations with secure RESTful interfaces. |
| **Responsive Real-Time UI** | Provide Blazor-based dashboards with live updates and actionable visual insights for operations teams. |
| **Deep AI Integration** | Integrate Azure OpenAI, Text Analytics, Language services, and optional Azure ML custom models for prediction and insight generation. |
| **Clean Architecture** | Maintain testable, modular components and strong separation of concerns for long-term extensibility. |
| **Azure-Native SaaS Best Practices** | Apply secure auth, scalable hosting, secrets management, and full-stack observability across environments. |
| **Security & Identity** | Enforce robust authentication, authorization, and tenant-aware access controls across all services. |
| **Event-Driven Processing** | Enable asynchronous analytics and notifications via decoupled messaging patterns and background jobs. |
| **Developer Experience** | Support maintainability, clear API documentation, and straightforward integration pathways for future growth. |

---

## Demo Highlights

- **Multi-Restaurant Demo**: Switch between tenants while preserving strict data isolation.
- **Interactive Dashboards**: Real-time analytics with actionable AI recommendations.
- **Natural Language Insights**: Ask business questions and receive plain-language decision guidance.
- **Seamless Operations**: Fast, secure, resilient user workflows as platform load scales.

---

## Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Backend API** | ASP.NET Core Web API | Business logic, tenant-aware endpoints, analytics orchestration |
| **Frontend** | Blazor Server | Interactive real-time UI for management dashboards |
| **AI & Analytics** | Azure Cognitive Services, Azure OpenAI, optional Azure Machine Learning | Forecasting, sentiment analysis, recommendations, natural language interactions |
| **Database** | Azure Database for PostgreSQL | Core relational persistence for tenant business data |
| **Caching** | Azure Cache for Redis | Low-latency access for frequently used metrics/configurations |
| **Object Storage** | Azure Blob Storage | Unstructured data storage, exports, artifacts, backups |
| **Identity & Auth** | Entra ID (Azure AD), OAuth2/OIDC, JWT, ASP.NET Core Identity, IdentityServer (optional) | Authentication, authorization, policy enforcement |
| **Observability** | Azure Application Insights, Azure Monitor, Log Analytics | Monitoring, diagnostics, alerting, telemetry analysis |
| **Messaging/Eventing** | Azure Service Bus, Event Grid, optional Event Hubs | Asynchronous processing and decoupled integration |
| **CI/CD** | GitHub Actions | Automated build-test-package-deploy workflows |

### Authentication and Authorization Options

- **Azure Active Directory (Entra ID):** Enterprise-grade SSO and RBAC.
- **OAuth2/OpenID Connect Providers:** Auth0, Okta, Google Identity, or custom OIDC providers.
- **IdentityServer (Duende):** Self-hosted identity for advanced federation scenarios.
- **ASP.NET Core Identity:** Local-account and role/claims support.
- **JWT Bearer Authentication:** Token-based auth for APIs/SPAs/mobile.
- **RBAC + Policy-based Authorization:** Fine-grained, tenant-aware control.
- **Azure Key Vault:** Secure secret, key, and certificate management.

---

## Architecture

### Layered Architecture Overview

The solution follows a layered, modular architecture:

1. **Web Client (Blazor Server)**
   - Tenant-aware UI, role-based access, real-time visual analytics.
2. **API Layer (ASP.NET Core Web API)**
   - Secure REST endpoints, business rules, and orchestration.
3. **AI/Analytics Services**
   - Forecasting, sentiment analysis, dish scoring, recommendation workflows.
4. **Event Bus/Messaging Layer**
   - Decoupled asynchronous triggers and background processing.
5. **Data Storage Layer**
   - PostgreSQL (transactional), Blob Storage (unstructured), Redis (cache).
6. **Security & Identity**
   - AuthN/AuthZ, tenant propagation, secrets, policy enforcement.
7. **Monitoring & Observability**
   - Logs, traces, metrics, alerts across all components.

### Initial Project Structure

```plaintext
/WebClient
/Api
/AI
/EventBus
/Contracts
/Infrastructure
/Security
/Monitoring
/.github/workflows
/DevOps
/docs
```

### Why this architecture

- Supports multi-tenant SaaS growth without coupling features tightly.
- Keeps AI/analytics extensible and independently evolvable.
- Enables secure, observable, production-ready cloud deployments.
- Balances delivery speed and maintainability through modular boundaries.

---

## AI Features

<img width="509" height="216" alt="image" src="https://github.com/user-attachments/assets/9692464c-9ce9-47f0-aff5-c7aa06fc31ea" />

| Capability | Description |
|------------|-------------|
| **Natural Language Insights** | Ask questions such as “What should I promote next week?” and get AI-generated, context-aware responses.
| **Sales Trend Prediction** | Forecast dish-level sales for upcoming periods using historical trend signals.
| **Sentiment Analysis** | Analyze customer reviews and identify satisfaction patterns or quality concerns.
| **Dish Performance Scoring** | Compute composite performance signals by combining sales, sentiment, and KPI data.
| **Automated Optimization Suggestions** | Continuously surface recommendations such as promotions, price changes, and seasonal adjustments.
| **Interactive AI Chat** | Support manager-facing conversational exploration of metrics and recommendations.
| **Anomaly Detection** | Detect unusual events (for example, sudden negative feedback spikes) and trigger alerts.
| **Customizable Models (Optional)** | Enable per-tenant model tuning and advanced ML pipeline customization.

---

## Low-Level Design (LLD) Summary

Smart Menu Optimizer’s LLD includes both application internals and team delivery mechanics.

### Component Scope

- Web Client (`/WebClient`)
- API Layer (`/Api`)
- AI/Analytics (`/AI`, `/Analytics`)
- Messaging (`/Messaging`, `/EventBus`)
- Data (`/Infrastructure`, `/Persistence`)
- Security (`/Security`, `/Identity`)
- Observability (cross-cutting)
- CI/CD & DevOps (`/.github`, `/DevOps`)

### Collaboration and Delivery

- GitHub flow with `main`, `develop`, `feature/*`, `bugfix/*`, `hotfix/*`.
- Mandatory pull requests with checks and review gates.
- Static analysis, testing, and documentation baked into delivery process.
- IaC-driven deployments and environment-specific secrets/configurations.

### CI/CD Pipeline

Typical stages:
1. **Build** — restore/build/lint
2. **Test** — unit + integration + coverage
3. **Package** — container image build/push
4. **Deploy** — staging auto, production gated approval
5. **Observe/Notify** — logs, telemetry, team notifications

---

## High-Level System Design

<img width="1024" height="1536" alt="image" src="https://github.com/user-attachments/assets/56ee500f-0cb4-4640-b465-221a28c68a60" />

### Core Flow

1. Web client sends request through secure edge entry.
2. API management/policies validate and route calls.
3. Application services execute business logic and publish events.
4. Event bus powers asynchronous AI/analytics workflows.
5. Data layer persists tenant-scoped business and insight data.
6. Monitoring captures operational and product health signals.

### Key Design Principles Met

1. Scalability
2. Reliability & fault tolerance
3. Security
4. Performance efficiency
5. Maintainability
6. Extensibility
7. Observability
8. Data isolation (multi-tenancy)
9. Cost efficiency
10. UX responsiveness

---

## Monitoring & Logging

- **Application Insights:** request traces, exceptions, custom metrics.
- **Azure Monitor + Log Analytics:** centralized dashboards and operational alerts.
- **Health endpoints:** service readiness/liveness checks for runtime confidence.

---

## Deployment & Infrastructure

Managed with IaC (Bicep/Terraform/ARM) and environment-specific configurations.

| Resource Type | Azure Service |
|---------------|---------------|
| App Hosting | Azure App Service (Web/API/AI workloads) |
| Containers | Azure Container Registry |
| Relational DB | Azure Database for PostgreSQL |
| Cache | Azure Redis |
| Eventing | Azure Service Bus / Event Grid |
| Secret Management | Azure Key Vault |
| Monitoring | Application Insights / Azure Monitor |

---

## Documentation

Primary documentation currently lives in this README and is organized by:

- Features (app + technical)
- Architecture (layered + high-level)
- AI capabilities
- LLD/CI-CD/DevOps delivery model
- Deployment, observability, and design rationale

If desired, this can be split into `/docs` files following the same style used in `gallery-manager`.

---

## Design Rationale

- **Business-first AI:** The platform is designed around measurable restaurant outcomes, not generic dashboards.
- **Layered modularity:** Keeps the system adaptable as features and tenants grow.
- **Event-driven integration:** Supports asynchronous, scalable analytics and notification flows.
- **Cloud-native reliability:** Uses managed Azure services for security, uptime, and operational visibility.
- **Developer-friendly structure:** Strong separation of concerns plus CI/CD practices for sustainable delivery.
