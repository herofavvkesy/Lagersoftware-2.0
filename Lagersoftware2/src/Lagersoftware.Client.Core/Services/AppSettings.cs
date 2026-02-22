namespace Lagersoftware.Client.Core.Services;

public class AppSettings
{
    public string ServerUrl { get; set; } = "http://localhost:5000";
    public int SyncIntervalMinutes { get; set; } = 5;
    public bool AutoSyncEnabled { get; set; } = true;
    public string? LastUsername { get; set; }
    public string? AuthToken { get; set; }
    public string? UserRole { get; set; }
    public int? UserId { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
}
