namespace FocusBlocker.Shared;

public sealed record PolicyState(
    bool IsBlocking,
    DateTimeOffset? UnblockUntil,
    DateOnly QuotaDate,
    int OneHourUsed,
    int TwoHoursUsed);

public interface ITimeProvider
{
    DateTimeOffset Now { get; }
    DateOnly Today { get; }
}

public sealed class SystemTimeProvider : ITimeProvider
{
    public DateTimeOffset Now => DateTimeOffset.Now;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}
