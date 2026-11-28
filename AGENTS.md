# AccessDesk Agent Rules

## Project summary
AccessDesk is an Identity Governance and Administration (IGA) demo for requesting and governing application access.
It implements manager/owner approvals, SoD checks, access reviews, JML events, temporary access, and audit search.
The repository contains a .NET 10 Clean Architecture API and a React 19/TypeScript/Vite frontend.

## Stack
- .NET SDK target `net10.0` (local verification host: SDK 10.0.404); C# with nullable enabled.
- ASP.NET Core, EF Core 10.0.13, SQL Server, ASP.NET Core Identity, JWT; MediatR 12.4.1, FluentValidation 11.11.0, MailKit 5.0.0.
- React 19, TypeScript ~7.0, Vite 8, Tailwind 4, TanStack Query 5; Node CI currently 22.
- xUnit 2.9.3, Moq 4.20.72, FluentAssertions 7.2.0, WebApplicationFactory 9.0.5.

## Architecture and conventions
- Read `HANDOFF.md` before changing code. Keep Domain independent of outer layers/frameworks.
- Dependency direction: Domain <- Application <- Infrastructure; API references Application and Infrastructure and is the composition root; frontend calls API over HTTP/JWT.
- Domain owns entities, enums, state transitions, and repository contracts. Application owns MediatR use cases, validators, and orchestration.
- Infrastructure owns EF Core mappings/migrations, repositories, Identity/token/email services, and background workers. API owns controllers, auth policy, middleware, and HTTP concerns.
- Follow existing names: PascalCase types/methods, camelCase locals/parameters, descriptive feature folders, async I/O with CancellationToken.
- Keep business transitions on domain entities; use EF `IEntityTypeConfiguration<T>` and repository interfaces as already established.
- Keep AuditLog append-only: do not add mutation/deletion paths. Enforce authorization at API boundaries; do not expose secrets or stack traces.
- Demo credentials are development-only. Never commit real credentials, signing keys, connection secrets, or populated local settings.

## Commands
- Build: `dotnet build AccessDesk.slnx`
- Tests: `dotnet test AccessDesk.slnx`
- API (HTTP): `dotnet run --project src/AccessDesk.Api --launch-profile http` (port 5201; see `Properties/launchSettings.json`).
- Frontend: `cd frontend; npm ci; npm run dev` (Vite port 5173; proxy target is in `vite.config.ts`).
- Frontend production build: `cd frontend; npm run build`.
- Migration application: API startup applies migrations with `Database.MigrateAsync()` for relational providers; seeding runs separately and only in Development. Alternatively use `dotnet ef database update --project src/AccessDesk.Infrastructure --startup-project src/AccessDesk.Api` (requires EF CLI and configured connection).
- SQL Server connection is `ConnectionStrings:DefaultConnection`; configure with user secrets or environment variable `ConnectionStrings__DefaultConnection`.
- Before every commit, run `dotnet build AccessDesk.slnx` and `dotnet test AccessDesk.slnx`; also run `npm run build` for frontend changes.

## Working agreement
- Make small, focused commits with clear messages. Do not commit generated secrets or local `appsettings.Development.json`.
- Update `HANDOFF.md` at the end of every work session, including verification and known blockers.
- Keep docs factual to code and verified behavior; distinguish in-memory tests from real SQL Server verification.
