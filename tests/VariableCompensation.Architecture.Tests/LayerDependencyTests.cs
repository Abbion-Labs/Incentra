using FluentAssertions;
using NetArchTest.Rules;

namespace VariableCompensation.Architecture.Tests;

[Trait("Category", "Architecture")]
public class LayerDependencyTests
{
    private const string DomainNamespace = "VariableCompensation.Domain";
    private const string ApplicationNamespace = "VariableCompensation.Application";
    private const string InfrastructureNamespace = "VariableCompensation.Infrastructure";
    private const string ApiNamespace = "VariableCompensation.Api";

    [Fact]
    public void Domain_ShouldNotReference_ApplicationOrInfrastructureOrApi()
    {
        var result = Types.InAssembly(typeof(Domain.ErrorCodes).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_ShouldNotReference_InfrastructureOrApi()
    {
        var result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Api_Controllers_ShouldNotReference_AppDbContext()
    {
        var result = Types.InAssembly(typeof(Api.DependencyInjection).Assembly)
            .That()
            .ResideInNamespace($"{ApiNamespace}.Controllers")
            .ShouldNot()
            .HaveDependencyOn("VariableCompensation.Infrastructure.Persistence.AppDbContext")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_CommandHandlers_ShouldBeSealed()
    {
        var result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
