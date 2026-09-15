namespace VariableCompensation.Application.Abstractions.Notifications;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken);
}
