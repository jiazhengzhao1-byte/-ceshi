namespace FocusBlocker.Service.Engine;

public interface IBlockEngine
{
    Task ApplyBlockedAsync(CancellationToken cancellationToken);
    Task ApplyUnblockedAsync(CancellationToken cancellationToken);
}
