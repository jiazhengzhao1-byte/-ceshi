using System.Diagnostics;
using System.Text;

namespace FocusBlocker.Service.Engine;

public sealed class FirewallRuleManager
{
    private const string RuleGroup = "FocusBlocker";

    public async Task ClearRulesAsync(CancellationToken cancellationToken)
    {
        var command = "Remove-NetFirewallRule -Group \"" + RuleGroup + "\" -ErrorAction SilentlyContinue";
        await RunPowerShellAsync(command, cancellationToken);
    }

    public async Task AddBlockRulesAsync(IReadOnlyList<string> domains, CancellationToken cancellationToken)
    {
        if (domains.Count == 0)
        {
            return;
        }

        var fqdnList = string.Join(",", domains.Select(domain => $"'{domain}'"));
        var command = new StringBuilder();
        command.Append("New-NetFirewallRule -DisplayName \"FocusBlocker Block\" ");
        command.Append("-Group \"").Append(RuleGroup).Append("\" ");
        command.Append("-Direction Outbound -Action Block -Enabled True ");
        command.Append("-RemoteFqdn @(").Append(fqdnList).Append(")");

        await RunPowerShellAsync(command.ToString(), cancellationToken);
    }

    private static async Task RunPowerShellAsync(string command, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -Command {command}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync(cancellationToken);
    }
}
