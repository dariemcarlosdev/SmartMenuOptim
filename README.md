## Project Foundation: Smart Menu Optimizer for Restaurants

🎯 **Goal:**  
Create a Blazor Web App that uses AI to analyze sales trends and suggest top-performing dishes and menu adjustments. The demo will showcase proficiency in:

- **ASP.NET Core Web API**
- **Blazor Server**
- **Integration with AI** (Azure Cognitive Services or custom ML model)
- **Clean architecture, multitenancy-ready design** (optional)
- **Charting and interactive UI**

---

## AI Features

- **Natural Language Insights:**  
  Ask, "What should I promote next week?" — AI responds using mock ML data.
- **Sales Trend Prediction:**  
  Show predictions for next week's sales per dish.
- **Sentiment Analysis:**  
  Analyze customer reviews (mocked) to understand dish satisfaction.

---

## Initial Project Structure

<img width="636" height="139" alt="image" src="https://github.com/user-attachments/assets/1f607f33-6970-4065-be95-c7291e0ee0e3" />

---

## Tech Stack

<img width="594" height="207" alt="image" src="https://github.com/user-attachments/assets/691e447c-b371-4a86-993d-b8bed574a7e3" />

---

## Features

<img width="509" height="216" alt="image" src="https://github.com/user-attachments/assets/9692464c-9ce9-47f0-aff5-c7aa06fc31ea" />

---

# High-Level Smart Menu Optimizer System Design

<img width="1024" height="1536" alt="image" src="https://github.com/user-attachments/assets/56ee500f-0cb4-4640-b465-221a28c68a60" />

---

## 1. Web Client Layer

**Users:** Restaurant managers, admins, or other tenant users.

**Purpose:**  
Provides an intuitive, tenant-specific interface for managing menus, viewing analytics, and receiving AI-driven recommendations.

**Responsibilities:**

- Display menus, performance metrics, and optimization suggestions.
- Provide tenant-specific configurations (menu preferences, promotions).
- Securely submit requests to backend services, carrying tenant context.
- Visualize sales trend analysis, forecasting charts, and predictive insights.

**Key Points:**

- Multi-tenant aware.
- Receives aggregated predictions and insights from backend services asynchronously.

---

## 2. Front Door (Azure Front Door)

**Purpose:**  
Acts as the global entry point for requests, providing load balancing, SSL offload, WAF, and routing.

**Responsibilities:**

- Global routing to the nearest regional service instance for low latency.
- SSL/TLS termination for encryption offloading.
- Web Application Firewall (WAF) for security (e.g., SQL injection, XSS).
- Health probes for automatic failover.

**Example Use Case:**  
A manager in New York accesses the platform; Front Door routes them to the closest regional backend instance while enforcing security policies and encrypting traffic.

---

## 3. API Management Layer

**Purpose:**  
Centralizes API management for authentication, rate limiting, and policy enforcement.

**Responsibilities:**

- Authentication & authorization with OAuth2/JWT for multi-tenant access.
- Rate limiting and throttling to prevent abuse.
- Policy enforcement (input validation, request transformations).
- API versioning and documentation for developers or partners.

**Example Use Case:**  
A third-party integration requests sales analytics. API Management enforces rate limits and validates the API key for the correct tenant.

---

## 4. Application / Service Layer (Enhanced with AI)

**Purpose:**  
Core business logic, analytics, and AI-driven intelligence.

**Responsibilities:**

- **Menu & Order Management:** CRUD for menu items and orders.
- **Analytics & Insights:** Dish performance calculation, KPIs, and optimization suggestions.
- **Sales Trend Analysis & Forecasting:** Historical sales aggregation, pattern detection, and ML-driven predictions.
- **Predictive & Prescriptive Analytics:** Identify trends, underperformers, and suggest menu changes/promotions.
- **Text Analytics & Sentiment Analysis:** Analyze customer reviews for actionable insights.
- **AI Chat & Recommendation Engine:** ChatGPT-style integration for natural language queries and contextual recommendations.
- **Tenant Context Propagation:** Ensures all AI insights and predictions respect tenant isolation.
- **Business Rules Enforcement:** Promote high-margin items, flag low-stock, highlight negative reviews.

**Design Patterns & Architecture:**

- **Service Layer Pattern:** Modular, testable, reusable logic.
- **Event-Driven AI Processing:** AI tasks (ML predictions, sentiment analysis) are triggered asynchronously via Event Bus.
- **CQRS for AI Data:** Separate commands (write/update) and queries (read/fetch).

**Example Flow:**

- Tenant requests “Sales Forecast for Next Month.”  
  The Service Layer fetches historical data → ML model estimates future sales → results stored in the database → visualized on the dashboard.

- Customer reviews arrive → Text Analytics & Sentiment Analysis → triggers Event Bus → Recommendation Service suggests menu changes → updates dashboard.

- Manager asks via ChatGPT: “Which dishes are likely to underperform next week?” → AI model responds with actionable suggestions.

**Integration with Event Bus / Messaging Layer:**

- **Scenario 1:** New review arrives → Event Bus triggers Sentiment Analysis → AI model scores dish → updates performance metrics.
- **Scenario 2:** High-volume sales data from POS → Event Hub triggers ML model → updates sales forecast asynchronously.
- **Scenario 3:** Manager asks ChatGPT for recommendation → AI Service consults predictive model → Event Bus updates dashboard asynchronously.

**Key Benefits of AI Integration:**

- Real-time insights for decision-making.
- Predictive power for menus, inventory, and promotions.
- Natural language support for enhanced UX.
- Decoupled, asynchronous AI processing for scalability.

---

## 5. Event Bus / Messaging Layer (Detailed)

The Event Bus / Messaging Layer is the backbone of decoupled, scalable, and resilient communication in the Smart Menu Optimizer system. It enables asynchronous processing, system extensibility, and real-time insights by allowing services to communicate through events rather than direct calls. Here’s a detailed breakdown:

---

### **Core Purposes**

1. **Decoupling**  
   Services publish and subscribe to events, so they don’t need to know about each other’s existence or implementation. For example, the Review Service can publish a `ReviewReceived` event, and the Analytics Service can subscribe to it.

2. **Scalability & Resilience**  
   Event-driven systems naturally scale: services can process messages at their own pace, and new consumer services can be added without changing the event producers.

3. **Reliability**  
   Supports features like retry policies, dead-letter queues, and persistence, ensuring no data loss even if services are temporarily unavailable.

4. **CQRS Support**  
   Cleanly separates commands (write operations) and queries (read operations):  
   - **Commands:** “UpdateSalesForecast”, “ProcessReview”, etc.  
   - **Events:** “MenuUpdated”, “SalesThresholdBreached”, etc.

---

### **Azure Messaging Services Overview**

- **Azure Service Bus**  
  - **Queues:** Point-to-point communication for commands (e.g., “ProcessNewOrder”).  
  - **Topics & Subscriptions:** Publish/subscribe for events (e.g., “ReviewReceived”).

- **Azure Event Grid**  
  - Lightweight, high-throughput event routing for domain/business events.  
  - Example: “MenuUpdated”, “SalesThresholdBreached”.

- **Azure Event Hubs** (Optional/Advanced)  
  - Handles high-volume telemetry/streaming data (e.g., bulk POS sales ingestion).

---

### **Key Responsibilities**

1. **Event Publishing**
   - Services (like Web/API, Review, or POS Ingestion) publish events after significant business actions (e.g., order creation, review submission).

2. **Event Consumption**
   - Analytics, Forecasting, Notification, and Recommendation services subscribe and react to these events asynchronously.

3. **Command Handling**
   - Commands are sent via queues to trigger specific actions (e.g., “GenerateForecast”), decoupling the request from immediate execution.

4. **Reliability & Error Handling**
   - Built-in retry, message dead-lettering, and delayed delivery ensure robust handling of transient failures.

5. **Multi-Tenancy Awareness**
   - Events and messages carry tenant context, so processing and data updates are isolated to the correct tenant.

---

### **Typical Event Flow Examples**

#### **A. Customer Review Submission**
1. Customer submits a review.
2. Web/API Layer publishes a `ReviewReceived` event to Event Grid.
3. Analytics Service consumes this event, analyzes sentiment, and updates dish metrics.
4. Recommendation Service may publish a `MenuInsightGenerated` event.
5. Notification Service sends insights or alerts to the manager asynchronously.

#### **B. Sales Data Ingestion and Forecast**
1. Bulk sales data is ingested (via POS or upload).
2. Event Hub receives high-throughput sales telemetry.
3. Forecasting Service listens to events, processes data, and predicts future trends.
4. Results are written to storage and a `ForecastGenerated` event is published.
5. Web client is updated with new forecasts.

#### **C. Menu Update**
1. Manager updates menu items.
2. API Layer publishes a `MenuUpdated` event to Event Grid.
3. Analytics, Notification, and Inventory Services consume the event and react accordingly (e.g., recalculate KPIs, alert kitchen, etc.).

---

### **Design Patterns & Best Practices**

- **Publisher-Subscriber Pattern:**  
  Enables many-to-many communication between event producers and consumers.

- **Event Sourcing:**  
  (Optional, advanced) Persist all events for auditability and replayability.

- **Outbox Pattern:**  
  Ensures reliable event publishing in the presence of transaction boundaries (avoiding lost updates).

- **Idempotency:**  
  Event handlers are designed to process repeated events without side effects.

- **Event Contracts:**  
  Events have well-defined schemas (JSON or Avro) to ensure compatibility between services.

---

### **Multi-Tenant Considerations**

- **Tenant Context Propagation:**  
  Every event/message includes tenant ID or context metadata, ensuring strict data and process isolation.

- **Per-Tenant Subscriptions:**  
  Optionally, services can subscribe to only those events relevant to specific tenants for efficiency and compliance.

---

### **Example Event Definitions (Pseudocode/JSON)**

```json
{
  "eventType": "ReviewReceived",
  "tenantId": "tenant-123",
  "reviewId": "rev-789",
  "dishId": "dish-456",
  "rating": 4,
  "comment": "Great taste, but a bit salty.",
  "timestamp": "2025-08-23T03:00:00Z"
}
```
```json
{
  "eventType": "SalesThresholdBreached",
  "tenantId": "tenant-123",
  "dishId": "dish-456",
  "salesCount": 300,
  "threshold": 250,
  "direction": "above",
  "period": "2025-W34"
}
```

---

### **Summary Table**

| Feature                     | Benefit                                  | Example                                  |
|-----------------------------|------------------------------------------|------------------------------------------|
| Decoupling                  | Flexible scaling, independent services   | Analytics service added with no API changes |
| Asynchronous Processing     | Responsive UI, background tasks          | Sentiment analysis triggered after review submission |
| Reliability                 | No data loss, robust error handling      | Sales events retried on failure          |
| Multi-Tenancy               | Data isolation, secure event handling    | Tenant context in all events             |
| CQRS                        | Optimized read/write, flexibility        | Separate “GenerateForecast” (cmd) and “ForecastGenerated” (event) |

---

### **Summary**

The Event Bus / Messaging Layer is essential for:
- Decoupling and scaling services
- Handling spikes in load and asynchronous tasks (like AI/ML predictions)
- Ensuring reliability, observability, and tenant data isolation
- Enabling rich, event-driven features that power real-time insights and recommendations

This architecture provides the flexibility and robustness required for a modern, AI-driven, multi-tenant SaaS platform like Smart Menu Optimizer.


## 6. Database Layer (Multi-Tenant)

**Purpose:**  
Secure, efficient storage of tenant data.

**Responsibilities:**

- Tenant-isolated storage (shared DB, separate schema per tenant).
- Store menus, orders, reviews, analytics.
- Optimize with indexing, caching, partitioning.
- Optionally: separate DB per large tenant.

**Data Flow Example:**  
Service Layer writes prediction results → stored in tenant schema → accessible by web client for dashboards.

---

## 7. Monitoring & Logging

**Purpose:**  
Reliability and observability.

**Responsibilities:**

- Track performance metrics, errors, anomalies.
- Audit user/tenant actions.
- Alert on threshold breaches.

**Tools:** Azure Monitor, Application Insights, Log Analytics.

**Example Use Case:**  
Anomalous spike in failed orders detected → alert triggered → DevOps investigates.

---

# High-Level Flow

1. Web client request → Front Door (routing, WAF, SSL offload)
2. Routed to API Management (authentication, rate limiting)
3. Request reaches Service Layer (business logic or event publishing)
4. Event Bus handles async tasks (analytics, notifications, forecasting)
5. Database persists tenant data
6. Monitoring & Logging capture metrics and anomalies

---

# Key Benefits

- **Global Scalability:**  
  Front Door + Event Bus = geo-distributed, horizontally scalable architecture.
- **Security:**  
  API Management, WAF, tenant-aware services provide multi-layer protection.
- **Analytics-Driven:**  
  Integrated sales trend analysis, prediction, and forecasting.
- **Maintainable & Extensible:**  
  Service Layer + Event Bus decoupling simplifies feature addition.
- **SaaS Ready:**  
  Multi-tenant aware, with per-tenant dashboards and predictive insights.
