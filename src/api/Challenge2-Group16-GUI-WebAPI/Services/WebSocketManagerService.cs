using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using Challenge2_Group16_GUI_WebAPI.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

public class WebSocketManagerService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ConcurrentDictionary<string, (WebSocket, string?)> _sockets = new();
    private readonly SseClientService _sseClientService;

    public WebSocketManagerService(
        SseClientService sseClientService,
        IServiceScopeFactory scopeFactory)
    {
        _sseClientService = sseClientService;
        this.scopeFactory = scopeFactory;
    }

    public void AddSocket(string id, WebSocket socket)
    {
        _sockets.TryAdd(id, (socket, null));
    }

    public async Task RemoveSocketAsync(string id)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (_sockets.TryRemove(id, out var pair))
            {
                var (socket, clientId) = pair;
                Console.WriteLine($"Removed socket {id} with client {clientId ?? "N/A"}");
                if (clientId != null)
                {
                    var client = _context.Clients.FirstOrDefault(x => x.Id == clientId);
                    if (client != null)
                    {
                        await _sseClientService.PublishAsJsonAsync("device", new
                        {
                            client_id = BitConverter.ToString(client.Identifier).Replace("-", "").ToLowerInvariant(),
                            action = "remove"
                        });
                    }
                }

                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed by the server",
                        CancellationToken.None
                    );
                }
                else if (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted)
                {
                    socket.Abort(); 
                    Console.WriteLine("WebSocket aborted due to an invalid state.");
                }
            }
        }   
    }

    public async Task BindClient(string socketId, RegisteredClient client)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();


            if (!_context.Clients.Any(x => x.Id == client.Id))
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

            var existingConnections = _sockets.Where(x => x.Value.Item2 == client.Id);
            foreach (var connection in existingConnections)
            {
                await RemoveSocketAsync(connection.Key);
            }

            await _sseClientService.PublishAsJsonAsync("device", new
            {
                client_id = BitConverter.ToString(client.Identifier).Replace("-", "").ToLowerInvariant(),
                action = "add"
            });
            Console.WriteLine($"Bound socket {socketId} to client {client.Id}");
            _sockets[key] = (socket, client.Id);
        }
    }

    public async Task UnbindClient(RegisteredClient client)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!_context.Clients.Any(x => x.Id == client.Id))
            {
                return;
            }

            var key = _sockets.Keys.FirstOrDefault(x => _sockets[x].Item2 == client.Id);
            if (key == null)
            {
                return;
            }

            await _sseClientService.PublishAsJsonAsync("device", new
            {
                client_id = BitConverter.ToString(client.Identifier).Replace("-", "").ToLowerInvariant(),
                action = "remove"
            });

            Console.WriteLine("Unbinded client");
            _sockets[key] = (_sockets[key].Item1, null);
        }
    }

    public bool IsClientBound(WebSocket socket, RegisteredClient client)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!_context.Clients.Any(x => x.Id == client.Id))
            {
                return false;
            }

            var key = _sockets.Keys.FirstOrDefault(x => _sockets[x].Item1 == socket);
            if (key == null)
            {
                return false;
            }

            return _sockets[key].Item2 == client.Id;
        }
    }

    public bool IsClientBound(string socketId, RegisteredClient client)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!_context.Clients.Any(x => x.Id == client.Id))
            {
                return false;
            }

            var key = _sockets.Keys.FirstOrDefault(x => x == socketId);
            if (key == null)
            {
                return false;
            }

            return _sockets[key].Item2 == client.Id;
        }
    }

    public RegisteredClient? GetBoundClient(string id)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var clientId = _sockets.TryGetValue(id, out var pair) ? pair.Item2 : null;
            if(id == null)
            {
                return null;
            }

            var client = _context.Clients.FirstOrDefault(x => x.Id == clientId);
            return client;
        }
    }

    public string? GetBoundSocket(RegisteredClient client)
    {
        return _sockets.FirstOrDefault(x => x.Value.Item2 == client.Id).Key;
    }

    public List<RegisteredClient> GetAllBoundClients()
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var clientIds = _sockets.Values.Select(x => x.Item2).Where(x => x != null);
            if (clientIds == null)
            {
                return new List<RegisteredClient>();
            }

            var clients = _context.Clients.Where(x => clientIds.Contains(x.Id)).ToList();
            return clients;
        }
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
