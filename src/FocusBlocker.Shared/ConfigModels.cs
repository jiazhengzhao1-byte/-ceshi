namespace FocusBlocker.Shared;

public sealed record AppSettings(PolicySettings Policy, LoggingSettings Logging, StorageSettings Storage)
{
    public static AppSettings Default => new(
        new PolicySettings("blocked", true, 120, new DailyQuotaSettings(1, 1)),
        new LoggingSettings(true, "info"),
        new StorageSettings("%ProgramData%/FocusBlocker"));
}

public sealed record PolicySettings(
    string DefaultMode,
    bool StackShortDurations,
    int ShortDurationCapMinutes,
    DailyQuotaSettings DailyQuota);

public sealed record DailyQuotaSettings(int OneHour, int TwoHours);

public sealed record LoggingSettings(bool Enabled, string Level);

public sealed record StorageSettings(string BasePath);

public sealed record DomainGroup(string Name, string Note, IReadOnlyList<string> Domains);

public sealed record DomainList(int Version, IReadOnlyList<DomainGroup> Groups);
