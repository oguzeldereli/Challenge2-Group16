﻿using Microsoft.VisualBasic;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;

namespace Challenge2_Group16_GUI_WebAPI.Models
{
    public enum PacketType
    {
        Ack,
        Register,
        Auth,
        RevokeAuth,
        Data,
        Error
    }

    public enum PacketError
    {
        None,
        MalformedPacket,
        InvalidPacket,
        InternalError
    }


    public enum PacketEncryptionMethod
    {
        None,
        AES
    }

    public class DataPacketModel
    {
        public static byte[] ValidSignature = { 1, 16, (byte)'e', (byte)'s', (byte)'p', (byte)'c', (byte)'o', (byte)'m' }; 

        public byte[] Signature { get; set; } // 8 bytes
        public ulong SentAt { get; set; } // 8 bytes
        public byte[] ChainIdentifier { get; set; } // 16 bytes
        public byte[] AuthorizationToken { get; set; } // 16 bytes
        public uint PacketType { get; set; } // 4 bytes
        public uint PacketError { get; set; } // 4 bytes
        public uint DataSize { get; set; } // 4 bytes
        public byte[] PacketData { get; set; } // DataSize bytes
        public byte[] PacketSignature { get; set; } // 32 bytes
            
        public DataPacketModel()
        {
            Signature = new byte[8];
            SentAt = 0;
            ChainIdentifier = new byte[16];
            AuthorizationToken = new byte[16];
            DataSize = 0;
            PacketSignature = new byte[32];
        }

        public static DataPacketModel? Create(byte[] packet)
        {
            // Minimum packet size: 96 bytes (fixed fields)
            if (packet.Length < 92)
            {
                Console.WriteLine("Packet length is less than the minimum required size of 100 bytes.");
                return null;
            }

            var createdPacket = new DataPacketModel();

            try
            {
                // 1. Extract Signature (bytes 0-7)
                createdPacket.Signature = packet[0..8];

                // 2. Extract SentAt (bytes 8-15)
                createdPacket.SentAt = BitConverter.ToUInt64(packet, 8);

                // 3. Extract PacketIdentifier (bytes 16-31)
                createdPacket.ChainIdentifier = packet[16..32];

                // 4. Extract AuthorizationToken (bytes 32-47)
                createdPacket.AuthorizationToken = packet[32..48];

                // 5. Extract PacketType (bytes 48-51)
                createdPacket.PacketType = BitConverter.ToUInt32(packet, 48);

                // 6. Extract PacketError (bytes 52-55)
                createdPacket.PacketError = BitConverter.ToUInt32(packet, 52);

                // 8. Extract DataSize (bytes 56-59)
                createdPacket.DataSize = BitConverter.ToUInt32(packet, 56);

                // Validate that the packet contains enough bytes for Data and PacketSign
                uint requiredLength = 92 + createdPacket.DataSize; // 60 bytes before Data, DataSize, 32 bytes for PacketSign
                if (packet.Length < requiredLength)
                {
                    return null;
                }

                // 9. Extract Data (bytes 60 to 60 + DataSize -1)
                if (createdPacket.DataSize > int.MaxValue)
                {
                    return null;
                }

                createdPacket.PacketData = packet[60..(60 + (int)createdPacket.DataSize)];

                // 10. Extract PacketSign (last 32 bytes)
                createdPacket.PacketSignature = packet[(60 + (int)createdPacket.DataSize)..(92 + (int)createdPacket.DataSize)];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"Error parsing packet: {ex.Message}");
                return null;
            }

            return createdPacket;
        }

        public static DataPacketModel MalformedPacket()
        {
            var dataPacketModel = new DataPacketModel();
            dataPacketModel.Signature = ValidSignature;
            dataPacketModel.SentAt = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            dataPacketModel.ChainIdentifier = Guid.NewGuid().ToByteArray();
            dataPacketModel.AuthorizationToken = new byte[16] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            dataPacketModel.PacketType = (uint)Models.PacketType.Error;
            dataPacketModel.PacketError = (uint)Models.PacketError.MalformedPacket;
            dataPacketModel.DataSize = 0;
            dataPacketModel.PacketData = new byte[0];
            dataPacketModel.PacketSignature = new byte[32];
            return dataPacketModel;
        }

        public static DataPacketModel InvalidPacket()
        {
            var dataPacketModel = new DataPacketModel();
            dataPacketModel.Signature = ValidSignature;
            dataPacketModel.SentAt = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            dataPacketModel.ChainIdentifier = Guid.NewGuid().ToByteArray();
            dataPacketModel.AuthorizationToken = new byte[16] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            dataPacketModel.PacketType = (uint)Models.PacketType.Error;
            dataPacketModel.PacketError = (uint)Models.PacketError.InvalidPacket;
            dataPacketModel.DataSize = 0;
            dataPacketModel.PacketData = new byte[0];
            dataPacketModel.PacketSignature = new byte[32];
            return dataPacketModel;
        }

        public static DataPacketModel InternalError()
        {
            var dataPacketModel = new DataPacketModel();
            dataPacketModel.Signature = ValidSignature;
            dataPacketModel.SentAt = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            dataPacketModel.ChainIdentifier = Guid.NewGuid().ToByteArray();
            dataPacketModel.AuthorizationToken = new byte[16] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            dataPacketModel.PacketType = (uint)Models.PacketType.Error;
            dataPacketModel.PacketError = (uint)Models.PacketError.InternalError;
            dataPacketModel.DataSize = 0;
            dataPacketModel.PacketData = new byte[0];
            dataPacketModel.PacketSignature = new byte[32];
            return dataPacketModel;
        }

        public static DataPacketModel RegisterResponse(byte[] secret, byte[] signatureKey)
        {
            var dataPacketModel = new DataPacketModel();
            dataPacketModel.Signature = DataPacketModel.ValidSignature;
            dataPacketModel.SentAt = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            dataPacketModel.ChainIdentifier = Guid.NewGuid().ToByteArray();
            dataPacketModel.AuthorizationToken = new byte[16] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            dataPacketModel.PacketType = (uint)Models.PacketType.Register;
            dataPacketModel.PacketError = (uint)Models.PacketError.None;
            dataPacketModel.DataSize = 64;
            dataPacketModel.PacketData = CombineByteArrays(
                secret,
                signatureKey);
            return dataPacketModel;
        }

        public static DataPacketModel AuthResponse(byte[] authToken)
        {
            var dataPacketModel = new DataPacketModel();
            dataPacketModel.Signature = DataPacketModel.ValidSignature;
            dataPacketModel.SentAt = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            dataPacketModel.ChainIdentifier = Guid.NewGuid().ToByteArray();
            dataPacketModel.AuthorizationToken = new byte[16] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }; ;
            dataPacketModel.PacketType = (uint)Models.PacketType.Auth;
            dataPacketModel.PacketError = (uint)Models.PacketError.None;
            dataPacketModel.DataSize = 16;
            dataPacketModel.PacketData = authToken;
            return dataPacketModel;
        }

        public static DataPacketModel RevokeAuthResponse()
        {
            var dataPacketModel = new DataPacketModel();
            dataPacketModel.Signature = DataPacketModel.ValidSignature;
            dataPacketModel.SentAt = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            dataPacketModel.ChainIdentifier = Guid.NewGuid().ToByteArray();
            dataPacketModel.AuthorizationToken = new byte[16] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }; ;
            dataPacketModel.PacketType = (uint)Models.PacketType.RevokeAuth;
            dataPacketModel.PacketError = (uint)Models.PacketError.None;
            dataPacketModel.DataSize = 0;
            dataPacketModel.PacketData = new byte[0];
            return dataPacketModel;
        }

        public static DataPacketModel Data(byte[] data)
        {
            var dataPacketModel = new DataPacketModel();
            dataPacketModel.Signature = DataPacketModel.ValidSignature;
            dataPacketModel.SentAt = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            dataPacketModel.ChainIdentifier = Guid.NewGuid().ToByteArray();
            dataPacketModel.AuthorizationToken = new byte[16] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }; ;
            dataPacketModel.PacketType = (uint)Models.PacketType.Data;
            dataPacketModel.PacketError = (uint)Models.PacketError.None;
            dataPacketModel.DataSize = (uint)data.Length;
            dataPacketModel.PacketData = data;
            return dataPacketModel;
        }

        public static DataPacketModel Ack(byte[] packetId)
        {
            var dataPacketModel = new DataPacketModel();
            dataPacketModel.Signature = DataPacketModel.ValidSignature;
            dataPacketModel.SentAt = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            dataPacketModel.ChainIdentifier = packetId;
            dataPacketModel.AuthorizationToken = new byte[16] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }; ;
            dataPacketModel.PacketType = (uint)Models.PacketType.Ack;
            dataPacketModel.PacketError = (uint)Models.PacketError.None;
            dataPacketModel.DataSize = 0;
            dataPacketModel.PacketData = new byte[0];
            return dataPacketModel;
        }

        public byte[] GetPacket() {
            return CombineByteArrays(
                    Signature,
                    BitConverter.GetBytes(SentAt),
                    ChainIdentifier,
                    AuthorizationToken,
                    BitConverter.GetBytes(PacketType),
                    BitConverter.GetBytes(PacketError),
                    BitConverter.GetBytes(DataSize),
                    PacketData,
                    PacketSignature
                );
        }

        public byte[] GetPacketContent()
        {
            return CombineByteArrays(
                    Signature,
                    BitConverter.GetBytes(SentAt),
                    ChainIdentifier,
                    AuthorizationToken,
                    BitConverter.GetBytes(PacketType),
                    BitConverter.GetBytes(PacketError),
                    BitConverter.GetBytes(DataSize),
                    DataSize > 0 ? PacketData : new byte[0]
                );
        }

        public static byte[] CombineByteArrays(params byte[][] arrays)
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

        public static byte[] AppendByteArrays(byte[] array1, byte[] array2)
        {
            byte[] result = new byte[array1.Length + array2.Length];

            // Use Span to slice and copy more efficiently
            Span<byte> span = result;
            array1.CopyTo(span.Slice(0, array1.Length));
            array2.CopyTo(span.Slice(array1.Length, array2.Length));

            return result;
        }
    }
}
