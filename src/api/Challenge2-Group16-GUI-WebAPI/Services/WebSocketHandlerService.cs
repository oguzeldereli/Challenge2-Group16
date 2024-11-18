using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

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
        private readonly ConcurrentDictionary<string, byte[]> _sockets = new();

        public WebSocketHandlerService(
            ApplicationDbContext context,
            ILogger<WebSocketHandlerService> logger,
            PacketService packetService,
            RegisteredClientService registeredClientService,
            WebSocketManagerService webSocketManagerService,
            PacketHandlingService packetHandlingService)
        {
            _context = context;
            _logger = logger;
            _packetService = packetService;
            _registeredClientService = registeredClientService;
            _webSocketManagerService = webSocketManagerService;
            _packetHandlingService = packetHandlingService;
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

        public async Task<DataPacketModel?> ParseRegisterPacketAsync(string socketId, DataPacketModel packet)
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
                    return _packetHandlingService.InternalErrorResponse();
                }

                var dataPacketModel = _packetHandlingService.RegisterResponse(registerResult, registerResult.Secret, registerResult.SignatureKey, registerResult.EncryptionKey, registerResult.EncryptionIV);
                
                return dataPacketModel;
            }

            return _packetHandlingService.InvalidPacketResponse();
        }

        public async Task<DataPacketModel?> ParseAuthPacketAsync(string socketId, DataPacketModel packet)
        {
            if (packet.AuthorizationToken.SequenceEqual(new byte[16]) &&
                    packet.PacketError == (uint)PacketError.None &&
                    packet.PacketEncryptionMethod == (uint)PacketEncryptionMethod.None &&
                    packet.DataSize == 48 &&
                    packet.EncryptedData.Length == 48)
            {
                byte[] clientId = packet.EncryptedData.Take(16).ToArray();
                byte[] clientSecret = packet.EncryptedData.Skip(16).Take(32).ToArray();

                var temporaryAuthToken = await _registeredClientService.AuthorizeClientAsync(clientId, clientSecret);
                if (temporaryAuthToken == null)
                {
                    return _packetHandlingService.InternalErrorResponse();
                }

                var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken.SequenceEqual(temporaryAuthToken));
                if (registeredClient == null)
                {
                    return _packetHandlingService.InternalErrorResponse();
                }

                var encryptedAuthToken = PacketService.Encrypt(temporaryAuthToken, registeredClient.EncryptionKey, registeredClient.EncryptionIV);
                if (encryptedAuthToken == null)
                {
                    return _packetHandlingService.InternalErrorResponse();
                }

                var dataPacketModel = _packetHandlingService.AuthResponse(registeredClient, encryptedAuthToken);

                _sockets.TryAdd(socketId, temporaryAuthToken);
                return dataPacketModel;
            }
            
            return _packetHandlingService.InvalidPacketResponse();
        }

        public async Task<DataPacketModel?> ParseRevokeAuthPacketAsync(string socketId, DataPacketModel packet)
        {
            if (packet.PacketError == (uint)PacketError.None &&
                    packet.PacketEncryptionMethod == (uint)PacketEncryptionMethod.AES &&
                    packet.DataSize == 0 &&
                    packet.EncryptedData.Length == 0)
            {
                var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken == packet.AuthorizationToken);
                if (registeredClient == null)
                {
                    return _packetHandlingService.InvalidPacketResponse();
                }

                var authValidationResult = ValidateAuthTokenAndSocketId(socketId, packet.AuthorizationToken);
                if (authValidationResult == false)
                {
                    return _packetHandlingService.InvalidPacketResponse();
                }

                if (!await _registeredClientService.RevokeClientAsync(packet.AuthorizationToken))
                {
                    return _packetHandlingService.InternalErrorResponse();
                }

                _sockets.TryRemove(socketId, out _);

                var dataPacketModel = _packetHandlingService.RevokeAuthResponse(registeredClient);

                return dataPacketModel;
            }

            return _packetHandlingService.InvalidPacketResponse();
        }

        public async Task<DataPacketModel?> ParseDataPacketAsync(string socketId, DataPacketModel packet)
        {
            if (packet.PacketError == (uint)PacketError.None)
            {
                var authValidationResult = ValidateAuthTokenAndSocketId(socketId, packet.AuthorizationToken);
                if (authValidationResult == false)
                {
                    return _packetHandlingService.InvalidPacketResponse();
                }

                var decryptedData = _packetService.GetDecryptedData(packet);
                if (decryptedData == null)
                {
                    return _packetHandlingService.InternalErrorResponse();
                }

                var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken.SequenceEqual(packet.AuthorizationToken));
                if (registeredClient == null)
                {
                    return _packetHandlingService.InvalidPacketResponse();
                }

                return await _packetHandlingService.HandleDataAsync(registeredClient, decryptedData, packet);
            }

            return _packetHandlingService.InvalidPacketResponse();
        }

        public async Task<DataPacketModel?> ParseAckPacketAsync(string socketId, DataPacketModel packet)
        {
            if (packet.PacketError == (uint)PacketError.None)
            {
                var authValidationResult = ValidateAuthTokenAndSocketId(socketId, packet.AuthorizationToken);
                if (authValidationResult == false)
                {
                    return _packetHandlingService.InvalidPacketResponse();
                }

                var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken.SequenceEqual(packet.AuthorizationToken));
                if (registeredClient == null)
                {
                    return _packetHandlingService.InvalidPacketResponse();
                }

                return await _packetHandlingService.HandleAckAsync(packet);
            }

            return _packetHandlingService.InvalidPacketResponse();
        }

        public async Task<DataPacketModel?> ParseErrorPacketAsync(string socketId, DataPacketModel packet)
        {
            if (packet.PacketEncryptionMethod == (uint)PacketEncryptionMethod.AES)
            {
                var authValidationResult = ValidateAuthTokenAndSocketId(socketId, packet.AuthorizationToken);
                if (authValidationResult == false)
                {
                    return _packetHandlingService.InvalidPacketResponse();
                }

                var decryptedData = _packetService.GetDecryptedData(packet);
                if (decryptedData == null)
                {
                    return _packetHandlingService.InternalErrorResponse();
                }

                var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken.SequenceEqual(packet.AuthorizationToken));
                if (registeredClient == null)
                {
                    return _packetHandlingService.InvalidPacketResponse();
                }

                return await _packetHandlingService.HandleErrorAsync(registeredClient, (PacketError)packet.PacketError, decryptedData, packet);
            }

            return _packetHandlingService.InvalidPacketResponse();
        }

        public async Task<DataPacketModel?> ParsePacketAsync(string socketId, DataPacketModel packet)
        {
            if (_packetService.ValidatePacket(packet) == false)
            {
                return _packetHandlingService.InvalidPacketResponse();
            }

            if (packet.PacketType == (uint)PacketType.Register)
            {
                return await ParseRegisterPacketAsync(socketId, packet);
            }
            else if (packet.PacketType == (uint)PacketType.Auth)
            {
                return await ParseAuthPacketAsync(socketId, packet);
            }
            else if (packet.PacketType == (uint)PacketType.RevokeAuth)
            {
                return await ParseRevokeAuthPacketAsync(socketId, packet);
            }
            else if (packet.PacketType == (uint)PacketType.Ack)
            {
                return await ParseAckPacketAsync(socketId, packet);
            }
            else if (packet.PacketType == (uint)PacketType.Data)
            {
                return await ParseDataPacketAsync(socketId, packet);
            }
            else if (packet.PacketType == (uint)PacketType.Error)
            {
                return await ParseErrorPacketAsync(socketId, packet);
            }

            return _packetHandlingService.InvalidPacketResponse();
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
                    DataPacketModel dataPacketModel = _packetHandlingService.MalformedPacketResponse();

                    await _webSocketManagerService.SendAsync(socket, dataPacketModel.GetPacket());
                    continue;
                }

                var response = await ParsePacketAsync(socketId, packet);
                if (response == null)
                {
                    continue;
                }

                await _webSocketManagerService.SendAsync(socket, response.GetPacket());

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

        private bool ValidateAuthTokenAndSocketId(string socketId, byte[] authToken)
        {
            if (!_sockets.TryGetValue(socketId, out var storedAuthToken))
            {
                return false;
            }

            return storedAuthToken.SequenceEqual(authToken);
        }
    }
}
