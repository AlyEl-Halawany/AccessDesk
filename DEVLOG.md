# Architecture & Design Log (DEVLOG)

## 1. Why Clean Architecture?
AccessDesk separates `Domain`, `Application`, `Infrastructure`, and `Api` into distinct projects:
- **`AccessDesk.Domain`** holds entities (`AccessRequest`, `AuditLog`, `AccessReview`), enums, and repository interfaces. It has zero dependency on EF Core or ASP.NET Core HTTP types, which makes core state-machine rules trivial to unit test.
- **`AccessDesk.Application`** contains MediatR commands/queries and FluentValidation validators. Handlers orchestrate use cases through repository interfaces without knowing whether data lives in SQL Server or an in-memory test store.
- **`AccessDesk.Infrastructure`** implements EF Core (`AccessDeskDbContext`), Identity/JWT (`TokenService`), SMTP email (`SmtpEmailService`), and the `AccessExpiryService` background worker.
- **`AccessDesk.Api`** is a thin HTTP layer responsible for routing, `[Authorize(Roles = ...)]` policies, Swagger, and global exception handling.

## 2. Why Short-Lived JWTs + Rotating Refresh Tokens?
- **Access tokens (15 min)** are stateless JWTs validated locally on every API request without a database round-trip.
- **Refresh tokens (7 days)** are cryptographically random strings persisted in the `RefreshTokens` table. When a client refreshes, the old token is marked `RevokedAt` and replaced (`ReplacedByToken`).
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
