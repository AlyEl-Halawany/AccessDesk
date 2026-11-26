namespace AccessDesk.Domain.Interfaces;

/// <summary>
/// Contract for sending emails. The Infrastructure layer provides an implementation
/// that talks to MailHog in development (or a real SMTP server in production).
/// Having an interface here means Application layer tests can use a fake/mock.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default);
}
