using FluentAssertions;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Domain.Tests;

[Trait("Category", "Unit")]
public class RoleCodesTests
{
    [Fact]
    public void SessionPriority_MatchesTheOrderTheFrontendOffers()
    {
        // HOME_ROUTE_PRIORITY in frontend/src/utils/homeNavigation.ts lists the
        // roles in this order; the two have to tell the same story.
        RoleCodes.SessionPriority.Should().Equal(
            RoleCodes.Evaluator,
            RoleCodes.Controller,
            RoleCodes.Employee,
            RoleCodes.Payroll,
            RoleCodes.Admin);
    }
}
