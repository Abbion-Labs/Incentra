using VariableCompensation.Application.Abstractions.Notifications;

namespace VariableCompensation.Testing.Common.Fakes;

public sealed class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string HtmlBody)> Sent { get; } = [];

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        this.Sent.Add((to, subject, htmlBody));
        return Task.CompletedTask;
    }
}
