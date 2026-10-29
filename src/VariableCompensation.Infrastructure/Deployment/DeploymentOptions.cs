namespace VariableCompensation.Infrastructure.Deployment;

public sealed class DeploymentOptions
{
    public const string SectionName = "Deployment";
    public const string VercelIdentityProvider = "Vercel";
    public const string PostgresIdentityStore = "Postgres";

    public string IdentityProvider { get; init; } = VercelIdentityProvider;
    public string IdentityStore { get; init; } = PostgresIdentityStore;
}
