using FluentAssertions;
using VariableCompensation.Application.Auth.Commands.UpdateNotificationPreferences;
using VariableCompensation.Domain.Entities.Identity;
using VariableCompensation.Testing.Common.Fakes;

namespace VariableCompensation.Application.Tests.Auth;

[Trait("Category", "Unit")]
public class UpdateNotificationPreferencesCommandHandlerTests
{
    [Fact]
    public async Task Handle_EnablesEmailNotifications_ReturnsUpdatedProfile()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users[1] = new User { Id = 1, Email = "evaluator@local.dev", IsActive = true };

        var handler = new UpdateNotificationPreferencesCommandHandler(
            FakeCurrentUserService.AsUser(1),
            userRepository,
            new FakeEmployeeRepository());

        var result = await handler.Handle(new UpdateNotificationPreferencesCommand(true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EmailNotificationsEnabled.Should().BeTrue();
        userRepository.Users[1].EmailNotificationsEnabled.Should().BeTrue();
    }
}
