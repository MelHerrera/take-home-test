# Loan Management System

A simple Loan Management System built with a **.NET 10** backend (Clean Architecture, EF Core, SQL Server) and an **Angular 19** frontend (Angular Material), developed as part of a take-home technical assessment.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API, Entity Framework Core |
| Database | SQL Server (LocalDB for local dev, SQL Server container in Docker) |
| Frontend | Angular 19 (standalone components), Angular Material |
| Testing | xUnit, Moq, FluentAssertions, SQLite (in-memory, integration tests) |
| Containerization | Docker, Docker Compose |

## Architecture

The backend follows **Clean (Onion) Architecture**, split into four projects under `backend/src`, plus a test project:

```
Fundo.Applications.WebApi   → Controllers, Program.cs (composition root), middleware
        │  depends on
        ▼
Fundo.Infrastructure        → EF Core DbContext, repositories, migrations
        │  depends on
        ▼
Fundo.Application           → DTOs, service interfaces/implementations, validation
        │  depends on
        ▼
Fundo.Domain                → Entities (Loan), enums (LoanStatus) — no external dependencies

Fundo.Services.Tests        → Unit tests (Application) + integration tests (WebApi)
```

Dependencies always point inward, toward `Domain`. `Domain` never depends on EF Core, ASP.NET Core, or any other infrastructure concern — it can be unit tested and reasoned about in complete isolation.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) + npm
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the containerized option)
- SQL Server LocalDB (ships with Visual Studio) — only needed for the non-Docker option

### Option A — Run the backend with Docker (recommended)

```sh
cd backend
cp .env.example .env
# edit .env and set a real MSSQL_SA_PASSWORD (must meet SQL Server's complexity policy)

docker compose up --build
```

This builds the API image, starts a SQL Server container, waits for it to be healthy, and applies migrations + seed data automatically on startup. The API is available at `http://localhost:8080`.

### Option B — Run the backend locally (LocalDB)

```sh
cd backend/src
dotnet build
dotnet test
cd Fundo.Applications.WebApi
dotnet run
```

Migrations and seed data are applied automatically on startup (`Database.Migrate()`), against LocalDB. The API listens on the ports declared in `Properties/launchSettings.json` (`https://localhost:60500` / `http://localhost:60501`).

### Frontend

```sh
cd frontend
npm install
npm start
```

Open `http://localhost:4200`. By default, `src/environments/environment.ts` points to the local backend port (`http://localhost:60501`). If you started the backend via Docker instead (port `8080`), update `apiUrl` in that file before running `npm start`.

## API Endpoints

| Method | Route | Description |
|---|---|---|
| `POST` | `/loans` | Create a new loan (`applicantName`, `amountRequested`) |
| `GET` | `/loans/{id}` | Get a single loan by id |
| `GET` | `/loans` | List all loans |
| `POST` | `/loans/{id}/payment` | Register a payment against a loan's balance |

All error responses use the standard [`ProblemDetails`](https://datatracker.ietf.org/doc/html/rfc7807) shape (`title`, `status`, `detail`, `instance`), produced by a global exception-handling middleware. Validation messages are resolved from resource files (`.resx`) rather than hardcoded strings, so they can be localized in the future.

## Testing

```sh
cd backend/src
dotnet test
```

19 tests total:
- **11 unit tests** for `LoanService` (business rules: overpayment, paying an already-settled loan, validation), using Moq for `ILoanRepository`.
- **8 integration tests** for the full HTTP pipeline (`WebApplicationFactory`), running against a fresh **SQLite in-memory** database per test (not a shared fixture), so no test can leak state into another regardless of execution order.

SQLite (not LocalDB) is used for integration tests specifically so the suite runs anywhere — including CI on Linux — without depending on a Windows-only database engine.

## Key Design Decisions

- **.NET 10** instead of the scaffold's original .NET 6 (which reached end-of-support in Nov 2024), with the modern minimal hosting model (`WebApplicationBuilder`, no `Startup.cs`).
- **Routes use `/loans`** (plural), matching the assessment's endpoint spec, rather than the original scaffold's singular `/loan`.
- **`ILoanRepository.GetAllAsync` returns `IReadOnlyList<Loan>`, not `IQueryable<Loan>`** — this keeps the ORM/provider-specific query-translation concerns fully contained inside `Fundo.Infrastructure`; nothing above that layer can accidentally compose a LINQ query the underlying provider can't translate.
- **`ExceptionHandlingMiddleware`** lives inside `Fundo.Applications.WebApi` rather than a separate shared library, since there is only one API in this repository today (YAGNI) — it's decoupled enough to extract later if a second API is ever added.
- **The payment dialog on the frontend only collects and validates input**; it doesn't call the API itself. The component that opened it decides what to do with the result — the standard pattern for Angular Material dialogs, and it keeps the dialog reusable/testable independent of any specific API call.
- **Structured logging with Serilog** (console + rolling daily JSON file, one compact line per request via `UseSerilogRequestLogging`). The file sink keeps the last 30 days (`retainedFileCountLimit: 30`) — a reasonable default for this project, but real retention should follow each organization's actual log-retention/compliance policy rather than an arbitrary number.

## Challenges Encountered

A few real issues were found and fixed during development (not merely anticipated — actually hit and debugged):

1. **`IStringLocalizer<T>` silently resolving to the wrong (or no) resource.** Namespacing the resource marker type under `.Resources` while also setting `ResourcesPath` caused the localizer to look for a doubled path (`Resources.Resources.X`) that didn't exist — and `IStringLocalizer`'s fallback is to silently return the resource *key* instead of throwing. Diagnosed by inspecting the compiled assembly's actual embedded resource names (`Assembly.GetManifestResourceNames()`) rather than assuming the naming convention.
2. **Swapping the EF Core provider in `WebApplicationFactory` for tests.** Removing only the `DbContextOptions<T>` service descriptor wasn't enough on EF Core 7+: `AddDbContext` also registers `IDbContextOptionsConfiguration<T>` entries that accumulate across calls instead of being replaced, so the old SQL Server configuration stayed active alongside the new SQLite one, and EF Core rejected having two providers registered at once. Fixed by removing both descriptor types.
3. **Automatic migrations (`Database.Migrate()`) conflicting with the test suite's schema setup.** `Migrate()` (used for real startup, both local and Docker) and `EnsureCreated()` (used by the SQLite test factory) are mutually exclusive schema-initialization strategies. Resolved with an explicit `SkipAutoMigrate` configuration flag injected only by the test factory, rather than overloading the environment name for something unrelated to environment.
4. **Missing `.dockerignore` leaking host-specific build artifacts into the container.** Without it, `COPY` picked up local `bin/`/`obj/` folders containing a Windows-specific NuGet fallback path baked into `project.assets.json`, which broke the build inside the Linux container. Fixed by excluding `bin/`, `obj/`, and IDE folders from the Docker build context.
5. **A known high-severity vulnerability (CVE-2025-6965) in a transitive dependency** (`SQLitePCLRaw.lib.e_sqlite3`, pulled in by the SQLite test provider) was caught during dependency review and fixed by explicitly pinning `SQLitePCLRaw.bundle_e_sqlite3` to a patched version.

## Known Limitations

- **No payment history/audit trail.** `POST /loans/{id}/payment` mutates `Loan.CurrentBalance` directly; there's no `Payment` entity recording individual transactions (amounts, dates). See "Potential Improvements" below.
- **No optimistic concurrency control.** Two simultaneous payments against the same loan could race (a "lost update"): both read the same balance, both write, one silently overwrites the other. Mitigation is a known, deliberate omission given the timeframe (see below).
- **No search or pagination** on `GET /loans` — acceptable at the current seed-data scale, would not be at production scale.
- **The frontend is not containerized** — Docker was only required for the backend per the assessment spec; the frontend runs via `npm start`.
- **Integration tests validate functional correctness only** (mapping, request/response flow, known error paths) against a small, fixed, deterministic dataset — they do not exercise performance at scale, real-world data diversity, or concurrency, which are different testing disciplines (load testing, fuzz testing) out of scope here.

## Potential Improvements

- Model `Payment` as its own entity (`Id`, `LoanId`, `Amount`, `Date`), exposed as `POST /loans/{id}/payments` (plural collection) — the more RESTful shape for genuinely *creating* a resource (`201 Created` + `Location`), and it would provide a real payment audit trail.
- Add a `RowVersion`/concurrency token to `Loan` so EF Core throws `DbUpdateConcurrencyException` on a conflicting concurrent write instead of silently losing a payment.
- Support multiple database providers via a configuration-driven switch in `AddInfrastructure` (e.g. `"DatabaseProvider": "SqlServer" | "Postgres"`), if a real multi-environment need arose.
- Search/filter (by status) and pagination on `GET /loans` — the repository interface already returns a materialized list rather than `IQueryable`, so this would be added as explicit repository parameters, not a leaked query surface.
- Centralize frontend loading-state handling (an `HttpInterceptor` + shared loading service) if the app grows beyond its current single view.
- Structured logging (Serilog), JWT authentication, and a GitHub Actions CI pipeline, per the assessment's optional bonus items.

---

## Original Take-Home Test Instructions

The original assignment instructions, kept below for reference:

# **Take-Home Test: Backend-Focused Full-Stack Developer (.NET C# & Angular)**

## **Objective**

This take-home test evaluates your ability to develop and integrate a .NET Core (C#) backend with an Angular frontend, focusing on API design, database integration, and basic DevOps practices.

## **Instructions**

1.  **Fork the provided repository** before starting the implementation.
2.  Implement the requested features in your forked repository.
3.  Once you have completed the implementation, **send the link** to your forked repository via email for review.

## **Task**

You will build a simple **Loan Management System** with a **.NET Core backend (C#)** exposing RESTful APIs and a **basic Angular frontend** consuming these APIs.

---

## **Requirements**

### **1. Backend (API) - .NET Core**

* Create a **RESTful API** in .NET Core to handle **loan applications**.
* Implement the following endpoints:
    * `POST /loans` → Create a new loan.
    * `GET /loans/{id}` → Retrieve loan details.
    * `GET /loans` → List all loans.
    * `POST /loans/{id}/payment` → Deduct from `currentBalance`.
* Loan example (feel free to improve it):

    ```json
    {
        "amount": 1500.00, // Amount requested
        "currentBalance": 500.00, // Remaining balance
        "applicantName": "Maria Silva", // User name
        "status": "active" // Status can be active or paid
    }
    ```

* Use **Entity Framework Core** with **SQL Server**.
* Create seed data to populate the loans (the frontend will consume this).
* Write **unit/integration tests for the API** (xUnit or NUnit).
* **Dockerize** the backend and create a **Docker Compose** file.
* Create a README with setup instructions.

### **2. Frontend - Angular (Simplified UI)**  

Develop a **lightweight Angular app** to interact with the backend

#### **Features:**  
- A **table** to display a list of existing loans.  

#### **Mockup:**  
[View Mockup](https://kzmgtjqt0vx63yji8h9l.lite.vusercontent.net/)  
(*The design doesn't need to be an exact replica of the mockup—it serves as a reference. Aim to keep it as close as possible.*)  

---

## **Bonus (Optional, Not Required)**

* **Improve error handling and logging** with structured logs.
* Implement **authentication**.
* Create a **GitHub Actions** pipeline for building and testing the backend.

---

## **Evaluation Criteria**

✔ **Code quality** (clean architecture, modularization, best practices).

✔ **Functionality** (the API and frontend should work as expected).

✔ **Security considerations** (authentication, validation, secure API handling).

✔ **Testing coverage** (unit tests for critical backend functions).

✔ **Basic DevOps implementation** (Docker for backend).

---

## **Additional Information**

Candidates are encouraged to include a `README.md` file in their repository detailing their implementation approach, any challenges they faced, features they couldn't complete, and any improvements they would make given more time. Ideally, the implementation should be completed within **two days** of starting the test.
