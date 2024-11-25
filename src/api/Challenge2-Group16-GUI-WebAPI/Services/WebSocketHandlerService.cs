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
        private readonly ConcurrentDictionary<string, byte[]> _authorizedSockets = new();
        

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
                    packet.PacketEncryptionMethod == (uint)PacketEncryptionMethod.None &&
                    packet.DataSize == 20 &&
                    packet.EncryptedData.Length == 20 &&
                    packet.PacketSignature.SequenceEqual(new byte[32]))
            {
                byte[] clientId = packet.EncryptedData.Take(16).ToArray();
                ClientType clientType = (ClientType)BitConverter.ToUInt32(packet.EncryptedData.Skip(16).Take(4).ToArray(), 0);
                var registerResult = await _registeredClientService.RegisterClientAsync(clientId, clientType);
                if (registerResult == null)
                {
                    await _packetHandlingService.InternalErrorResponse(socketId);
                    return;
                }

                await _packetHandlingService.RegisterResponse(socketId, registerResult, registerResult.Secret, registerResult.SignatureKey, registerResult.EncryptionKey, registerResult.EncryptionIV);
                return;
            }

            await _packetHandlingService.InvalidPacketResponse(socketId);
            return;
        }

        public async Task ParseAuthPacketAsync(string socketId, DataPacketModel packet)
        {
            if (packet.AuthorizationToken.SequenceEqual(new byte[16]) &&
                    packet.PacketError == (uint)PacketError.None &&
                    packet.PacketEncryptionMethod == (uint)PacketEncryptionMethod.None &&
                    packet.DataSize == 48 &&
                    packet.EncryptedData.Length == 48)
            {
                byte[] clientId = packet.EncryptedData.Take(16).ToArray();
                byte[] clientSecret = packet.EncryptedData.Skip(16).Take(32).ToArray();

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

                var encryptedAuthToken = PacketService.Encrypt(temporaryAuthToken, registeredClient.EncryptionKey, registeredClient.EncryptionIV);
                if (encryptedAuthToken == null)
                {
                    await _packetHandlingService.InternalErrorResponse(socketId);
                    return;
                }

                await _packetHandlingService.AuthResponse(socketId, registeredClient, encryptedAuthToken);
                _authorizedSockets.TryAdd(socketId, temporaryAuthToken);
                return;
            }

            await _packetHandlingService.InvalidPacketResponse(socketId);
            return;
        }

        public async Task ParseRevokeAuthPacketAsync(string socketId, DataPacketModel packet)
        {
            if (packet.PacketError == (uint)PacketError.None &&
                    packet.PacketEncryptionMethod == (uint)PacketEncryptionMethod.AES &&
                    packet.DataSize == 0 &&
                    packet.EncryptedData.Length == 0)
            {
                var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken == packet.AuthorizationToken);
                if (registeredClient == null)
                {
                    await _packetHandlingService.InvalidPacketResponse(socketId);
                    return;
                }

                var authValidationResult = ValidateAuthTokenAndSocketId(socketId, packet.AuthorizationToken);
                if (authValidationResult == false)
                {
                    await _packetHandlingService.InternalErrorResponse(socketId);
                    return;
                }

                if (!await _registeredClientService.RevokeClientAsync(packet.AuthorizationToken))
                {
                    await _packetHandlingService.InternalErrorResponse(socketId);
                    return;
                }

                _authorizedSockets.TryRemove(socketId, out _);

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
                var authValidationResult = ValidateAuthTokenAndSocketId(socketId, packet.AuthorizationToken);
                if (authValidationResult == false)
                {
                    await _packetHandlingService.InvalidPacketResponse(socketId);
                    return;
                }

                var decryptedData = _packetService.GetDecryptedData(packet);
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
                var authValidationResult = ValidateAuthTokenAndSocketId(socketId, packet.AuthorizationToken);
                if (authValidationResult == false)
                {
                    await _packetHandlingService.InvalidPacketResponse(socketId);
                    return;
                }

                var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken.SequenceEqual(packet.AuthorizationToken));
                if (registeredClient == null)
                {
                    await _packetHandlingService.InvalidPacketResponse(socketId);
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
            if (packet.PacketEncryptionMethod == (uint)PacketEncryptionMethod.AES)
            {
                var authValidationResult = ValidateAuthTokenAndSocketId(socketId, packet.AuthorizationToken);
                if (authValidationResult == false)
                {
                    await _packetHandlingService.InvalidPacketResponse(socketId);
                    return;
                }

                var decryptedData = _packetService.GetDecryptedData(packet);
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

                await _packetHandlingService.HandleErrorAsync(socketId, registeredClient, (PacketError)packet.PacketError, decryptedData, packet);
                return;
            }

            await _packetHandlingService.InvalidPacketResponse(socketId);
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

                await ParseAndHandlePacketAsync(socketId, packet);
            }
            while (type != WebSocketMessageType.Close && socket.State == WebSocketState.Open);

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

        private bool ValidateAuthTokenAndSocketId(string socketId, byte[] authToken)
        {
            if (!_authorizedSockets.TryGetValue(socketId, out var storedAuthToken))
            {
                return false;
            }

            return storedAuthToken.SequenceEqual(authToken);
        }
    }
}
