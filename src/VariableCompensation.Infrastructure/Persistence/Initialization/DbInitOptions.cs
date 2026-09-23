namespace VariableCompensation.Infrastructure.Persistence.Initialization;

public sealed class DbInitOptions
{
    public const string SectionName = "DbInit";
    public const string OnStartPolicy = "OnStart";
    public const string OnDeployPolicy = "OnDeploy";

    public string Policy { get; init; } = OnStartPolicy;
    public bool Seed { get; init; } = true;
}
