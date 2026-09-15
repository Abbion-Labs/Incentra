namespace VariableCompensation.Application.Notifications;

public sealed class EmailNotificationSettings
{
    public const string SectionName = "Email";

    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
