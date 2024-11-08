﻿using System.Security.Cryptography;

namespace Challenge2_Group16_GUI_WebAPI.Models
{
    public enum ClientType
    {
        @public,
        confidential
    }

    public class RegisteredClient
    {
        public string Id { get; set; }

        // Auth details
        public byte[] Identifier { get; set; }
        public byte[] Secret { get; set; }

        // Temporary Auth Token
        public byte[] TemporaryAuthToken { get; set; }

        // Client Type
        public ClientType Type { get; set; }

        // Packet Signature Key
        public byte[] SignatureKey { get; set; }

        // Data Encryption Key and IV
        public byte[] EncryptionKey { get; set; }
        public byte[] EncryptionIV { get; set; }

        // Raw Data (each entry is kept for 1 day)
        public List<TempData> TempData { get; set; }
        public List<pHData> pHData { get; set; }
        public List<StirringData> StirringData { get; set; }
        public List<DeviceStatusData> DeviceStatusData { get; set; }
        public List<ErrorData> ErrorData { get; set; }

        // Cold Storage (entries are aggregated into daily entries)
        public List<TempAggregateData> TempAggregateData { get; set; }
        public List<pHAggregateData> pHAggregateData { get; set; }
        public List<StirringAggregateData> StirringAggregateDatas { get; set; }
        public List<DeviceStatusAggregateData> deviceStatusAggregateDatas { get; set; }
        public List<ErrorAggregateData> ErrorAggregateDatas { get; set; }


        public RegisteredClient()
        {
            Id = Guid.NewGuid().ToString();
            Identifier = new byte[16];
            TemporaryAuthToken = new byte[16];
            Secret = new byte[32];
            SignatureKey = new byte[32];
            EncryptionKey = new byte[32];
            EncryptionIV = new byte[16];
        }

        public static RegisteredClient? Create(byte[] identifier, ClientType type)
        {
            if(identifier.Length != 16)
            {
                return null;
            }

            RegisteredClient client = new RegisteredClient();
            client.Identifier = identifier;
            client.Type = type;
            RandomNumberGenerator.Fill(client.Secret);
            RandomNumberGenerator.Fill(client.SignatureKey);
            RandomNumberGenerator.Fill(client.EncryptionKey);
            RandomNumberGenerator.Fill(client.EncryptionIV);
            Array.Clear(client.TemporaryAuthToken, 0, client.TemporaryAuthToken.Length); // make sure auth token is empty

            return client;
        }
    }
}