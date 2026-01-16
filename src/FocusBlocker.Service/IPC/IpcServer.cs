using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using FocusBlocker.Service.Policy;
using FocusBlocker.Shared;
using Microsoft.Extensions.Logging;

namespace FocusBlocker.Service.IPC;

public sealed class IpcServer(StateMachine stateMachine, ILogger<IpcServer> logger)
{
    private const string PipeName = "FocusBlockerPipe";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listenTask = Task.Run(() => ListenAsync(_cts.Token), cancellationToken);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();
        if (_listenTask is not null)
        {
            await _listenTask.WaitAsync(cancellationToken);
        }
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var server = new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await server.WaitForConnectionAsync(cancellationToken);

            try
            {
                var request = await ReadRequestAsync(server, cancellationToken);
                var response = await HandleRequestAsync(request, cancellationToken);
                await WriteResponseAsync(server, response, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "IPC server error.");
            }
        }
    }

    private static async Task<IpcRequest> ReadRequestAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var payload = await reader.ReadLineAsync(cancellationToken) ?? "";
        return JsonSerializer.Deserialize<IpcRequest>(payload, JsonOptions) ?? new IpcRequest(IpcCommandType.GetStatus, 0);
    }

    private static async Task WriteResponseAsync(Stream stream, IpcResponse response, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(response, JsonOptions);
        await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync(payload.AsMemory(), cancellationToken);
    }

    private async Task<IpcResponse> HandleRequestAsync(IpcRequest request, CancellationToken cancellationToken)
    {
        return request.Command switch
        {
            IpcCommandType.GetStatus => new IpcResponse(true, "ok", stateMachine.GetStatus()),
            IpcCommandType.Unblock => new IpcResponse(true, "ok", await stateMachine.RequestUnblockAsync(request.DurationMinutes, cancellationToken)),
            IpcCommandType.Reblock => new IpcResponse(true, "ok", await stateMachine.RequestReblockAsync(cancellationToken)),
            IpcCommandType.ReloadConfig => new IpcResponse(true, "ok", await stateMachine.ReloadConfigAsync(cancellationToken)),
            _ => new IpcResponse(false, "unknown command", stateMachine.GetStatus())
        };
    }
}
