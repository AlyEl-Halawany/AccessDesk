# AccessDesk — .NET Interview Prep (30+ Questions & Answers)

Each answer references the exact file in this codebase where the concept is implemented.

---

### OOP & Domain Modeling

1. **Why do `AccessRequest` methods like `ApproveByManager` and `Reject` live on the entity instead of inside a service?**
   - **Answer:** Putting state transitions on the entity (rich domain model) ensures invalid transitions (e.g., approving an already rejected request) are impossible regardless of which caller invokes them.
   - **File:** [AccessRequest.cs](src/AccessDesk.Domain/Entities/AccessRequest.cs)

2. **What is the purpose of `BaseEntity` and `protected set` on `Id` and `CreatedAt`?**
   - **Answer:** `BaseEntity` centralizes common identity and audit timestamps (`Id`, `CreatedAt`, `UpdatedAt`) using inheritance, while `protected set` prevents external code from arbitrarily overwriting primary keys.
   - **File:** [BaseEntity.cs](src/AccessDesk.Domain/Common/BaseEntity.cs)

3. **Why is `ExpiresAt` a computed property instead of a stored column?**
   - **Answer:** `ExpiresAt => AccessGrantedAt?.AddDays(DurationDays)` avoids data inconsistency between `AccessGrantedAt`, `DurationDays`, and `ExpiresAt`. In EF Core we explicitly ignore it with `builder.Ignore(r => r.ExpiresAt)`.
   - **File:** [AccessRequest.cs](src/AccessDesk.Domain/Entities/AccessRequest.cs), [AccessRequestConfiguration.cs](src/AccessDesk.Infrastructure/Persistence/Configurations/AccessRequestConfiguration.cs)

4. **How are enums like `RequestStatus` and `AccessLevel` stored in SQL Server?**
   - **Answer:** Configured with `.HasConversion<string>()` in EF Core Fluent API so database rows store human-readable strings (`"PendingManager"`, `"Approved"`) instead of opaque integers.
   - **File:** [AccessRequestConfiguration.cs](src/AccessDesk.Infrastructure/Persistence/Configurations/AccessRequestConfiguration.cs)

5. **How does `AccessDesk.Domain` model the manager-employee hierarchy?**
   - **Answer:** `ApplicationUser` has a self-referencing foreign key `ManagerId` pointing to `ApplicationUser? Manager` and a `DirectReports` collection, configured with `DeleteBehavior.Restrict` so deleting a manager doesn't cascade-delete employees.
   - **File:** [ApplicationUser.cs](src/AccessDesk.Domain/Entities/ApplicationUser.cs), [ApplicationUserConfiguration.cs](src/AccessDesk.Infrastructure/Persistence/Configurations/ApplicationUserConfiguration.cs)

---

### SOLID & Clean Architecture

6. **How does AccessDesk apply the Dependency Inversion Principle (DIP)?**
   - **Answer:** `AccessDesk.Application` depends only on interfaces (`IAccessRequestRepository`, `IAuditLogRepository`, `IEmailService`) defined in `AccessDesk.Domain`. `AccessDesk.Infrastructure` implements them and is wired in `Program.cs`.
   - **File:** [IAccessRequestRepository.cs](src/AccessDesk.Domain/Interfaces/IAccessRequestRepository.cs), [InfrastructureServiceExtensions.cs](src/AccessDesk.Infrastructure/InfrastructureServiceExtensions.cs)

7. **Give an example of the Single Responsibility Principle (SRP) in the request pipeline.**
   - **Answer:** Input validation is separated from business execution: `SubmitAccessRequestValidator` validates field rules, while `SubmitAccessRequestHandler` executes SoD checks, persistence, and auditing.
   - **File:** [SubmitAccessRequestValidator.cs](src/AccessDesk.Application/AccessRequests/Commands/SubmitAccessRequestValidator.cs), [SubmitAccessRequestHandler.cs](src/AccessDesk.Application/AccessRequests/Commands/SubmitAccessRequestHandler.cs)

8. **How does `ValidationBehavior<TRequest, TResponse>` work with MediatR?**
   - **Answer:** It implements `IPipelineBehavior<,>`, intercepts every MediatR command/query, runs all registered FluentValidation `IValidator<TRequest>` instances, and throws `ValidationException` before the handler runs if any rule fails.
   - **File:** [ValidationBehavior.cs](src/AccessDesk.Application/Common/Behaviors/ValidationBehavior.cs)

9. **Why is `AuditLog` enforced as append-only at multiple layers?**
   - **Answer:** `IAuditLogRepository` only exposes `AddAsync` and `SearchAsync` (Interface Segregation), and `AuditLogsController` only exposes `GET` — no `PUT`/`DELETE` endpoints exist.
   - **File:** [IAuditLogRepository.cs](src/AccessDesk.Domain/Interfaces/IAuditLogRepository.cs), [AuditLogsController.cs](src/AccessDesk.Api/Controllers/AuditLogsController.cs)

10. **How does `GlobalExceptionMiddleware` keep API responses secure?**
    - **Answer:** It catches `ValidationException` to return structured 400 errors and catches unexpected exceptions to log them via Serilog while returning a generic 500 JSON message without stack traces.
    - **File:** [GlobalExceptionMiddleware.cs](src/AccessDesk.Api/Middleware/GlobalExceptionMiddleware.cs)

---

### Entity Framework Core & SQL

11. **Why do we use `IEntityTypeConfiguration<T>` classes instead of putting everything in `OnModelCreating`?**
    - **Answer:** Splitting configuration per entity keeps `AccessDeskDbContext.OnModelCreating` clean via `builder.ApplyConfigurationsFromAssembly(...)`.
    - **File:** [AccessDeskDbContext.cs](src/AccessDesk.Infrastructure/Persistence/AccessDeskDbContext.cs)

12. **Why did we add database indexes on `AccessRequests.Status` and `AuditLogs.Timestamp`?**
    - **Answer:** Approval queues frequently filter by `Status`, and audit log searches filter and sort by `Timestamp`. Indexes avoid full table scans as data grows.
    - **File:** [AccessRequestConfiguration.cs](src/AccessDesk.Infrastructure/Persistence/Configurations/AccessRequestConfiguration.cs), [AuditLogConfiguration.cs](src/AccessDesk.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs)

13. **What is the N+1 query problem and how does AccessDesk avoid it?**
    - **Answer:** Lazy loading related entities in a loop causes N+1 SQL queries. Repositories use eager loading with `.Include(...)` and `.ThenInclude(...)` to fetch related `Requester` and `Application` data in one query.
    - **File:** [AccessRequestRepository.cs](src/AccessDesk.Infrastructure/Persistence/Repositories/AccessRequestRepository.cs)

14. **Why does `DatabaseSeeder` check `if (db.Database.IsRelational())` before calling `MigrateAsync()`?**
    - **Answer:** `MigrateAsync()` executes SQL migrations, which throws on EF Core's `InMemoryDatabase` used in `WebApplicationFactory` integration tests.
    - **File:** [DatabaseSeeder.cs](src/AccessDesk.Infrastructure/Persistence/DatabaseSeeder.cs)

15. **What is `DeleteBehavior.Restrict` and why is it used on `AccessRequests` foreign keys?**
    - **Answer:** SQL Server rejects multiple cascade delete paths to the same table (`ApplicationUser` referenced as Requester, ManagerApprover, and OwnerApprover). `Restrict` prevents accidental cascading deletes and satisfies SQL Server constraints.
    - **File:** [AccessRequestConfiguration.cs](src/AccessDesk.Infrastructure/Persistence/Configurations/AccessRequestConfiguration.cs)

---

### Async, Concurrency & Background Services

16. **Why does `AccessExpiryService` inject `IServiceScopeFactory` instead of `AccessDeskDbContext` directly?**
    - **Answer:** `BackgroundService` is a singleton, whereas `DbContext` is scoped (and not thread-safe). Creating a fresh scope on each iteration ensures a clean, short-lived `DbContext` and avoids memory leaks or stale tracked entities.
    - **File:** [AccessExpiryService.cs](src/AccessDesk.Infrastructure/BackgroundServices/AccessExpiryService.cs)

17. **Why are `CancellationToken` parameters passed through controllers, MediatR handlers, and repositories?**
    - **Answer:** Passing `CancellationToken` allows ASP.NET Core to abort database queries and async work immediately if the HTTP client disconnects or the host shuts down.
    - **File:** [ApplicationsController.cs](src/AccessDesk.Api/Controllers/ApplicationsController.cs)

18. **How does `SmtpEmailService` prevent SMTP outages from breaking access approvals?**
    - **Answer:** It wraps MailKit's `SendAsync` in a `try/catch`, logging the failure via `ILogger` without rethrowing so the core approval/review transaction still succeeds.
    - **File:** [SmtpEmailService.cs](src/AccessDesk.Infrastructure/Services/SmtpEmailService.cs)

---

### Dependency Injection & Configuration

19. **What are the three DI lifetimes in ASP.NET Core, and how are they used here?**
    - **Answer:** `Singleton` (`AccessExpiryService`), `Scoped` (repositories, `AccessDeskDbContext`, `TokenService`), and `Transient` (`ValidationBehavior<,>`).
    - **File:** [InfrastructureServiceExtensions.cs](src/AccessDesk.Infrastructure/InfrastructureServiceExtensions.cs), [ApplicationServiceExtensions.cs](src/AccessDesk.Application/ApplicationServiceExtensions.cs)

20. **How does AccessDesk use the Options pattern?**
    - **Answer:** `JwtSettings` and `SmtpSettings` are bound from configuration via `services.Configure<T>(section)` and injected as `IOptions<T>` into `TokenService` and `SmtpEmailService`.
    - **File:** [TokenService.cs](src/AccessDesk.Infrastructure/Services/TokenService.cs), [SmtpEmailService.cs](src/AccessDesk.Infrastructure/Services/SmtpEmailService.cs)

21. **How are secrets kept out of source control?**
    - **Answer:** `appsettings.json` leaves `Jwt:Secret` empty; local development uses `dotnet user-secrets` and `.gitignore` excludes `appsettings.Development.json` and `.env`.
    - **File:** [appsettings.json](src/AccessDesk.Api/appsettings.json), [.env.example](.env.example)

---

### Authentication & Authorization

22. **How are passwords hashed in AccessDesk?**
    - **Answer:** Via ASP.NET Core Identity's `UserManager<ApplicationUser>`, which uses salted PBKDF2 with HMAC-SHA256 by default.
    - **File:** [DatabaseSeeder.cs](src/AccessDesk.Infrastructure/Persistence/DatabaseSeeder.cs), [AuthController.cs](src/AccessDesk.Api/Controllers/AuthController.cs)

23. **How does `[Authorize(Roles = "SecurityAdmin")]` know a user's roles from a JWT?**
    - **Answer:** `TokenService.CreateAccessTokenAsync` embeds each role as a `ClaimTypes.Role` claim inside the signed JWT. `JwtBearerHandler` populates `HttpContext.User` from those claims.
    - **File:** [TokenService.cs](src/AccessDesk.Infrastructure/Services/TokenService.cs)

24. **Why set `ClockSkew = TimeSpan.Zero` in `TokenValidationParameters`?**
    - **Answer:** By default, JWT validation allows a 5-minute grace period after expiration. Setting `ClockSkew = TimeSpan.Zero` ensures tokens expire at their exact `exp` timestamp.
    - **File:** [InfrastructureServiceExtensions.cs](src/AccessDesk.Infrastructure/InfrastructureServiceExtensions.cs)

25. **How does refresh token rotation work?**
    - **Answer:** `TokenService.RefreshAsync` verifies the incoming token is active, marks it `RevokedAt = UtcNow`, generates a new `RefreshToken`, links `ReplacedByToken`, and returns a fresh JWT + refresh token pair.
    - **File:** [TokenService.cs](src/AccessDesk.Infrastructure/Services/TokenService.cs)

26. **What happens during a JML Leaver event?**
    - **Answer:** `JmlController.Leaver` sets `DeactivatedAt`, expires all active `Approved` requests, cancels pending requests, revokes all active refresh tokens, and writes an audit log entry.
    - **File:** [JmlController.cs](src/AccessDesk.Api/Controllers/JmlController.cs)

---

### Testing

27. **What is the difference between `AccessDesk.UnitTests` and `AccessDesk.IntegrationTests`?**
    - **Answer:** Unit tests test domain entities and MediatR handlers in isolation with `Moq` mocks; integration tests boot the full ASP.NET Core pipeline with `WebApplicationFactory<Program>` and test HTTP status codes, auth, and DB persistence.
    - **File:** [ApprovalHandlerTests.cs](tests/AccessDesk.UnitTests/Application/ApprovalHandlerTests.cs), [ApiIntegrationTests.cs](tests/AccessDesk.IntegrationTests/ApiIntegrationTests.cs)

28. **Why does `Program.cs` end with `public partial class Program { }`?**
    - **Answer:** Minimal-hosting `Program.cs` generates an internal `Program` class by default; declaring `public partial class Program` exposes it to `WebApplicationFactory<Program>` in the integration test project.
    - **File:** [Program.cs](src/AccessDesk.Api/Program.cs)

29. **How does `AccessDeskWebApplicationFactory` isolate tests from SQL Server?**
    - **Answer:** It removes `DbContextOptions<AccessDeskDbContext>` and `IDbContextOptionsConfiguration<AccessDeskDbContext>` from DI and registers a unique `UseInMemoryDatabase` per factory instance.
    - **File:** [AccessDeskWebApplicationFactory.cs](tests/AccessDesk.IntegrationTests/AccessDeskWebApplicationFactory.cs)

30. **How are Separation of Duties (SoD) conflicts evaluated?**
    - **Answer:** `SodConflictRepository.FindConflictsForRequestAsync` checks the user's currently `Approved` accesses against active `SodConflict` rules. `SubmitAccessRequestHandler` either blocks the request (if `IsBlocking` is true) or flags it as `FlaggedForReview`.
    - **File:** [OtherRepositories.cs](src/AccessDesk.Infrastructure/Persistence/Repositories/OtherRepositories.cs), [SubmitAccessRequestHandler.cs](src/AccessDesk.Application/AccessRequests/Commands/SubmitAccessRequestHandler.cs)
