using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using Microsoft.AspNetCore.Http;
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
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WebSocketHandlerService> _logger;
        private readonly PacketService _packetService;
        private readonly RegisteredClientService _registeredClientService;
        private readonly WebSocketManagerService _webSocketManagerService;
        private readonly PacketHandlingService _packetHandlingService;
        private readonly ChainService _chainService;


        public WebSocketHandlerService(
            ApplicationDbContext context,
            ILogger<WebSocketHandlerService> logger,
            PacketService packetService,
            RegisteredClientService registeredClientService,
            WebSocketManagerService webSocketManagerService,
            PacketHandlingService packetHandlingService,
            ChainService chainService)
        {
            _context = context;
            _logger = logger;
            _packetService = packetService;
            _registeredClientService = registeredClientService;
            _webSocketManagerService = webSocketManagerService;
            _packetHandlingService = packetHandlingService;
            _chainService = chainService;
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

        private async Task CloseAsync(WebSocket socket, WebSocketCloseStatus status, string closeReason)
        {
            if (socket.State != WebSocketState.Open)
            {
                return;
            }

            await socket.CloseAsync(status, closeReason, CancellationToken.None);
        }

        public async Task ParseRegisterPacketAsync(string socketId, DataPacketModel packet)
        {

            if (packet.AuthorizationToken.SequenceEqual(new byte[16]) &&
                    packet.PacketError == (uint)PacketError.None &&
                    packet.DataSize == 36 &&
                    packet.PacketData.Length == 36 &&
                    packet.PacketSignature.SequenceEqual(new byte[32]) &&
                    _packetService.ValidatePacket(packet))
            {
                byte[] clientId = packet.PacketData.Take(32).ToArray();
                ClientType clientType = (ClientType)BitConverter.ToUInt32(packet.PacketData.Skip(32).Take(4).ToArray(), 0);
                var registerResult = await _registeredClientService.RegisterClientAsync(clientId, clientType);
                if (registerResult == null)
                {
                    await _packetHandlingService.InternalErrorResponse(socketId);
                    return;
                }

                await _packetHandlingService.RegisterResponse(socketId, registerResult, registerResult.Secret, registerResult.SignatureKey);
                return;
            }

            await _packetHandlingService.InvalidPacketResponse(socketId);
            return;
        }

        public async Task ParseAuthPacketAsync(string socketId, DataPacketModel packet)
        {
            if (packet.AuthorizationToken.SequenceEqual(new byte[16]) &&
                    packet.PacketError == (uint)PacketError.None &&
                    packet.DataSize == 64 &&
                    packet.PacketData.Length == 64 &&
                    _packetService.ValidatePacket(packet))
            {
                byte[] clientId = packet.PacketData.Take(32).ToArray();
                byte[] clientSecret = packet.PacketData.Skip(32).Take(32).ToArray();

                var temporaryAuthToken = await _registeredClientService.AuthorizeClientAsync(socketId, clientId, clientSecret);
                if (temporaryAuthToken == null)
                {
                    await _packetHandlingService.InternalErrorResponse(socketId);
                    return;
                }

                var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken.SequenceEqual(temporaryAuthToken));
                if (registeredClient == null)
                {
                    await _packetHandlingService.InternalErrorResponse(socketId);
                    return;
                }

                await _packetHandlingService.AuthResponse(socketId, registeredClient, temporaryAuthToken);
                return;
            }

            await _packetHandlingService.InvalidPacketResponse(socketId);
            return;
        }

        public async Task ParseRevokeAuthPacketAsync(string socketId, DataPacketModel packet)
        {
            if (packet.PacketError == (uint)PacketError.None &&
                    packet.DataSize == 0 &&
                    packet.PacketData.Length == 0 &&
                    _packetService.ValidatePacket(packet))
            {
                var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken == packet.AuthorizationToken);
                if (registeredClient == null)
                {
                    await _packetHandlingService.InvalidPacketResponse(socketId);
                    return;
                }

                if (!_webSocketManagerService.IsClientBound(socketId, registeredClient))
                {
                    await _packetHandlingService.InternalErrorResponse(socketId);
                    return;
                }

                if (!await _registeredClientService.RevokeClientAsync(packet.AuthorizationToken))
                {
                    await _packetHandlingService.InternalErrorResponse(socketId);
                    return;
                }

                await _packetHandlingService.RevokeAuthResponse(socketId, registeredClient);
                return;
            }

            await _packetHandlingService.InvalidPacketResponse(socketId);
            return;
        }

        public async Task ParseDataPacketAsync(string socketId, DataPacketModel packet)
        {
            if (packet.PacketError == (uint)PacketError.None &&
                    _packetService.ValidatePacket(packet))
            {
                var decryptedData = _packetService.GetData(packet);
                if (decryptedData == null)
                {
                    await _packetHandlingService.InternalErrorResponse(socketId);
                    return;
                }

                var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken.SequenceEqual(packet.AuthorizationToken));
                if (registeredClient == null)
                {
                    await _packetHandlingService.InvalidPacketResponse(socketId);
                    return;
                }

                if (!_webSocketManagerService.IsClientBound(socketId, registeredClient))
                {
                    await _packetHandlingService.InternalErrorResponse(socketId);
                    return;
                }

                await _packetHandlingService.HandleDataAsync(socketId, registeredClient, decryptedData, packet);
                return;
            }

            await _packetHandlingService.InvalidPacketResponse(socketId);
            return;
        }

        public async Task ParseAckPacketAsync(string socketId, DataPacketModel packet)
        {
            if (packet.PacketError == (uint)PacketError.None &&
                    _packetService.ValidatePacket(packet))
            {
                var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken.SequenceEqual(packet.AuthorizationToken));
                if (registeredClient == null)
                {
                    await _packetHandlingService.InvalidPacketResponse(socketId);
                    return;
                }

                if (!_webSocketManagerService.IsClientBound(socketId, registeredClient))
                {
                    await _packetHandlingService.InternalErrorResponse(socketId);
                    return;
                }

                if (packet.ChainIdentifier == null || packet.ChainIdentifier.Length != 16)
                {
                    await _packetHandlingService.InvalidPacketResponse(socketId);
                    return;
                }

                var ackResult = _chainService.AckChain(packet.ChainIdentifier);
                return;
            }

            await _packetHandlingService.InvalidPacketResponse(socketId);
            return;
        }

        public async Task ParseErrorPacketAsync(string socketId, DataPacketModel packet)
        {
            if (!_packetService.ValidatePacket(packet))
            {
                await _packetHandlingService.InvalidPacketResponse(socketId);
                return;
            }

            var decryptedData = _packetService.GetData(packet);
            if (decryptedData == null)
            {
                await _packetHandlingService.InternalErrorResponse(socketId);
                return;
            }

            var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken.SequenceEqual(packet.AuthorizationToken));
            if (registeredClient == null)
            {
                await _packetHandlingService.InvalidPacketResponse(socketId);
                return;
            }

            if (!_webSocketManagerService.IsClientBound(socketId, registeredClient))
            {
                await _packetHandlingService.InternalErrorResponse(socketId);
                return;
            }

            await _packetHandlingService.HandleErrorAsync(socketId, registeredClient, (PacketError)packet.PacketError, decryptedData, packet);
            return;
        }

        public async Task ParseAndHandlePacketAsync(string socketId, DataPacketModel packet)
        {

            if (packet.PacketType == (uint)PacketType.Register)
            {
                await ParseRegisterPacketAsync(socketId, packet);
                return;
            }
            else if (packet.PacketType == (uint)PacketType.Auth)
            {
                await ParseAuthPacketAsync(socketId, packet);
                return;
            }
            else if (packet.PacketType == (uint)PacketType.RevokeAuth)
            {
                await ParseRevokeAuthPacketAsync(socketId, packet);
                return;
            }
            else if (packet.PacketType == (uint)PacketType.Ack)
            {
                await ParseAckPacketAsync(socketId, packet);
                return;
            }
            else if (packet.PacketType == (uint)PacketType.Data)
            {
                await ParseDataPacketAsync(socketId, packet);
                return;
            }
            else if (packet.PacketType == (uint)PacketType.Error)
            {
                await ParseErrorPacketAsync(socketId, packet);
                return;
            }

            await _packetHandlingService.InvalidPacketResponse(socketId);
            return;
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
                                var packet = DataPacketModel.Create(message);
                                if (packet == null)
                                {
                                    await _packetHandlingService.MalformedPacketResponse(socketId);
                                }
                                else
                                {
                                    await ParseAndHandlePacketAsync(socketId, packet);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error while processing packet: {ex.Message}");
                                Console.WriteLine($"Socket: {socketId}, Bound user: {_webSocketManagerService.GetBoundClient(socketId)?.Id ?? ""}");
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

        private const int PingInterval = 5000; // 1 second
        private const int TimeoutThreshold = 10000; // 5 seconds
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
