using Challenge2_Group16_GUI_WebAPI.Interfaces;
using Challenge2_Group16_GUI_WebAPI.Models;
using System.Collections.Concurrent;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class PacketManagingService
    {
        private readonly DataService _dataService;
        private readonly PacketService _packetService;
        private readonly IConfiguration _configuration;
        private readonly PacketHandlingService _packetHandlingService;

        public PacketManagingService(
            DataService dataService,
            PacketService packetService,
            IConfiguration configuration,
            PacketHandlingService packetHandlingService)
        {
            _dataService = dataService;
            _packetService = packetService;
            _configuration = configuration;
            _packetHandlingService = packetHandlingService;
        }

        public DataPacketModel StartRequest(RegisteredClient client)
        {
            byte flag = 0b00011000; // binary command data
            byte[] data = { flag, 0xff }; // start command 0xff

            var encryptedData = PacketService.Encrypt(data, client.EncryptionKey, client.EncryptionIV);

            var packet = DataPacketModel.Data(encryptedData);
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                return _packetHandlingService.InternalErrorResponse();
            }

            packet.PacketSignature = packetSignature;
            _packetHandlingService.ExpectedAcks[packet.PacketIdentifier] = packet;
            return packet;
        }

        public DataPacketModel StopRequest(RegisteredClient client)
        {
            byte flag = 0b00011000; // binary command data
            byte[] data = { flag, 0x00 }; // stop command 0x00

            var encryptedData = PacketService.Encrypt(data, client.EncryptionKey, client.EncryptionIV);

            var packet = DataPacketModel.Data(encryptedData);
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                return _packetHandlingService.InternalErrorResponse();
            }

            packet.PacketSignature = packetSignature;
            _packetHandlingService.ExpectedAcks[packet.PacketIdentifier] = packet;
            return packet;
        }

        public DataPacketModel SeTargetRequest(RegisteredClient client, byte dataType, byte[] data)
        {
            if(data.Length == 0 || dataType > 2)
            {
                return _packetHandlingService.InternalErrorResponse();
            }

            byte flag = 0b00011000; // binary command data
            byte[] fulldata = { flag, 0x01, dataType }; // set target command 0x01
            fulldata = fulldata.Concat(data).ToArray();

            var encryptedData = PacketService.Encrypt(fulldata, client.EncryptionKey, client.EncryptionIV);

            var packet = DataPacketModel.Data(encryptedData);
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                return _packetHandlingService.InternalErrorResponse();
            }

            packet.PacketSignature = packetSignature;
            _packetHandlingService.ExpectedAcks[packet.PacketIdentifier] = packet;
            return packet;
        }
    }
}
