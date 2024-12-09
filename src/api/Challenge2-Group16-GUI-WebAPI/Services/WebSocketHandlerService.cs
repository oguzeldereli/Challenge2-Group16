using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class WebSocketHandlerService
    {
        private readonly ILogger<WebSocketHandlerService> _logger;
        private readonly WebSocketManagerService _webSocketManagerService;
        private readonly PacketHandlingService _packetHandlingService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public WebSocketHandlerService(
            ILogger<WebSocketHandlerService> logger,
            WebSocketManagerService webSocketManagerService,
            PacketHandlingService packetHandlingService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _webSocketManagerService = webSocketManagerService;
            _packetHandlingService = packetHandlingService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        private async Task<(WebSocketMessageType, byte[])> ReceiveAsync(WebSocket socket)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);
            WebSocketReceiveResult result = null;

            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);

                    if (result.CloseStatus != null)
                    {
                        return (WebSocketMessageType.Close, new byte[0]);
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return (WebSocketMessageType.Close, new byte[0]);
                    }

                    ms.Write(buffer.Array, buffer.Offset, result.Count);

                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                return (result.MessageType, ms.ToArray());
            }
        }

        private DateTime LastPongTime { get; set; } = DateTime.UtcNow;
        public async Task HandleAsync(WebSocket socket, string socketId)
        {
            try
            {
                WebSocketMessageType type;
                while (socket.State == WebSocketState.Open)
                {
                    (type, var message) = await ReceiveAsync(socket);
                    if (type == WebSocketMessageType.Close)
                    {
                        return; // terminate connection
                    }
                    
                    if (type == WebSocketMessageType.Text && message.SequenceEqual(Encoding.UTF8.GetBytes("PONG")))
                    {
                        LastPongTime = DateTime.UtcNow;
                        // Console.WriteLine("pong");
                    }
                    else if (type == WebSocketMessageType.Text && message.SequenceEqual(Encoding.UTF8.GetBytes("PING")))
                    {
                        var buffer = Encoding.UTF8.GetBytes("PONG");
                        await socket.SendAsync(
                            new ArraySegment<byte>(buffer),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }
                    else
                    {

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using (var scope = _serviceScopeFactory.CreateScope())
                                {
                                    var packetHandlingService = scope.ServiceProvider.GetRequiredService<PacketHandlingService>();
                                    var packet = DataPacketModel.Create(message);
                                    if (packet == null)
                                    {
                                        await packetHandlingService.MalformedPacketResponse(socketId);
                                    }
                                    else
                                    {
                                        await packetHandlingService.ParseAndHandlePacketAsync(socketId, packet);
                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error while processing packet: {ex.Message}");
                                Console.WriteLine($"Socket: {socketId}, Bound user: {(await _webSocketManagerService.GetBoundClient(socketId))?.Id ?? ""}");
                                await _webSocketManagerService.RemoveSocketAsync(socketId);
                                return;
                            }
                        });
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Fatal error in WebSocket handler for socket {socketId}: {ex.Message}");
                await _webSocketManagerService.RemoveSocketAsync(socketId);
                return;
            }
        }

        private const int TimeoutThreshold = 10000; // 5 seconds
        private const int PingInterval = 5000; // 5 second
        public async Task SendPing(WebSocket webSocket)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var currentTime = DateTime.UtcNow;

                    if ((currentTime - LastPongTime).TotalMilliseconds > TimeoutThreshold)
                    {
                        Console.WriteLine("Connection timeout. Closing WebSocket.");
                        return;
                    }

                    // Send a ping message
                    var buffer = Encoding.UTF8.GetBytes("PING");
                    // Console.WriteLine("ping");
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );

                    await Task.Delay(PingInterval);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending ping: {ex.Message}");
                    break;
                }
            }
        }

        private byte[] CombineByteArrays(params byte[][] arrays)
        {
            byte[] combined = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, combined, offset, array.Length);
                offset += array.Length;
                
            }
            return combined;
        }
    }
}
