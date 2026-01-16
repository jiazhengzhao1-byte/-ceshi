using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using FocusBlocker.Shared;

namespace FocusBlocker.UI;

public sealed class IpcClient
{
    private const string PipeName = "FocusBlockerPipe";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public async Task<IpcResponse> SendAsync(IpcRequest request, CancellationToken cancellationToken)
    {
        using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(1000, cancellationToken);

        await using var writer = new StreamWriter(client, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
        var payload = JsonSerializer.Serialize(request, JsonOptions);
        await writer.WriteLineAsync(payload.AsMemory(), cancellationToken);

        using var reader = new StreamReader(client, Encoding.UTF8, leaveOpen: true);
        var responsePayload = await reader.ReadLineAsync(cancellationToken) ?? string.Empty;
        return JsonSerializer.Deserialize<IpcResponse>(responsePayload, JsonOptions)
            ?? new IpcResponse(false, "empty", new ServiceStatus(true, null, 0, true, true, false));
    }
}
