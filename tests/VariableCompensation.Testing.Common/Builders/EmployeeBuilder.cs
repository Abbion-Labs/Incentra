using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Testing.Common.Builders;

public sealed class EmployeeBuilder
{
    private long id = 1;
    private string firstName = "Marko";
    private string lastName = "Marković";
    private long organizationUnitId = 1;
    private long jobPositionId = 1;
    private long? evaluatorEmployeeId = 2;
    private bool isActive = true;

    public EmployeeBuilder WithId(long value)
    {
        this.id = value;
        return this;
    }

    public EmployeeBuilder WithName(string first, string last)
    {
        this.firstName = first;
        this.lastName = last;
        return this;
    }

    public EmployeeBuilder WithOrganizationUnit(long value)
    {
        this.organizationUnitId = value;
        return this;
    }

    public EmployeeBuilder WithJobPosition(long value)
    {
        this.jobPositionId = value;
        return this;
    }

    public EmployeeBuilder WithEvaluator(long? value)
    {
        this.evaluatorEmployeeId = value;
        return this;
    }

    public Employee Build() => new()
    {
        Id = this.id,
        FirstName = this.firstName,
        LastName = this.lastName,
        OrganizationUnitId = this.organizationUnitId,
        JobPositionId = this.jobPositionId,
        EvaluatorEmployeeId = this.evaluatorEmployeeId,
        IsActive = this.isActive,
    };
}
