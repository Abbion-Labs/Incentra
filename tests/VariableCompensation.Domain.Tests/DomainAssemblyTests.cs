namespace VariableCompensation.Domain.Tests;

public class DomainAssemblyTests
{
    [Fact]
    public void DomainAssembly_Loads()
    {
        var assembly = typeof(VariableCompensation.Domain.ErrorCodes).Assembly;
        Assert.NotNull(assembly);
    }
}
