# Known Issues and Limitations

1. **MailHog is not running locally.** Review campaign email dispatch logs `SocketException (10061)` for `localhost:1025`. Campaign records persist; use Docker Compose or configure a reachable SMTP server to test delivery.
2. **Docker Compose is unverified.** Docker is not installed in this environment. SQL Server Express is installed and the API was verified against it independently.
3. **GitHub metadata and latest CI status need authentication.** `gh auth status` reports the active GitHub token is invalid. Authenticate with `gh auth login -h github.com`, then verify Actions on `main` and set the description/topics.
4. **Historical repository contains development-only Compose fallbacks.** The original `e04df7a` commit included a demo SQL SA password and demo JWT signing value. Current `docker-compose.yml` requires `MSSQL_SA_PASSWORD` and `JWT_SECRET` instead. These were public development placeholders, not production credentials; Git history was not rewritten.
5. **Access JWT revocation is delayed until expiry.** Refresh-token revocation and leaver processing stop future refreshes, but existing stateless access JWTs remain valid until their short expiration.
6. **Audit protection is application-level only.** API/repository expose search and EF rejects tracked update/delete operations, but a database administrator can still change rows. No hash chain or external immutable retention is implemented.
7. **Frontend bundle is large.** `npm run build` succeeds, with a Vite advisory that the minified JavaScript chunk exceeds 500 kB.
8. **Production/negative security coverage is incomplete.** Most integration tests use EF InMemory. SQL Server rowversion concurrency is opt-in and was run locally; Docker SQL tests and broad authorization attack cases are not in CI.

## Verified against SQL Server Express 2025 (2026-11-28)

- Migrations applied to `AccessDeskDb`; development seeding and demo login succeeded.
- Live API flows passed: request creation, manager approval, owner approval, blocking SoD rule, audit search, review campaign creation, and JML joiner/mover/leaver.
- Expiry worker transitioned an expired grant to `Expired` and appended an `AccessRequest.Expired` audit event.
- Hashed refresh-token persistence, rotation/reuse handling, `/health`, security headers, login rate limiting, bounded request pagination, and refresh rotation transaction were verified against the live API.
- The opt-in SQL Server concurrency test ran and passed using `ACCESSDESK_SQLSERVER_CONNECTION`.
- `dotnet list AccessDesk.slnx package --vulnerable --include-transitive` reported no vulnerable packages.
- Playwright screenshots were captured from the Development app and saved under `docs/screenshots/`.
- Git-history scan found no private-key markers, AWS access-key IDs, or GitHub tokens; it identified the development-only Compose defaults noted above. Current Compose requires local environment values.
