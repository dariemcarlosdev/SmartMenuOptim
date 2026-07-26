# Data Flow Diagrams Guide

Conventions for creating ASCII data flow diagrams (DFDs) for threat modeling.

## DFD Elements

| Element | Symbol | Description |
|---------|--------|-------------|
| **Process** | `[ Box ]` or `┌────┐` | Application, service, or component that processes data |
| **Data Store** | `═══════` or `║ DB ║` | Database, file system, cache, blob storage |
| **External Entity** | `( Entity )` or `┌──┐` | Users, external APIs, third-party services |
| **Data Flow** | `──────►` | Direction of data movement |
| **Trust Boundary** | `├──────┤` | Dashed line separating trust zones |

## NexTruzt Escrow Platform DFD Template

```
┌──────────────────────────────────────────────────────────────────────┐
│                    TRUST BOUNDARY: Internet (Untrusted)               │
│                                                                      │
│  ┌───────────┐                              ┌───────────────────┐   │
│  │  Buyer    │  HTTPS/TLS 1.3 + JWT        │  Seller           │   │
│  │  Browser  │ ────────────────────┐        │  Browser          │   │
│  │  (Blazor) │                     │        │  (Blazor)         │   │
│  └───────────┘                     │        └────────┬──────────┘   │
│                                    │                 │              │
├────────────────────────────────────┼─────────────────┼──────────────┤
│              TRUST BOUNDARY: DMZ / Reverse Proxy                    │
│                                    ▼                 ▼              │
│                            ┌──────────────────────────────┐        │
│                            │  Azure App Gateway / WAF     │        │
│                            │  (Rate Limiting, DDoS)       │        │
│                            └──────────────┬───────────────┘        │
│                                           │                         │
├───────────────────────────────────────────┼─────────────────────────┤
│              TRUST BOUNDARY: Application Tier (Trusted)             │
│                                           ▼                         │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  Blazor Server (ASP.NET Core)                                │  │
│  │  ┌─────────┐  ┌─────────────┐  ┌───────────────────┐       │  │
│  │  │ SignalR  │  │ MediatR     │  │ Background        │       │  │
│  │  │ Hub      │──│ Handlers    │──│ Workers           │       │  │
│  │  │ (UI)     │  │ (CQRS)      │  │ (Timeout/Release) │       │  │
│  │  └─────────┘  └──────┬──────┘  └─────────┬─────────┘       │  │
│  └───────────────────────┼───────────────────┼─────────────────┘  │
│                          │                   │                     │
├──────────────────────────┼───────────────────┼─────────────────────┤
│              TRUST BOUNDARY: Data Tier (Highly Trusted)             │
│                          ▼                   ▼                     │
│  ┌───────────────┐  ┌──────────┐  ┌──────────────────────────┐   │
│  │  SQL Server   │  │  Redis   │  │  Azure Service Bus       │   │
│  │  (EF Core)    │  │  Cache   │  │  (Events/Commands)       │   │
│  │  TDE Enabled  │  │          │  │                          │   │
│  └───────────────┘  └──────────┘  └──────────────────────────┘   │
│                                                                    │
├────────────────────────────────────────────────────────────────────┤
│              TRUST BOUNDARY: External Services                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐            │
│  │  Stripe      │  │  SendGrid    │  │  Entra ID    │            │
│  │  (Payments)  │  │  (Email)     │  │  (Auth/OIDC) │            │
│  └──────────────┘  └──────────────┘  └──────────────┘            │
│                                                                    │
├────────────────────────────────────────────────────────────────────┤
│              TRUST BOUNDARY: Secrets (Most Trusted)                 │
│  ┌──────────────┐  ┌──────────────┐                               │
│  │  Key Vault   │  │  Managed     │                               │
│  │  (Secrets)   │  │  Identity    │                               │
│  └──────────────┘  └──────────────┘                               │
└────────────────────────────────────────────────────────────────────┘
```

## Data Flow Catalog Template

| ID | Source | Destination | Data | Classification | Protocol | Auth | Encryption |
|----|--------|-------------|------|---------------|----------|------|------------|
| DF1 | Buyer Browser | App Gateway | Escrow requests | Confidential | HTTPS | JWT (Entra ID) | TLS 1.3 |
| DF2 | App Gateway | Blazor Server | Proxied requests | Confidential | HTTPS | Forwarded JWT | TLS 1.3 |
| DF3 | MediatR Handler | SQL Server | Escrow CRUD | Restricted | TCP | SQL Auth/MI | TDE + TLS |
| DF4 | MediatR Handler | Redis | Session/cache | Internal | TCP | Password | TLS |
| DF5 | MediatR Handler | Stripe | Payment intents | Restricted | HTTPS | API Key | TLS 1.3 |
| DF6 | Background Worker | Service Bus | Escrow events | Confidential | AMQP | SAS Token | TLS |
| DF7 | Blazor Server | Entra ID | Auth tokens | Restricted | HTTPS | OIDC | TLS 1.3 |
| DF8 | App Service | Key Vault | Secret retrieval | Restricted | HTTPS | Managed Identity | TLS 1.3 |

## Trust Boundary Classification

| Boundary | From → To | Risk Level | Key Threats |
|----------|-----------|-----------|-------------|
| Internet → DMZ | Buyer/Seller → WAF | **Highest** | All input is hostile |
| DMZ → App | WAF → Blazor Server | **High** | Bypassed WAF rules |
| App → Data | Blazor → SQL/Redis | **Medium** | Injection, credential theft |
| App → External | Blazor → Stripe/SendGrid | **Medium** | Data exposure, MITM |
| User → Admin | Regular → Admin functions | **High** | Privilege escalation |
| App → Secrets | Blazor → Key Vault | **Low** | Managed Identity protects |

## DFD Best Practices

1. **Label every arrow** — Include protocol, auth method, and data classification
2. **Mark every trust boundary** — Use clear horizontal lines with labels
3. **Number data flows** — Reference them in STRIDE analysis by ID
4. **Show both directions** — If data flows both ways, use bidirectional arrows
5. **Include background processes** — Workers, timers, and scheduled jobs are attack surfaces too
6. **Keep it readable** — One page/screen; use multiple DFDs for complex systems
7. **Update on change** — New integration or component = update the DFD
