using FocusBlocker.Service.Engine;
using FocusBlocker.Service.IPC;
using FocusBlocker.Service.Policy;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FocusBlocker.Service;

public sealed class ServiceHost(
    StateMachine stateMachine,
    IBlockEngine blockEngine,
    IpcServer ipcServer,
    ILogger<ServiceHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FocusBlocker service starting.");
        await blockEngine.ApplyBlockedAsync(stoppingToken);
        await ipcServer.StartAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await stateMachine.TickAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("FocusBlocker service stopping.");
        await ipcServer.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
