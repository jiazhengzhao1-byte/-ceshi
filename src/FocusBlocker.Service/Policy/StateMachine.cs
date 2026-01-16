using FocusBlocker.Service.Engine;
using FocusBlocker.Service.Storage;
using FocusBlocker.Shared;
using Microsoft.Extensions.Logging;

namespace FocusBlocker.Service.Policy;

public sealed class StateMachine(
    ConfigStore configStore,
    QuotaTracker quotaTracker,
    IBlockEngine blockEngine,
    ITimeProvider timeProvider,
    ILogger<StateMachine> logger)
{
    private readonly object _lock = new();

    public PolicyState CurrentState => quotaTracker.State;

    public ServiceStatus GetStatus()
    {
        var state = quotaTracker.State;
        var remaining = state.UnblockUntil is null
            ? 0
            : Math.Max(0, (int)(state.UnblockUntil.Value - timeProvider.Now).TotalSeconds);

        return new ServiceStatus(
            state.IsBlocking,
            state.UnblockUntil,
            remaining,
            quotaTracker.CanUseOneHour(),
            quotaTracker.CanUseTwoHours(),
            true);
    }

    public async Task<ServiceStatus> RequestUnblockAsync(int minutes, CancellationToken cancellationToken)
    {
        if (minutes <= 0)
        {
            return GetStatus();
        }

        var settings = configStore.LoadAppSettings().Policy;
        quotaTracker.ResetIfNeeded();

        lock (_lock)
        {
            var state = quotaTracker.State;
            var now = timeProvider.Now;
            var target = now.AddMinutes(minutes);
            var isLongDuration = minutes >= 60;

            if (isLongDuration)
            {
                if (minutes >= 120 && !quotaTracker.CanUseTwoHours())
                {
                    return GetStatus();
                }

                if (minutes >= 60 && minutes < 120 && !quotaTracker.CanUseOneHour())
                {
                    return GetStatus();
                }
            }

            DateTimeOffset unblockUntil = target;
            if (state.UnblockUntil is not null && settings.StackShortDurations && minutes < 60)
            {
                var remaining = state.UnblockUntil.Value - now;
                var newDuration = Math.Min(settings.ShortDurationCapMinutes, (int)remaining.TotalMinutes + minutes);
                unblockUntil = now.AddMinutes(newDuration);
            }

            if (minutes >= 120)
            {
                quotaTracker.ConsumeTwoHours();
            }
            else if (minutes >= 60)
            {
                quotaTracker.ConsumeOneHour();
            }

            var newState = state with { IsBlocking = false, UnblockUntil = unblockUntil };
            quotaTracker.UpdateState(newState);
        }

        await blockEngine.ApplyUnblockedAsync(cancellationToken);
        logger.LogInformation("Unblocked for {Minutes} minutes.", minutes);
        return GetStatus();
    }

    public async Task<ServiceStatus> RequestReblockAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var state = quotaTracker.State;
            var newState = state with { IsBlocking = true, UnblockUntil = null };
            quotaTracker.UpdateState(newState);
        }

        await blockEngine.ApplyBlockedAsync(cancellationToken);
        logger.LogInformation("Reblocked by user request.");
        return GetStatus();
    }

    public async Task<ServiceStatus> ReloadConfigAsync(CancellationToken cancellationToken)
    {
        var state = quotaTracker.State;
        if (state.IsBlocking)
        {
            await blockEngine.ApplyBlockedAsync(cancellationToken);
            logger.LogInformation("Reloaded configuration and refreshed block rules.");
        }

        return GetStatus();
    }

    public async Task TickAsync(CancellationToken cancellationToken)
    {
        quotaTracker.ResetIfNeeded();
        var state = quotaTracker.State;
        if (state.IsBlocking || state.UnblockUntil is null)
        {
            return;
        }

        if (timeProvider.Now >= state.UnblockUntil.Value)
        {
            lock (_lock)
            {
                var updated = quotaTracker.State with { IsBlocking = true, UnblockUntil = null };
                quotaTracker.UpdateState(updated);
            }

            await blockEngine.ApplyBlockedAsync(cancellationToken);
            logger.LogInformation("Unblock period ended, reblocking.");
        }
    }
}
