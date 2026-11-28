# Known Issues

1. **MailHog is not running in the local environment:**
   - SQL Server workflows succeed, but review campaign email dispatch logs `SocketException (10061)` when `localhost:1025` has no SMTP server. Email failure is caught and does not roll back the campaign. Start MailHog with Docker Compose when Docker is available, or configure `Smtp` for a reachable test server.

2. **Screenshots are not yet captured:**
   - README still contains screenshot placeholders. Genuine browser screenshots are pending Playwright/UI verification.

3. **Docker Compose has not been run:**
   - Docker is unavailable in this environment. SQL Server Express is installed locally and has been verified independently.

## Verified against SQL Server Express 2025 (2026-11-28)

- `AccessDeskDb` migrations applied successfully to SQL Server 2025 Express (`LAPTOP-8MVO6LG8\SQLEXPRESS`). The second migration deletes old development refresh-token rows because those records were stored raw.
- API startup, role/user/application/SoD/sample-request seeding, and demo-account login succeeded using the Development profile.
- Live API flows passed: request creation, manager approval, owner approval, blocking SoD rule, audit search, review campaign creation, and JML joiner/mover/leaver.
- The expiry worker was triggered by marking a test grant expired in SQL Server and restarting the API; it transitioned the request to `Expired` and wrote an `AccessRequest.Expired` audit entry.
- Hashed refresh-token persistence, rotation/reuse revocation, `/health`, security headers, login rate limiting, and bounded request pagination were confirmed against the live API.
- The opt-in SQL Server concurrency test passed with `ACCESSDESK_SQLSERVER_CONNECTION` configured. Default test runs skip it.
- `dotnet list AccessDesk.slnx package --vulnerable --include-transitive` reported no vulnerable packages after dependency updates.
