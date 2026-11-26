# Known Issues

1. **Local SQL Server / Docker not available in build environment:**
   - SQL Server LocalDB, SQL Server Express, and Docker Desktop were not installed on the local build machine, so `dotnet ef database update` and `docker compose up` were not executed against a live SQL Server container here.
   - Mitigation: EF Core migrations (`InitialCreate`) were generated and verified, and all API workflows were verified end-to-end using `WebApplicationFactory` with EF Core InMemory (`AccessDesk.IntegrationTests`).

2. **MailKit / MimeKit NuGet Advisory Warnings (`NU1902` / `NU1903`):**
   - Building the solution emits NuGet vulnerability warnings for `MailKit 4.11.0`, `MimeKit 4.11.0`, and transitive `System.Security.Cryptography.Pkcs 8.0.1`. These are used only for SMTP email dispatch in `SmtpEmailService`.
