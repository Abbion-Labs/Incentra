namespace VariableCompensation.Infrastructure.Notifications;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public bool UseSsl { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = "noreply@incentra.local";

    public string FromName { get; set; } = "Incentra";

    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
}
