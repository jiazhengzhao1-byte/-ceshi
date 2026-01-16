using FocusBlocker.Service.Storage;
using FocusBlocker.Shared;

namespace FocusBlocker.Service.Policy;

public sealed class QuotaTracker(StateStore stateStore, ITimeProvider timeProvider)
{
    private PolicyState _state = stateStore.LoadState(timeProvider);

    public PolicyState State => _state;

    public void ResetIfNeeded()
    {
        if (_state.QuotaDate == timeProvider.Today)
        {
            return;
        }

        _state = _state with { QuotaDate = timeProvider.Today, OneHourUsed = 0, TwoHoursUsed = 0 };
        stateStore.SaveState(_state);
    }

    public bool CanUseOneHour() => _state.OneHourUsed < 1;

    public bool CanUseTwoHours() => _state.TwoHoursUsed < 1;

    public void ConsumeOneHour()
    {
        _state = _state with { OneHourUsed = _state.OneHourUsed + 1 };
        stateStore.SaveState(_state);
    }

    public void ConsumeTwoHours()
    {
        _state = _state with { TwoHoursUsed = _state.TwoHoursUsed + 1 };
        stateStore.SaveState(_state);
    }

    public void UpdateState(PolicyState state)
    {
        _state = state;
        stateStore.SaveState(_state);
    }
}
