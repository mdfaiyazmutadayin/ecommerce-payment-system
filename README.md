# E-commerce Ordering & Payment System

A backend system for managing users, products, orders, and payments with support for multiple payment providers (Stripe, bKash), built with a 3-layer .NET Framework architecture ((ecommerce)-Presentation Layer / BLL / DAL).

**Live demo:** `https://ecommerce-payment-system.vercel.app/`
**API base URL (via ngrok tunnel):** `https://bubble-epidermis-landslide.ngrok-free.dev`
**Example:** `https://bubble-epidermis-landslide.ngrok-free.dev/api/Product/all`
> ⚠️ The backend runs locally and is exposed via ngrok per the assessment's deployment requirement. If the link above is unreachable, it means the local machine hosting it is offline — see [Known Limitations](#known-limitations) below.

---

## Table of contents
- [System architecture](#system-architecture)
- [Entity relationship diagram](#entity-relationship-diagram)
- [Tech stack](#tech-stack)
- [Features implemented](#features-implemented)
- [Design patterns & algorithms](#design-patterns--algorithms)
- [API documentation](#api-documentation)
- [Setup & local development](#setup--local-development)
- [Deployment](#deployment)
- [Known limitations](#known-limitations)

---

## System architecture

![System architecture diagram]<img width="2720" height="2048" alt="ecommerce_system_architecture" src="https://github.com/user-attachments/assets/2d46ff51-083b-401c-87e2-84dd9fbc9206" />


Request flow: **React frontend (Vercel)** → **ngrok tunnel** (public HTTPS) → **ASP.NET Web API** (3-layer: Presentation / Business Logic / Data Access) → **SQL Server** + **Redis cache**, with the Business Logic layer also calling out to **Stripe** and **bKash**'s real APIs for payment processing.

---

## Entity relationship diagram

![ERD]<img width="2880" height="2610" alt="ecommerce_erd" src="https://github.com/user-attachments/assets/b5f54de8-9e61-467b-8f4f-5def2f7002a0" />


Core tables: `Users`, `Categories`, `Products`, `Orders`, `OrderItems`, `Payments`. Categories are self-referencing (`ParentCategoryId`) to support the hierarchical category tree used for product recommendations.

---

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | React (Vite), deployed on Vercel |
| Backend | ASP.NET Web API, .NET Framework 4.8, C# |
| Database | SQL Server, Entity Framework 6 (Code First + Migrations) |
| Cache | Redis (Memurai locally / Docker container) |
| Payments | Stripe API (test mode), bKash Tokenized Checkout (sandbox) |
| Local exposure | ngrok |
| Containerization | Docker Compose (SQL Server + Redis) |
| API testing | Postman |

---

## Features implemented

- ✅ **User management** — registration with hashed passwords (PBKDF2), login, unique email enforcement
- ✅ **Product management** — full CRUD, category assignment, stock tracking
- ✅ **Order management** — multi-item orders with deterministic total/subtotal calculation
- ✅ **Payment processing** — real Stripe API integration (test mode) and real bKash sandbox integration (Tokenized Checkout: create → execute → query)
- ✅ **Webhooks** — Stripe server-to-server webhook with signature verification; bKash browser-redirect callback
- ✅ **Stock reduction** — safely reduces inventory only after confirmed payment, inside a single DB transaction
- ✅ **Category tree + product recommendations** — DFS traversal over a Redis-cached category hierarchy

---

## Design patterns & algorithms

| Requirement | Where it lives |
|---|---|
| OOP | `User`, `Product`, `Order`, `Payment` model classes across all layers |
| Strategy pattern | `IPaymentStrategy` / `PaymentStrategyFactory` — swaps Stripe/bKash logic without touching `PaymentOrchestrator` |
| Deterministic algorithms | `OrderService.CreateOrderAsync` (totals/subtotals), `StockService.ValidateAndReduceAsync` (safe stock reduction) |
| DFS traversal | `CategoryService.GetDescendantCategoryIds` — explicit stack-based depth-first search over the category tree |
| Caching | `RedisCacheProvider` — category tree cached in Redis, invalidated on category writes |

---

## API documentation

Full Postman collection (all endpoints, organized by feature, with example requests): [`Ecommerce_API_Collection.postman_collection.json`]- https://drive.google.com/file/d/1cs5nWR_lpBF8tuWhV5VxHsY88A1ErXFq/view?usp=sharing


Import it directly into Postman — includes:
- User registration/login (+ negative test cases)
- Category tree + DFS-based related-products
- Product CRUD
- Order creation
- Stripe & bKash checkout/confirm flows, plus webhook/callback documentation

---

## Setup & local development

```bash
# Clone
git clone https://github.com/yourusername/ecommerce-payment-system.git

# Backend: open ecommerce.sln in Visual Studio, restore NuGet packages, then:
# 1. Update Web.config connection strings (SQL Server + Redis)
# 2. Run Update-Database in Package Manager Console
# 3. F5 to run via IIS Express

# Frontend
cd frontend
npm install
npm run dev
```

### Docker (database + cache)
```bash
docker compose up -d
```
Provisions SQL Server and Redis in containers per `docker-compose.yml`.

---

## Deployment

- **Frontend:** deployed to Vercel from the `frontend/` directory
- **Backend:** runs locally via IIS Express, exposed publicly via `ngrok http <port> --host-header=localhost`
- **Database** containerized via Docker Compose (`docker-compose.yml`, `Dockerfile` included)

---

## Known limitations

Documented honestly rather than omitted:

- **Backend uptime depends on a local machine staying online.** Since the assessment specifies "backend running locally via ngrok," the API is only reachable while the development machine, IIS Express, and the ngrok tunnel are all active.
- **API container not run end-to-end.** A `Dockerfile` for the ASP.NET backend is included and reviewed for correctness, but wasn't run to completion in this environment due to a Windows-container/virtualization limitation on the development machine's Docker Desktop install. SQL Server and Redis containers were built and verified successfully.
- **No authentication enforcement yet.** Registration/login work and passwords are hashed, but endpoints like order creation and payment don't yet require a valid session/token — this is a scoped next step, not an oversight.
- **Stripe/bKash use test/sandbox credentials only**, as appropriate for an assessment context — no live payment credentials are used anywhere in this project.
