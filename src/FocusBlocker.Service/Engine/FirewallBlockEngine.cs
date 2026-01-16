using Microsoft.Extensions.Logging;

namespace FocusBlocker.Service.Engine;

public sealed class FirewallBlockEngine(
    DomainListLoader domainListLoader,
    FirewallRuleManager ruleManager,
    ILogger<FirewallBlockEngine> logger) : IBlockEngine
{
    public async Task ApplyBlockedAsync(CancellationToken cancellationToken)
    {
        var domains = domainListLoader.LoadDomains();
        logger.LogInformation("Applying firewall block rules for {DomainCount} domains.", domains.Count);
        await ruleManager.ClearRulesAsync(cancellationToken);
        await ruleManager.AddBlockRulesAsync(domains, cancellationToken);
    }

    public async Task ApplyUnblockedAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Removing firewall block rules.");
        await ruleManager.ClearRulesAsync(cancellationToken);
    }
}
