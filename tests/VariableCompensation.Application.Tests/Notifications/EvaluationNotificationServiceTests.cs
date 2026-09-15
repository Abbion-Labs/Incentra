using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VariableCompensation.Application.Notifications;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Testing.Common.Builders;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Notifications;

[Trait("Category", "Unit")]
public class EvaluationNotificationServiceTests
{
    [Fact]
    public async Task NotifySubmittedForReview_WhenNotificationsDisabled_DoesNotSendEmail()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithEvaluator(2)
            .WithController(3)
            .Build());

        var userRepository = new FakeUserRepository();
        userRepository.Users[10] = new User
        {
            Id = 10,
            Email = "controller@local.dev",
            Employee = new Employee { Id = 3, UserId = 10 },
        };

        var emailSender = new FakeEmailSender();
        var service = CreateService(repository, userRepository, emailSender);

        await service.NotifySubmittedForReviewAsync(1, CancellationToken.None);

        emailSender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task NotifySubmittedForReview_WhenNotificationsEnabled_SendsEmailToController()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithEvaluator(2)
            .WithController(3)
            .Build());

        var userRepository = new FakeUserRepository();
        var controllerUser = new User
        {
            Id = 10,
            Email = "controller@local.dev",
            Employee = new Employee { Id = 3, UserId = 10 },
        };
        controllerUser.SetEmailNotificationsEnabled(true);
        userRepository.Users[10] = controllerUser;

        var emailSender = new FakeEmailSender();
        var service = CreateService(repository, userRepository, emailSender);

        await service.NotifySubmittedForReviewAsync(1, CancellationToken.None);

        emailSender.Sent.Should().ContainSingle();
        emailSender.Sent[0].To.Should().Be("controller@local.dev");
        emailSender.Sent[0].Subject.Should().Be("Nova ocena na pregled");
    }

    [Fact]
    public async Task NotifyApproved_WhenNoLinkedUser_DoesNotSendEmail()
    {
        var repository = new FakeEvaluationRepository();
        repository.Seed(new EvaluationBuilder()
            .WithId(1)
            .WithEvaluator(2)
            .WithController(3)
            .Build());

        var emailSender = new FakeEmailSender();
        var service = CreateService(repository, new FakeUserRepository(), emailSender);

        await service.NotifyApprovedAsync(1, CancellationToken.None);

        emailSender.Sent.Should().BeEmpty();
    }

    private static EvaluationNotificationService CreateService(
        FakeEvaluationRepository evaluationRepository,
        FakeUserRepository userRepository,
        FakeEmailSender emailSender) =>
        new(
            evaluationRepository,
            userRepository,
            emailSender,
            Options.Create(new EmailNotificationSettings { FrontendBaseUrl = "http://localhost:5173" }),
            NullLogger<EvaluationNotificationService>.Instance);
}
