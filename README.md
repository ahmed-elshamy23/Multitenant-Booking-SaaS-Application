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

When the application is first seeded, a default **System Owner** account is created. 

The Owner role acts as the site's overall admin, allowing you to perform global CRUD operations on tenants. Because the Owner does not belong to a specific tenant, you **do not** need to provide the `x-tenant-id` header when hitting the `/login` endpoint with these credentials.

* **Email:** `owner@gmail.com`
* **Password:** `P@ssw0rd`

> ⚠️ **Important:** These credentials are for local development and testing only. You must change this password or remove the seed data before deploying to a production environment.

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

## 🗺 What's Next?

* **Payment Gateway Integration:** Handling booking payments and issuing refunds.
* **Waitlist & Notifications:** Automated queuing and notification triggers for highly contested schedules.
* **Subscription Plans:** SaaS tiering (e.g., Free, Pro, Enterprise) linked to tenant limits.