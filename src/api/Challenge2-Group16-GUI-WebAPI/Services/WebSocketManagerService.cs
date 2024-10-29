using Microsoft.AspNetCore.DataProtection;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

public class WebSocketManagerService
{
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

    public void AddSocket(string id, WebSocket socket)
    {
        _sockets.TryAdd(id, socket);
    }

    public async Task RemoveSocketAsync(string id)
    {
        if (_sockets.TryRemove(id, out var socket))
        {
            await socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Connection closed by the server",
                CancellationToken.None
            );
        }
    }

    public async Task SendMessageAsync(byte[] message)
    {
        var tasks = _sockets.Values.Select(async socket =>
        {
            if (socket.State != WebSocketState.Open)
            {
                return;
            }

            var offset = 0;
            var count = 4096;
            while (offset < message.Length)
            {
                if (offset + count > message.Length)
                {
                    count = message.Length - offset;
                }
                await socket.SendAsync(new ArraySegment<byte>(message, offset, count), WebSocketMessageType.Binary, offset + count >= message.Length, System.Threading.CancellationToken.None);
                offset += count;
            }
        });
        await Task.WhenAll(tasks);
    }

    public async Task SendAsync(string id, byte[] message)
    {
        if (_sockets.TryGetValue(id, out var socket))
        {
            if (socket.State != WebSocketState.Open)
            {
                return;
            }

            var offset = 0;
            var count = 4096;
            while (offset < message.Length)
            {
                if (offset + count > message.Length)
                {
                    count = message.Length - offset;
                }
                await socket.SendAsync(new ArraySegment<byte>(message, offset, count), WebSocketMessageType.Binary, offset + count >= message.Length, System.Threading.CancellationToken.None);
                offset += count;
            }
        }
    }

    public async Task SendAsync(WebSocket socket, byte[] message)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var offset = 0;
        var count = 4096;
        while (offset < message.Length)
        {
            if (offset + count > message.Length)
            {
                count = message.Length - offset;
            }
            await socket.SendAsync(new ArraySegment<byte>(message, offset, count), WebSocketMessageType.Binary, offset + count >= message.Length, System.Threading.CancellationToken.None);
            offset += count;
        }
    }
}
