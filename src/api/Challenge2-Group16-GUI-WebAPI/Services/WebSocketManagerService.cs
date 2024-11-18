using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using Microsoft.AspNetCore.DataProtection;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

public class WebSocketManagerService
{
    private readonly ApplicationDbContext _context;
    private readonly ConcurrentDictionary<string, (WebSocket, RegisteredClient?)> _sockets = new();

    public WebSocketManagerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public void AddSocket(string id, WebSocket socket)
    {
        _sockets.TryAdd(id, (socket, null));
    }

    public async Task RemoveSocketAsync(string id)
    {
        if (_sockets.TryRemove(id, out var pair))
        {
            var (socket, _) = pair;
            await socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Connection closed by the server",
                CancellationToken.None
            );
        }
    }

    public void BindClient(string socketId, RegisteredClient client)
    {
        if (!_context.Users.Any(x => x.Id == client.Id))
        {
            return;
        }

        var key = socketId;
        if (!_sockets.Any(x => x.Key == key))
        {
            return;
        }

        var socket = _sockets[key].Item1;
        if (socket == null || socket.State != WebSocketState.Open)
        {
            return;
        }

        _sockets[key] = (socket, client);
    }

    public void BindClient(WebSocket socket, RegisteredClient client)
    {
        if(!_context.Users.Any(x => x.Id == client.Id))
        {
            return;
        }

        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var key = _sockets.Keys.FirstOrDefault(x => _sockets[x].Item1 == socket);
        if(key == null)
        {
            return;
        }

        _sockets[key] = (socket, client);
    }

    public void UnbindClient(RegisteredClient client)
    {
        if (!_context.Users.Any(x => x.Id == client.Id))
        {
            return;
        }

        var key = _sockets.Keys.FirstOrDefault(x => _sockets[x].Item2 == client);
        if (key == null)
        {
            return;
        }

        _sockets[key] = (_sockets[key].Item1, null);
    }

    public bool IsClientBound(WebSocket socket, RegisteredClient client)
    {
        if (!_context.Users.Any(x => x.Id == client.Id))
        {
            return false;
        }

        var key = _sockets.Keys.FirstOrDefault(x => _sockets[x].Item1 == socket);
        if (key == null)
        {
            return false;
        }

        return _sockets[key].Item2 == client;
    }

    public bool IsClientBound(string socketId, RegisteredClient client)
    {
        if (!_context.Users.Any(x => x.Id == client.Id))
        {
            return false;
        }

        var key = _sockets.Keys.FirstOrDefault(x => x == socketId);
        if (key == null)
        {
            return false;
        }

        return _sockets[key].Item2 == client;
    }

    public RegisteredClient? GetBoundClient(string id)
    {
        return _sockets.TryGetValue(id, out var pair) ? pair.Item2 : null;
    }

    public List<RegisteredClient> GetAllBoundClients()
    {
        return _sockets.Values.Select(x => x.Item2).Where(x => x != null).ToList();
    }

    public async Task SendMessageAsync(byte[] message)
    {
        var tasks = _sockets.Values.Select(async pair =>
        {
            var (socket, _) = pair;
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
        if (_sockets.TryGetValue(id, out var pair))
        {
            var (socket, _) = pair;
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
