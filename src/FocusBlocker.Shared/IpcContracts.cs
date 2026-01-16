namespace FocusBlocker.Shared;

public enum IpcCommandType
{
    GetStatus,
    Unblock,
    Reblock,
    ReloadConfig
}

public sealed record IpcRequest(IpcCommandType Command, int DurationMinutes);

public sealed record IpcResponse(bool Success, string Message, ServiceStatus Status);

public sealed record ServiceStatus(
    bool IsBlocking,
    DateTimeOffset? UnblockUntil,
    int RemainingSeconds,
    bool OneHourAvailable,
    bool TwoHoursAvailable,
    bool ServiceOnline);
