using Challenge2_Group16_GUI_WebAPI.Interfaces;
using Challenge2_Group16_GUI_WebAPI.Models;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class PacketManagingService
    {
        private readonly DataService _dataService;
        private readonly PacketService _packetService;
        private readonly IConfiguration _configuration;
        private readonly PacketHandlingService _packetHandlingService;
        private readonly WebSocketHandlerService _webSocketHandlerService;
        private readonly WebSocketManagerService _webSocketManagerService;
        private readonly ChainService _chainService;

        public PacketManagingService(
            DataService dataService,
            PacketService packetService,
            IConfiguration configuration,
            PacketHandlingService packetHandlingService,
            WebSocketHandlerService webSocketHandlerService,
            WebSocketManagerService webSocketManagerService,
            ChainService chainService)
        {
            _dataService = dataService;
            _packetService = packetService;
            _configuration = configuration;
            _packetHandlingService = packetHandlingService;
            _webSocketHandlerService = webSocketHandlerService;
            _webSocketManagerService = webSocketManagerService;
            _chainService = chainService;
        }

        public async Task StartRequest(string socketId, RegisteredClient client)
        {
            byte flag = 0b00011000; // binary command data
            byte[] data = { flag, 0xff }; // start command 0xff

            var packet = DataPacketModel.Data(data);
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                await _packetHandlingService.InternalErrorResponse(socketId);
                return;
            }

            packet.PacketSignature = packetSignature;

            await _webSocketManagerService.SendAsync(socketId, packet.GetPacket());
            await _chainService.ExpectAck(packet.ChainIdentifier);
        }

        public async Task PauseRequest(string socketId, RegisteredClient client)
        {
            byte flag = 0b00011000; // binary command data
            byte[] data = { flag, 0x00 }; // stop command 0x00

            var packet = DataPacketModel.Data(data);
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                await _packetHandlingService.InternalErrorResponse(socketId);
                return;
            }

            packet.PacketSignature = packetSignature;

            await _webSocketManagerService.SendAsync(socketId, packet.GetPacket());
            await _chainService.ExpectAck(packet.ChainIdentifier);
        }

        public async Task SetTargetRequest(string socketId, RegisteredClient client, byte dataType, byte[] data)
        {
            if(data.Length == 0 || dataType > 2)
            {
                await _packetHandlingService.InternalErrorResponse(socketId);
                return;
            }

            byte flag = 0b00011000; // binary command data
            byte[] fulldata = { flag, 0x01, dataType }; // set target command 0x01
            fulldata = fulldata.Concat(data).ToArray();

            var packet = DataPacketModel.Data(fulldata);
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                await _packetHandlingService.InternalErrorResponse(socketId);
                return;
            }

            packet.PacketSignature = packetSignature;

            await _webSocketManagerService.SendAsync(socketId, packet.GetPacket());
            await _chainService.ExpectAck(packet.ChainIdentifier);
        }

        public async Task<DeviceStatusData?> DeviceStatusRequest(string socketId, RegisteredClient client)
        {
            byte flag = 0b00011000; // binary command data
            byte[] fulldata = { flag, 0x02 }; // get status command 0x02

            var packet = DataPacketModel.Data(fulldata);
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                await _packetHandlingService.InternalErrorResponse(socketId);
                return null;
            }

            packet.PacketSignature = packetSignature;

            await _webSocketManagerService.SendAsync(socketId, packet.GetPacket());
            var data = await _chainService.Expect<DeviceStatusData>(packet.ChainIdentifier);

            return data;
        }   
    }
}
