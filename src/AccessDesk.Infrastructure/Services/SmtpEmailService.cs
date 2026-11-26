using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AccessDesk.Domain.Interfaces;

namespace AccessDesk.Infrastructure.Services;

/// <summary>
/// Sends emails using MailKit. In development this talks to MailHog (a local
/// fake SMTP server that catches all emails so they don't go to real inboxes).
/// In production, swap the SmtpSettings values to point to a real SMTP provider.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, MailKit.Security.SecureSocketOptions.None, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            // Log the error but don't crash — email failure should not block the main workflow
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}

/// <summary>
/// Strongly-typed configuration class for SMTP settings.
/// Values come from appsettings.json or environment variables.
/// No secrets in source control — use dotnet user-secrets for local dev.
/// </summary>
public class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025; // MailHog default
    public string SenderEmail { get; set; } = "noreply@accessdesk.dev";
    public string SenderName { get; set; } = "AccessDesk";
}
