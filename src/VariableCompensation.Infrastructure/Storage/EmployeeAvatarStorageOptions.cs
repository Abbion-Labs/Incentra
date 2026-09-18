namespace VariableCompensation.Infrastructure.Storage;

public sealed class EmployeeAvatarStorageOptions
{
    public const string SectionName = "Storage";
    public const string LocalProvider = "Local";
    public const string SupabaseProvider = "Supabase";

    public string Provider { get; set; } = LocalProvider;
}
