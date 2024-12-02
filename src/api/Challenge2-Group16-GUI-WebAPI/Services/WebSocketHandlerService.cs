using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
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

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                        return (result.MessageType, null);
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
                    packet.PacketSignature.SequenceEqual(new byte[32]))
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
                    packet.PacketData.Length == 64)
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
                    packet.PacketData.Length == 0)
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
            if (packet.PacketError == (uint)PacketError.None)
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
            if (packet.PacketError == (uint)PacketError.None)
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
            if (_packetService.ValidatePacket(packet) == false)
            {
                await _packetHandlingService.InvalidPacketResponse(socketId);
                return;
            }

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

        public async Task HandleAsync(WebSocket socket)
        {
            var socketId = Guid.NewGuid().ToString();
            _webSocketManagerService.AddSocket(socketId, socket);

            WebSocketMessageType type;

            do
            {
                (type, var message) = await ReceiveAsync(socket);
                var packet = DataPacketModel.Create(message);
                if (packet == null)
                {
                    await _packetHandlingService.MalformedPacketResponse(socketId);
                    continue;
                }

                Console.WriteLine(DateTime.Now);
                await ParseAndHandlePacketAsync(socketId, packet);
            }
            while (type != WebSocketMessageType.Close);

            await _webSocketManagerService.RemoveSocketAsync(socketId);
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
