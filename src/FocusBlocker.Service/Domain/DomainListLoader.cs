using FocusBlocker.Service.Storage;
using FocusBlocker.Shared;

namespace FocusBlocker.Service;

public sealed class DomainListLoader(ConfigStore configStore)
{
    public IReadOnlyList<string> LoadDomains()
    {
        var domainList = configStore.LoadDomains();
        return domainList.Groups.SelectMany(group => group.Domains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
