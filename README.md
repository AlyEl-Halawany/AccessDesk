# AccessDesk

[![CI](https://github.com/AlyEl-Halawany/AccessDesk/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/AlyEl-Halawany/AccessDesk/actions/workflows/ci.yml)

AccessDesk is a portfolio-scale Identity Governance and Administration (IGA) application. Employees request time-bound application access; managers and application owners approve it; SoD rules, JML events, access reviews, expiry processing, and audit search govern the resulting access.

It demonstrates IAM/IGA controls in a working .NET 10 and React stack. It does not claim regulatory compliance or production readiness.

## Contents

- [What it does](#what-it-does)
- [Governance concepts](#governance-concepts)
- [Screenshots](#screenshots)
- [Architecture](#architecture)
- [Authentication and approval flow](#authentication-and-approval-flow)
- [Audit and concurrency](#audit-and-concurrency)
- [Technology stack](#technology-stack)
- [Local setup](#local-setup)
- [Build and tests](#build-and-tests)
- [Design decisions](#design-decisions-and-tradeoffs)
- [Security considerations](#security-considerations-and-future-improvements)
- [Repository map](#repository-map)

## What it does

| Capability | Behavior |
|---|---|
| Access requests | Employees submit an application, level, duration, and justification (minimum 20 characters). |
| Two-stage approval | Manager approval, then application-owner approval, with required rejection reasons. |
| SoD | Blocking rules reject the submission; non-blocking rules flag it for SecurityAdmin review. |
| Time-bound grants | Approved access expires; a background worker records expiry and notifies the user. |
| JML | Joiners create accounts; movers reassign managers; leavers lose active grants and refresh sessions. |
| Access reviews | SecurityAdmin launches campaigns; managers confirm or revoke assigned entitlements. |
| Audit | SecurityAdmin searches an append-only log of security-relevant actions. |
| Session security | Short-lived JWTs, rotating hashed refresh tokens with reuse detection, login throttling, and account lockout. |

Role boundaries: **Employee** requests and views own access; **Manager** and **AppOwner** approve their steps; **SecurityAdmin** manages applications, SoD, reviews, JML, and audit search.

## Governance concepts

- **IAM** controls who can sign in and which roles they hold. AccessDesk uses ASP.NET Core Identity, JWTs, and role-based API policies.
- **IGA** governs why access is needed, who approved it, how long it remains active, and whether it should be reviewed or revoked.
- **RBAC** assigns user capabilities through Employee, Manager, AppOwner, and SecurityAdmin roles.
- **SoD** prevents conflicting access combinations, such as administering payroll and independently auditing financial records.
- **JML** models employee joiner, mover, and leaver changes; a leaver loses active grants and refresh sessions.
- **Access certification** asks reviewers to confirm or revoke active entitlements on a campaign schedule.
- **Auditability** makes security actions searchable. App paths reject ordinary audit edits/deletes, but database administrators remain trusted operators.

## Screenshots

Captures of the running Development UI are stored in [`docs/screenshots/`](docs/screenshots/). They were generated with Playwright against SQL Server-backed data, not mocked fixtures. Screenshots cover Employee, Manager, AppOwner, and SecurityAdmin experiences.

| Screen | Screenshot |
|---|---|
| Login | ![Login](docs/screenshots/login.png) |
| Dashboard | ![Dashboard](docs/screenshots/dashboard.png) |
| New request | ![New request](docs/screenshots/new-request.png) |
| Approval inbox | ![Approval inbox](docs/screenshots/approval-inbox.png) |
| Application-owner inbox | ![Application-owner inbox](docs/screenshots/approval-inbox-owner.png) |
| My access | ![My access](docs/screenshots/my-access.png) |
| Audit log | ![Audit log](docs/screenshots/audit-log.png) |
| Applications and SoD | ![Applications and SoD](docs/screenshots/applications.png) |
| JML console | ![JML console](docs/screenshots/jml-console.png) |
| Access reviews | ![Access reviews](docs/screenshots/access-reviews.png) |

Recreate them after the API (`http://localhost:5201`) and Vite app (`http://localhost:5173`) are running:

```powershell
cd frontend
npm ci
npx playwright install chromium
npm run capture:screenshots
```

The script signs in with Development demo accounts and writes PNG files to `docs/screenshots/`.

## Architecture

Clean Architecture keeps business rules independent of HTTP and persistence. Dependencies point inward: Application depends on Domain contracts; Infrastructure implements them; the API is the composition root. The React SPA talks to the API over HTTP with JWT bearer tokens.

```mermaid
flowchart LR
  SPA[React 19 + TypeScript] -->|HTTP / JWT| API[AccessDesk.Api]
  API --> APP[AccessDesk.Application]
  API --> INF[AccessDesk.Infrastructure]
  APP --> DOM[AccessDesk.Domain]
  INF --> DOM
  INF --> DB[(SQL Server)]
  INF --> SMTP[SMTP / MailHog]
```

### Data model

```mermaid
erDiagram
  ApplicationUser ||--o{ AccessRequest : requests
  ApplicationUser ||--o{ Application : owns
  ApplicationUser o|--o{ ApplicationUser : manages
  Application ||--o{ AccessRequest : protects
  AccessRequest ||--o{ AuditLog : records
  AccessRequest ||--o{ AccessReviewItem : reviewed
  AccessReview ||--o{ AccessReviewItem : contains
  ApplicationUser ||--o{ RefreshToken : receives
  ApplicationUser ||--o{ Notification : receives
  Application ||--o{ SodConflict : first_application
  Application ||--o{ SodConflict : second_application
```

`AccessRequest` stores the requested level, justification, duration, status, approvers/timestamps, and SQL Server `rowversion`. `RefreshToken` stores a SHA-256 digest, never the raw bearer token. Audit writes are append-only through the application path; a database administrator can still alter rows directly.

## Authentication and approval flow

Identity hashes passwords. Access tokens are short-lived JWTs. Refresh rotates a random token in a database transaction; only its hash is stored. Reuse of a revoked token revokes the user's other active refresh tokens. Login is limited to five attempts per IP per minute; Identity locks an account after five failed password checks.

```mermaid
stateDiagram-v2
  [*] --> PendingManager: employee submits
  PendingManager --> PendingOwner: manager approves
  PendingManager --> Rejected: manager rejects
  PendingOwner --> Approved: owner approves
  PendingOwner --> Rejected: owner rejects
  PendingManager --> Cancelled: requester cancels
  PendingOwner --> Cancelled: requester cancels
  Approved --> Expired: expiry worker
  PendingManager --> FlaggedForReview: nonblocking SoD conflict
  PendingOwner --> FlaggedForReview: nonblocking SoD conflict
```

A blocking SoD rule rejects the submission. A nonblocking rule flags it for SecurityAdmin review. The sample rule pairs Payroll Admin and Financial Audit Admin access.

## Audit and concurrency

Audit search is restricted to SecurityAdmin. The repository exposes add/search, the API exposes GET only, and `AccessDeskDbContext` rejects tracked updates/deletes; integration tests cover both paths. This is not immutable storage or cryptographic tamper evidence.

SQL Server generates a new `rowversion` for each access-request update. A stale concurrent approval produces HTTP 409 ProblemDetails. An opt-in SQL Server test verifies the second update fails.

## Technology stack

- **Backend:** ASP.NET Core / .NET 10, C# (nullable enabled), EF Core 10.0.13, SQL Server, ASP.NET Core Identity.
- **Application:** MediatR 12.4.1, FluentValidation 11.11, Serilog, JWT bearer, MailKit 5.
- **Frontend:** React 19, TypeScript 7, Vite 8, React Router 7, TanStack Query 5, Tailwind 4, Recharts 3, Lucide icons.
- **Tests:** xUnit 2, Moq, FluentAssertions, WebApplicationFactory, EF InMemory; opt-in SQL Server concurrency test.
- **Tooling:** local `dotnet-ef` 10.0.13, Docker Compose, GitHub Actions (.NET 10, Node 22), Playwright screenshot capture.

## Local setup

Prerequisites: .NET 10 SDK, Node.js LTS/npm, and SQL Server Express/LocalDB (or Docker for Compose).

### Configure database and JWT

The ignored Development settings file `src/AccessDesk.Api/appsettings.Development.json` uses `localhost\SQLEXPRESS`. Change `ConnectionStrings:DefaultConnection` there or set `ConnectionStrings__DefaultConnection`. Example:

```text
Server=localhost\SQLEXPRESS;Database=AccessDeskDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Keep development settings and signing keys out of Git. Store a local signing key with user-secrets:

```powershell
dotnet user-secrets init --project src/AccessDesk.Api
dotnet user-secrets set "Jwt:Secret" "Replace-With-A-Local-Secret-At-Least-32-Chars" --project src/AccessDesk.Api
```

### Run the API

```powershell
dotnet tool restore
dotnet ef database update --project src/AccessDesk.Infrastructure --startup-project src/AccessDesk.Api
dotnet run --project src/AccessDesk.Api --launch-profile http
```

The API listens at `http://localhost:5201`. Swagger is `/swagger`, health is `/health`. Startup migrates in non-Testing environments and seeds demo data only in Development.

### Run the frontend

```powershell
cd frontend
npm ci
npm run dev
```

Vite serves `http://localhost:5173` and proxies `/api` to port 5201.

### Docker Compose

In PowerShell, provide local-only values first (Compose rejects missing values):

```powershell
$env:MSSQL_SA_PASSWORD = Read-Host "Local SQL Server SA password"
$env:JWT_SECRET = Read-Host "Local JWT signing secret (32+ characters)"
docker compose up --build
Remove-Item Env:MSSQL_SA_PASSWORD, Env:JWT_SECRET
```

Compose exposes frontend `http://localhost:3000`, API `http://localhost:5000`, SQL Server on 1433, and MailHog UI on 8025.

### Development demo accounts

These accounts are seeded only in Development. Do not reuse their passwords elsewhere.

| Role | Email | Password |
|---|---|---|
| SecurityAdmin | `admin@accessdesk.dev` | `Admin@123!` |
| Manager | `alice@accessdesk.dev` | `Manager@123!` |
| AppOwner | `bob@accessdesk.dev` | `Owner@123!` |
| Employee | `charlie@accessdesk.dev` | `Employee@123!` |
| Employee | `diana@accessdesk.dev` | `Employee@123!` |
| Employee | `evan@accessdesk.dev` | `Employee@123!` |

The login page can fill these accounts for local walkthroughs.

## Build and tests

```bash
dotnet build AccessDesk.slnx
dotnet test AccessDesk.slnx
cd frontend && npm ci && npm run build
```

The default API integration tests use EF InMemory. For the SQL Server rowversion test, set `ACCESSDESK_SQLSERVER_CONNECTION` and run:

```powershell
$env:ACCESSDESK_SQLSERVER_CONNECTION = "Server=localhost\SQLEXPRESS;Database=AccessDeskDb;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test tests/AccessDesk.IntegrationTests/AccessDesk.IntegrationTests.csproj
```

## Design decisions and tradeoffs

- **Clean Architecture:** isolates business rules and makes provider swaps/test doubles possible; it adds project and mapping boundaries.
- **MediatR:** makes use cases and validation explicit; it adds indirection, so simple reads remain direct.
- **Refresh-token rotation:** limits replay and enables revocation; an already-issued access JWT remains valid until expiry.
- **Append-only audit path:** repository/controller/EF controls prevent ordinary app mutation; a database administrator can still edit rows without immutable external storage.
- **Rowversion:** detects stale approvals without long-held locks; a conflicting client must reload and retry.

## Security considerations and future improvements

This is a learning project, not a production deployment. Production use would need external identity/MFA, managed secrets, operational monitoring, backup and retention policies, database-level immutable audit storage, and an HRIS integration. SMTP is local MailHog; campaign persistence succeeds even when SMTP is unavailable.

Access JWTs are stateless and remain usable until their short expiry even after refresh-token revocation or leaver processing.

Future work: OIDC/Entra sign-in, durable expiry scheduling and delivery retries, SQL integration tests in CI, browser end-to-end tests in CI, tenant-aware administration, and immutable external audit retention.

## Repository map

| Path | Role |
|---|---|
| `src/AccessDesk.Domain/` | Entities, enums, state transitions, repository contracts |
| `src/AccessDesk.Application/` | MediatR use cases, validators, pipeline behavior |
| `src/AccessDesk.Infrastructure/` | EF mappings/migrations, Identity/JWT/mail, expiry worker |
| `src/AccessDesk.Api/` | Controllers, auth policy, middleware, composition root |
| `tests/` | Unit tests and WebApplicationFactory integration tests |
| `frontend/` | React SPA |
| `frontend/capture-screenshots.mjs` | Playwright captures for README images |
| `docs/screenshots/` | Login, dashboard, request, manager/owner approvals, access, apps/SoD, audit, JML, reviews |
| `.github/workflows/ci.yml` | Backend build/tests and frontend production build |
| `HANDOFF.md` | Session status, verification, and known blockers |

Additional notes: [`DEVLOG.md`](DEVLOG.md), [`KNOWN_ISSUES.md`](KNOWN_ISSUES.md), [`INTERVIEW_NOTES.md`](INTERVIEW_NOTES.md), [`AGENTS.md`](AGENTS.md).
