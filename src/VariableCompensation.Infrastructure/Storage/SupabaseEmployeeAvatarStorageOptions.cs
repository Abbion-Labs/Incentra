namespace VariableCompensation.Infrastructure.Storage;

public sealed class SupabaseEmployeeAvatarStorageOptions
{
    public const string SectionName = "Storage:Supabase";

    public string Url { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string Bucket { get; set; } = "employee-avatars";
}
