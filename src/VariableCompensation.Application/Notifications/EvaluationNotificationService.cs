using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VariableCompensation.Application.Abstractions.Notifications;
using VariableCompensation.Application.Abstractions.Persistence;

namespace VariableCompensation.Application.Notifications;

public sealed class EvaluationNotificationService : IEvaluationNotificationService
{
    private readonly IEvaluationRepository evaluationRepository;
    private readonly IUserRepository userRepository;
    private readonly IEmailSender emailSender;
    private readonly EmailNotificationSettings emailSettings;
    private readonly ILogger<EvaluationNotificationService> logger;

    public EvaluationNotificationService(
        IEvaluationRepository evaluationRepository,
        IUserRepository userRepository,
        IEmailSender emailSender,
        IOptions<EmailNotificationSettings> emailSettings,
        ILogger<EvaluationNotificationService> logger)
    {
        this.evaluationRepository = evaluationRepository;
        this.userRepository = userRepository;
        this.emailSender = emailSender;
        this.emailSettings = emailSettings.Value;
        this.logger = logger;
    }

    public Task NotifySubmittedForReviewAsync(long evaluationId, CancellationToken cancellationToken) =>
        this.SendAsync(
            evaluationId,
            recipientEmployeeIdSelector: e => e.ControllerEmployeeId,
            subject: "Nova ocena na pregled",
            bodyBuilder: (evaluation, link) =>
                $"""
                <p>Zdravo,</p>
                <p>Ocenjivač je predao ocenu zaposlenog <strong>{evaluation.Employee.FullName}</strong> za period Q{evaluation.Quarter}/{evaluation.Year} na pregled.</p>
                <p><a href="{link}">Otvorite ocenu u aplikaciji</a></p>
                """,
            linkPath: id => $"/controller/evaluations/{id}",
            cancellationToken);

    public Task NotifyApprovedAsync(long evaluationId, CancellationToken cancellationToken) =>
        this.SendAsync(
            evaluationId,
            recipientEmployeeIdSelector: e => e.EvaluatorEmployeeId,
            subject: "Ocena je odobrena",
            bodyBuilder: (evaluation, link) =>
                $"""
                <p>Zdravo,</p>
                <p>Kontrolor je odobrio vašu ocenu zaposlenog <strong>{evaluation.Employee.FullName}</strong> za period Q{evaluation.Quarter}/{evaluation.Year}.</p>
                <p><a href="{link}">Otvorite ocenu u aplikaciji</a></p>
                """,
            linkPath: id => $"/evaluator/evaluations/{id}",
            cancellationToken);

    public Task NotifyReturnedForRevisionAsync(
        long evaluationId,
        string revisionComment,
        CancellationToken cancellationToken) =>
        this.SendAsync(
            evaluationId,
            recipientEmployeeIdSelector: e => e.EvaluatorEmployeeId,
            subject: "Ocena vraćena na doradu",
            bodyBuilder: (evaluation, link) =>
                $"""
                <p>Zdravo,</p>
                <p>Kontrolor je vratio ocenu zaposlenog <strong>{evaluation.Employee.FullName}</strong> za period Q{evaluation.Quarter}/{evaluation.Year} na doradu.</p>
                <p><strong>Komentar kontrolora:</strong></p>
                <p>{System.Net.WebUtility.HtmlEncode(revisionComment)}</p>
                <p><a href="{link}">Otvorite ocenu u aplikaciji</a></p>
                """,
            linkPath: id => $"/evaluator/evaluations/{id}",
            cancellationToken);

    private async Task SendAsync(
        long evaluationId,
        Func<Domain.Entities.Evaluation.Evaluation, long?> recipientEmployeeIdSelector,
        string subject,
        Func<Domain.Entities.Evaluation.Evaluation, string, string> bodyBuilder,
        Func<long, string> linkPath,
        CancellationToken cancellationToken)
    {
        try
        {
            var evaluation = await this.evaluationRepository.FindByIdAsync(evaluationId, cancellationToken);
            if (evaluation is null)
            {
                this.logger.LogWarning("Evaluation {EvaluationId} not found for email notification", evaluationId);
                return;
            }

            var recipientEmployeeId = recipientEmployeeIdSelector(evaluation);
            if (recipientEmployeeId is null)
            {
                this.logger.LogInformation(
                    "No recipient employee for evaluation {EvaluationId} notification",
                    evaluationId);
                return;
            }

            var user = await this.userRepository.FindByEmployeeIdAsync(recipientEmployeeId.Value, cancellationToken);
            if (user is null || !user.IsActive)
            {
                this.logger.LogInformation(
                    "No active user linked to employee {EmployeeId} for evaluation {EvaluationId} notification",
                    recipientEmployeeId.Value,
                    evaluationId);
                return;
            }

            if (!user.EmailNotificationsEnabled)
            {
                return;
            }

            var baseUrl = this.emailSettings.FrontendBaseUrl.TrimEnd('/');
            var link = $"{baseUrl}{linkPath(evaluationId)}";
            var body = bodyBuilder(evaluation, link);

            await this.emailSender.SendAsync(user.Email, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to send email notification for evaluation {EvaluationId}", evaluationId);
        }
    }
}
