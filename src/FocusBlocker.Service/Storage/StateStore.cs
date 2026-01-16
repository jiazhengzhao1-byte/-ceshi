using System.Text.Json;
using FocusBlocker.Shared;

namespace FocusBlocker.Service.Storage;

public sealed class StateStore(ConfigStore configStore)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private string StatePath => Path.Combine(configStore.BasePath, "state.json");

    public PolicyState LoadState(ITimeProvider timeProvider)
    {
        if (!File.Exists(StatePath))
        {
            return new PolicyState(true, null, timeProvider.Today, 0, 0);
        }

        var json = File.ReadAllText(StatePath);
        var state = JsonSerializer.Deserialize<PolicyState>(json, JsonOptions)
            ?? new PolicyState(true, null, timeProvider.Today, 0, 0);

        return state.QuotaDate == timeProvider.Today
            ? state
            : state with { QuotaDate = timeProvider.Today, OneHourUsed = 0, TwoHoursUsed = 0 };
    }

    public void SaveState(PolicyState state)
    {
        Directory.CreateDirectory(configStore.BasePath);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(StatePath, json);
    }
}
