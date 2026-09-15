using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using VariableCompensation.Application.Abstractions.Notifications;

namespace VariableCompensation.Infrastructure.Notifications;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions options;
    private readonly ILogger<SmtpEmailSender> logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (!this.options.Enabled)
        {
            this.logger.LogInformation(
                "Email sending is disabled. Skipped message to {Recipient} with subject {Subject}",
                to,
                subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(this.options.FromName, this.options.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            this.options.Host,
            this.options.Port,
            this.options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(this.options.Username))
        {
            await client.AuthenticateAsync(this.options.Username, this.options.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        this.logger.LogInformation("Email sent to {Recipient} with subject {Subject}", to, subject);
    }
}
