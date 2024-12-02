using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using System.Security.Cryptography;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class PacketService
    {
        private readonly ApplicationDbContext _context;

        public PacketService(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool IsPacketValid(DataPacketModel packet)
        {
            var isSignatureValid = packet.Signature.SequenceEqual(DataPacketModel.ValidSignature);
            var isSentAtValid = packet.SentAt <= (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            return isSignatureValid && isSentAtValid;
        }

        public bool IsPacketAuthorized(DataPacketModel packet)
        {
            var client = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken.SequenceEqual(packet.AuthorizationToken));
            return client != null;
        }

        public bool IsPacketSignatureValid(DataPacketModel packet)
        {
            // get registered client
            var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken.SequenceEqual(packet.AuthorizationToken));
            if (registeredClient == null)
            {
                return false;
            }

            // get signature from packet
            var signature = GetPacketSignature(packet);
            if(signature == null)
            {
                return false;
            }

            // compute signature from packet content
            HMACSHA256 hmac = new HMACSHA256(registeredClient.SignatureKey);
            var sign = hmac.ComputeHash(packet.GetPacketContent());

            // compare them
            return packet.PacketSignature.SequenceEqual(sign);
        }

        public byte[]? SignPacket(DataPacketModel packet, byte[] key)
        {
            if(key.Length != 32)
            {
                return null;
            }

            HMACSHA256 hmac = new HMACSHA256(key);
            return hmac.ComputeHash(packet.GetPacketContent());
        }

        public bool ValidatePacket(DataPacketModel packet)
        {
            return IsPacketValid(packet) && 
                (IsBeforeAuthPacket(packet) || (IsPacketAuthorized(packet) && IsPacketSignatureValid(packet)));
        }

        public byte[]? GetData(DataPacketModel packet)
        {
            // get registered client
            var registeredClient = _context.Clients.FirstOrDefault(c => c.Identifier.SequenceEqual(packet.AuthorizationToken));
            if (registeredClient == null)
            {
                return null;
            }

            var data = packet.PacketData;

            return data;
        }

        private bool IsAfterAuthPacket(DataPacketModel packet)
        {
            return !IsBeforeAuthPacket(packet);
        }

        private bool IsBeforeAuthPacket(DataPacketModel packet)
        {
            return (PacketType)packet.PacketType == PacketType.Auth || (PacketType)packet.PacketType == PacketType.Register;
        }

        private byte[]? GetPacketSignature(DataPacketModel packet)
        {
            // get registered client
            var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken.SequenceEqual(packet.AuthorizationToken));
            if (registeredClient == null)
            {
                return null;
            }

            var signature = packet.PacketSignature;

            return signature;
        }
    }
}
