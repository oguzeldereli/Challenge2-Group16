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
            var client = _context.Clients.FirstOrDefault(c => c.Identifier.SequenceEqual(packet.AuthorizationToken));
            return client != null;
        }

        public bool IsPacketSignatureValid(DataPacketModel packet)
        {
            // get registered client
            var registeredClient = _context.Clients.FirstOrDefault(c => c.Identifier.SequenceEqual(packet.AuthorizationToken));
            if (registeredClient == null)
            {
                return false;
            }

            // get signature from packet
            var signature = GetDecryptedPacketSignature(packet);
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
                (IsAuthRelatedPacket(packet) || IsPacketAuthorized(packet)) &&
                (IsRegisterPacket(packet) || IsPacketSignatureValid(packet));
        }

        public byte[]? GetDecryptedData(DataPacketModel packet)
        {
            // get registered client
            var registeredClient = _context.Clients.FirstOrDefault(c => c.Identifier.SequenceEqual(packet.AuthorizationToken));
            if (registeredClient == null)
            {
                return null;
            }

            var data = packet.EncryptedData;
            if (packet.PacketEncryptionMethod == (uint)PacketEncryptionMethod.AES)
            {
                data = Decrypt(packet.EncryptedData, registeredClient.EncryptionKey, registeredClient.EncryptionIV);
            }

            return data;
        }

        private bool IsAuthRelatedPacket(DataPacketModel packet)
        {
            return (PacketType)packet.PacketType == PacketType.Auth || (PacketType)packet.PacketType == PacketType.Register;
        }

        private bool IsRegisterPacket(DataPacketModel packet)
        {
            return (PacketType)packet.PacketType == PacketType.Register;
        }

        private byte[]? GetDecryptedPacketSignature(DataPacketModel packet)
        {
            // get registered client
            var registeredClient = _context.Clients.FirstOrDefault(c => c.Identifier.SequenceEqual(packet.AuthorizationToken));
            if (registeredClient == null)
            {
                return null;
            }

            var signature = packet.PacketSignature;
            if (packet.PacketEncryptionMethod == (uint)PacketEncryptionMethod.AES)
            {
                signature = Decrypt(packet.PacketSignature, registeredClient.EncryptionKey, registeredClient.EncryptionIV);
            }

            return signature;
        }

        public static byte[] Encrypt(byte[] plainBytes, byte[] key, byte[] iv)
        {
            if (plainBytes == null || plainBytes.Length <= 0)
                throw new ArgumentNullException(nameof(plainBytes));
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));
            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException(nameof(iv));

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(plainBytes, 0, plainBytes.Length);
                    }

                    return msEncrypt.ToArray();
                }
            }
        }

        public static byte[] Decrypt(byte[] cipherBytes, byte[] key, byte[] iv)
        {
            if (cipherBytes == null || cipherBytes.Length <= 0)
                throw new ArgumentNullException(nameof(cipherBytes));
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));
            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException(nameof(iv));

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;

                // Create a decryptor
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (MemoryStream originalBytes = new MemoryStream())
                        {
                            csDecrypt.CopyTo(originalBytes);
                            return originalBytes.ToArray(); // Return decrypted byte array
                        }
                    }
                }
            }
        }
    }
}
