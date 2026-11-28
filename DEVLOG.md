# Architecture & Design Log (DEVLOG)

## 1. Why Clean Architecture?
AccessDesk separates `Domain`, `Application`, `Infrastructure`, and `Api` into distinct projects:
- **`AccessDesk.Domain`** holds entities (`AccessRequest`, `AuditLog`, `AccessReview`), enums, and repository interfaces. It has zero dependency on EF Core or ASP.NET Core HTTP types, which makes core state-machine rules trivial to unit test.
- **`AccessDesk.Application`** contains MediatR commands/queries and FluentValidation validators. Handlers orchestrate use cases through repository interfaces without knowing whether data lives in SQL Server or an in-memory test store.
- **`AccessDesk.Infrastructure`** implements EF Core (`AccessDeskDbContext`), Identity/JWT (`TokenService`), SMTP email (`SmtpEmailService`), and the `AccessExpiryService` background worker.
- **`AccessDesk.Api`** is a thin HTTP layer responsible for routing, `[Authorize(Roles = ...)]` policies, Swagger, and global exception handling.

## 2. Why Short-Lived JWTs + Rotating Refresh Tokens?
- **Access tokens (15 min)** are stateless JWTs validated locally on every API request without a database round-trip.
- **Refresh tokens (7 days)** are cryptographically random strings sent to the client; only SHA-256 hashes are persisted. Rotation revokes the old hash and stores the replacement hash.
- If an employee leaves the company (`POST /api/jml/leaver`), we immediately revoke all their active refresh tokens in the database so they cannot mint new access tokens.

## 3. Why an Append-Only Audit Log?
In enterprise access governance, audit logs must be tamper-evident. AccessDesk enforces append-only behavior in two places:
1. `IAuditLogRepository` only exposes `AddAsync` and `SearchAsync` — there are no update or delete methods.
2. `AuditLogsController` only exposes `GET /api/audit-logs` (restricted to `SecurityAdmin`) — there are no `PUT`, `PATCH`, or `DELETE` endpoints.

## 4. How the Approval Workflow Is Modeled
`AccessRequest` encapsulates its own state machine (`ApproveByManager`, `ApproveByOwner`, `Reject`, `Cancel`, `Expire`, `FlagForSodReview`) instead of leaving status mutations scattered across controllers or services:
- `PendingManager` → `PendingOwner` → `Approved`
- Rejection at either stage requires a non-empty reason and transitions to `Rejected`.
- `AccessExpiryService` (`BackgroundService`) periodically queries expired `Approved` grants and calls `request.Expire()`, recording a system `AuditLog` entry and notifying the user.

## 5. Real SQL Server verification and security hardening (2026-11-28)
- Installed SQL Server 2025 Express and verified the migrations, startup seeding, demo login, manager/owner approval, blocking SoD, audit search, review campaign, JML events, and expiry service against `AccessDeskDb`.
- Aligned the .NET 10 framework and EF Core package references/tooling to 10.0.13. MailKit 5 removes the reported mail advisories; the SMTP call uses its request-object API. A patched Pkcs reference resolves the transitive SQL client advisory.
- Refresh token records now contain only SHA-256 hashes. Reusing a rotated token revokes the user’s remaining active tokens. The migration clears preexisting development token rows to invalidate plaintext sessions.
- Added SQL Server `rowversion` to access requests. An opt-in integration test confirms a second concurrent manager update raises `DbUpdateConcurrencyException`.
- Login uses a per-IP five-per-minute rate limit; existing Identity lockout remains five failed passwords. Demo-data seeding now runs only in Development.
- Added ProblemDetails responses, `/health`, bounded/filterable request and notification lists, existing bounded audit search, basic response security headers, and EF-enforced append-only audit logs with a mutation/deletion test.
- MailHog was not running on `localhost:1025`; email failure was logged while the review campaign persisted successfully.
