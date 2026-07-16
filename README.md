# Multitenant Booking SaaS Application

A high-performance, highly reliable multi-tenant scheduling and booking API. Built using .NET, this project enforces strict data integrity and network resilience to handle concurrent scheduling in a distributed environment.

## 💻 Tech Stack

* **Framework:** .NET 9 (ASP.NET Core Web API)
* **Language:** C#
* **Data Access:** Entity Framework Core
* **Caching & Idempotency:** Redis (`StackExchange.Redis`)
* **Background Processing:** Hangfire
* **API Documentation:** Swagger / OpenAPI

## 🔐 Default System Credentials

When the application is first seeded, several default accounts are created for testing the multi-tenant architecture and role-based access control. 

All seeded users share the same default password:
* **Password:** `P@ssw0rd`

### System Owner (Global Access)
The Owner role acts as the site's overall admin, allowing you to perform global CRUD operations on tenants. Because the Owner does not belong to a specific tenant, you **do not** need to provide the `X-Tenant-Id` header when hitting the `/login` endpoint or managing global resources.

| Role | Email | Tenant Name | `X-Tenant-Id` Header |
| :--- | :--- | :--- | :--- |
| **Owner** | `owner@gmail.com` | *None (Global)* | *Not Required* |

### Tenant Accounts (Isolated Access)
These accounts belong to specific active tenants. When authenticating or accessing tenant-scoped resources with these users, you **must** include the corresponding `x-tenant-id` header in your HTTP requests.

| Role | Email | Tenant Name | `X-Tenant-Id` Header |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin1@gmail.com` | Organization A | `1` |
| User | `user1@gmail.com` | Organization A | `1` |
| User | `user11@gmail.com` | Organization A | `1` |
| **Admin** | `admin2@gmail.com` | Organization B | `2` |
| User | `user2@gmail.com` | Organization B | `2` |
| **Admin** | `admin3@gmail.com` | Organization C | `3` |
| User | `user3@gmail.com` | Organization C | `3` |

> ⚠️ **Important:** These credentials are for local development and testing only. You must change the default passwords or remove the seed data before deploying to a production environment.

## 🏗 Architecture & Patterns

This solution adheres to **Onion Architecture** principles, divided into five main layers (`Domain`, `Services.Abstraction`, `Services`, `Persistence`, and `API`) to ensure strict dependency inversion and separation of concerns.

* **Repository & Unit of Work:** Centralized data access and transaction management via Entity Framework Core.
* **Generic Caching Pattern:** Open generic repository (`ICacheRepository<T>`) dynamically routing to isolated Redis databases based on the calling context.
* **Pessimistic Concurrency Control:** Database-level locking on parent entities (Resources, Schedules) to prevent race conditions during heavy booking load.
* **Standardized Responses:** API endpoints return uniform `Result` and `Result<T>` envelopes for consistent client parsing.

## 🚀 Key Features Implemented

### 1. Advanced Authentication & Security
* **JWT Access Tokens:** Standard stateless authentication.
* **Token Family Rotation:** Refresh tokens are tracked via `FamilyId`. Generating a new refresh token instantly revokes the previous one.
* **Token Reuse Detection:** If a compromised or expired refresh token is replayed, the system instantly revokes the entire token family, forcing a secure re-authentication.

### 2. Multi-Tenancy & Resource Management
* **Tenant Scoping:** Core entities and requests are strictly isolated by `TenantId`.
* **Resource & Schedule Setup:** Tenant Admins can create and configure bookable resources and time slots.
* **Background Job Orchestration:** Integration with **Hangfire** for scheduling lifecycle management. Updating a schedule automatically purges old jobs and queues new ones.

### 3. High-Reliability Booking Engine
* **Transactional Integrity:** Strict unique constraints and parent-locking ensure overlapping bookings or double-bookings are mathematically impossible.
* **Schedule Capacity Caching:** Transitioning schedule availability checks from SQL aggregates (`COUNT()`) to atomic Redis operations (`INCR`/`DECR`) for sub-millisecond reads.
* **Idempotency Gateway:** A custom `IdempotencyFilter` caches successful API responses in **Redis**. 
    * Safeguards the database against network retries.
    * Bypasses heavy pessimistic locks by returning the cached JSON payload (HTTP Status Code + Body) on duplicate requests.
    * Supports `POST` and complex `PUT` endpoints (like `Schedule_Update` with side effects).

## 🧪 Testing Strategy

Stability and reliability are core pillars of this project. To ensure the business logic remains robust as the codebase grows, a comprehensive suite of **over 200+ unit tests** is maintained covering our services, validators, and core domain rules.

### 🛠️ The Testing Stack

Unit tests are built using industry-standard tools designed for isolation, readability, and speed:

* **[xUnit](https://xunit.net/)** – Our modern, robust testing framework of choice, utilized for structuring clean, isolated test execution paths.
* **[Moq](https://github.com/devlooped/moq)** – Used to cleanly isolate our service-layer logic by mocking external dependencies (such as repositories, identity managers, and validators).
* **[FluentAssertions](https://fluentassertions.com/)** – Employs a highly readable, natural-language assertion style that makes test failures incredibly easy to diagnose.

---

### ✍️ Test Design Patterns

A **Self-Contained AAA (Arrange, Act, Assert)** approach is followed. Rather than relying on shared state, constructor setups, or global mocks that can hide logic:

* **Every test is a standalone story.** All mocked behavior, inputs, and expected outcomes are declared explicitly inside the test itself.
* **Zero side-effects.** Tests do not leak state or configuration to other tests, guaranteeing predictable execution and preventing flaky test runs.
* **Optimized Test Doubles.** Heavy, slow-running integration paths (like multi-tenant query filters, database transactions, or resource locks) are deliberately out-of-scope for our unit tests, keeping our test suite blazing fast.

> 💡 **CI/CD Friendly:** Because our unit tests are completely decoupled from external databases and networks, the entire suite of 200+ tests runs in seconds, making it ideal for pull request validation gates.

---

### 🚀 Running the Tests

To run the entire unit test suite from your terminal, execute:

```bash
dotnet test
```


## 🗺 What's Next?

* **Payment Gateway Integration:** Handling booking payments and issuing refunds.
* **Waitlist & Notifications:** Automated queuing and notification triggers for highly contested schedules.
* **Subscription Plans:** SaaS tiering (e.g., Free, Pro, Enterprise) linked to tenant limits.