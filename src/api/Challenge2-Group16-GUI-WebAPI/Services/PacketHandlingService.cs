using Challenge2_Group16_GUI_WebAPI.Interfaces;
using Challenge2_Group16_GUI_WebAPI.Models;
using Microsoft.AspNetCore.DataProtection;
using NuGet.Packaging;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class PacketHandlingService
    {
        private readonly DataService _dataService;
        private readonly PacketService _packetService;
        private readonly IConfiguration _configuration;

        private readonly ConcurrentDictionary<byte[], DataPacketModel> _expectedAcks = new();

        public PacketHandlingService(
            DataService dataService,
            PacketService packetService,
            IConfiguration configuration)
        {
            _dataService = dataService;
            _packetService = packetService;
            _configuration = configuration;
        }

        public DataPacketModel MalformedPacketResponse()
        {
            var packet = DataPacketModel.MalformedPacketResponse();
            _expectedAcks[packet.PacketIdentifier] = packet;
            return packet;
        }

        public DataPacketModel InvalidPacketResponse()
        {
            var packet = DataPacketModel.InvalidPacketResponse();
            _expectedAcks[packet.PacketIdentifier] = packet;
            return packet;
        }

        public DataPacketModel InternalErrorResponse()
        {
            var packet = DataPacketModel.InternalErrorResponse();
            _expectedAcks[packet.PacketIdentifier] = packet;
            return packet;
        }

        public DataPacketModel RegisterResponse(RegisteredClient client, byte[] secret, byte[] signatureKey, byte[] encryptionKey, byte[] encryptionIv)
        {
            var packet = DataPacketModel.RegisterResponse(secret, signatureKey, encryptionKey, encryptionIv);
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                return InternalErrorResponse();
            }

            packet.PacketSign = packetSignature;
            _expectedAcks[packet.PacketIdentifier] = packet;

            return packet;
        }


        public DataPacketModel AuthResponse(RegisteredClient client, byte[] encryptedAuthToken)
        {
            var packet = DataPacketModel.AuthResponse(encryptedAuthToken);
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                return InternalErrorResponse();
            }

            packet.PacketSign = packetSignature;
            _expectedAcks[packet.PacketIdentifier] = packet;
            return packet;
        }

        public DataPacketModel RevokeAuthResponse(RegisteredClient client)
        {
            var packet = DataPacketModel.RevokeAuthResponse();
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                return InternalErrorResponse();
            }

            packet.PacketSign = packetSignature;
            _expectedAcks[packet.PacketIdentifier] = packet;
            return packet;
        }

        public DataPacketModel DataReturnResponse(RegisteredClient client, byte[] encryptedData)
        {
            var packet = DataPacketModel.DataReturnResponse(encryptedData);
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                return InternalErrorResponse();
            }

            packet.PacketSign = packetSignature;
            _expectedAcks[packet.PacketIdentifier] = packet;
            return packet;
        }

        public DataPacketModel AckResponse(byte[] packetIdentifier)
        {
            var packet = DataPacketModel.AckResponse(packetIdentifier);

            return packet;
        }


        // For read
        // packet has the following format
        /*
        char dataType;
        char timeSetting;
        char returnClosestInstead;
        long TimeStamp1; // Unix Time in seconds
        long TimeStamp2; // Unix Time in seconds

        dataType: 
        0 or Temp for Temperature
        1 or Stir for Stirring RPM
        2 or PH for PH
        3 or DevS for Device Status
        4 or Erro for Errors

        timeSetting:
        0 for only return last entry
        1 for specific timestamp
        2 for range of timestamps

        returnClosestInstead:
        0 for return exact timestamp
        1 for return closest timestamp

        A read request causes a store response in return
        */

        // For write
        // packet has the following format
        /*
        char dataType;
        long dataCount;

        dataType: 
        0 or Temp for Temperature
        1 or Stir for Stirring RPM
        2 or PH for PH
        3 or DevS for Device Status
        4 or Erro for Errors

        dataCount:
        Amount of entries of data in Timestamp + data format.

        A store response does not cause any response  
        */

        public async Task<DataPacketModel?> HandleBinaryData(int command, RegisteredClient client, byte[] pureData, DataPacketModel packet)
        {
            DataPacketModel? packetModel = null;

            if (command == 0)
            {
                if (_expectedAcks.TryGetValue(packet.PacketIdentifier, out _))
                {
                    _expectedAcks.TryRemove(packet.PacketIdentifier, out _);
                }
            }
            else if (command == 1)
            {
                var dataTypeToRead = pureData[0];
                var timeSetting = pureData[1];
                var returnClosestInstead = Convert.ToBoolean(pureData[2]);
                var timeStamp1 = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(pureData[3..11])).UtcDateTime;
                var timeStamp2 = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(pureData[11..19])).UtcDateTime;

                byte[] dataBytes = new byte[1];

                if (timeSetting == 0)
                {
                    if (dataTypeToRead == 0)
                    {
                        // Read last temperature
                        var data = await _dataService.ReadRawDataFromDbAsyc<TempData>(client);
                        byte dataFlag = 0b00010000; // store binary data
                        if (data == null)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            BitConverter.GetBytes(data.Temperature));
                    }
                    else if (dataTypeToRead == 1)
                    {
                        // Read last stirring RPM
                        var data = await _dataService.ReadRawDataFromDbAsyc<StirringData>(client);
                        byte dataFlag = 0b00010000; // store binary data
                        if (data == null)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            BitConverter.GetBytes(data.RPM));
                    }
                    else if (dataTypeToRead == 2)
                    {
                        // Read last PH
                        var data = await _dataService.ReadRawDataFromDbAsyc<pHData>(client);
                        byte dataFlag = 0b00010000; // store binary data
                        if (data == null)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            BitConverter.GetBytes(data.pH));
                    }
                    else if (dataTypeToRead == 3)
                    {
                        // Read last device status
                        var data = await _dataService.ReadRawDataFromDbAsyc<DeviceStatusData>(client);
                        byte dataFlag = 0b00010000; // store binary data
                        if (data == null)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            Encoding.UTF8.GetBytes(data.Status));
                    }
                    else if (dataTypeToRead == 4)
                    {
                        // Read last error
                        var data = await _dataService.ReadRawDataFromDbAsyc<ErrorData>(client);
                        byte dataFlag = 0b00010000; // store binary data
                        if (data == null)
                        {
                            return DataPacketModel.InternalErrorResponse();
                        }

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            BitConverter.GetBytes(data.Error));
                    }
                }
                else if (timeSetting == 1)
                {
                    if (dataTypeToRead == 0)
                    {
                        // Read last temperature
                        var data = await _dataService.ReadRawDataFromDbAsyc<TempData>(client, timeStamp1, returnClosestInstead);
                        byte dataFlag = 0b00010000; // store binary data
                        if (data == null)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            BitConverter.GetBytes(data.Temperature));
                    }
                    else if (dataTypeToRead == 1)
                    {
                        // Read last stirring RPM
                        var data = await _dataService.ReadRawDataFromDbAsyc<StirringData>(client, timeStamp1, returnClosestInstead);
                        byte dataFlag = 0b00010000; // store binary data
                        if (data == null)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            BitConverter.GetBytes(data.RPM));
                    }
                    else if (dataTypeToRead == 2)
                    {
                        // Read last PH
                        var data = await _dataService.ReadRawDataFromDbAsyc<pHData>(client, timeStamp1, returnClosestInstead);
                        byte dataFlag = 0b00010000; // store binary data
                        if (data == null)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            BitConverter.GetBytes(data.pH));
                    }
                    else if (dataTypeToRead == 3)
                    {
                        // Read last device status
                        var data = await _dataService.ReadRawDataFromDbAsyc<DeviceStatusData>(client, timeStamp1, returnClosestInstead);
                        byte dataFlag = 0b00010000; // store binary data
                        if (data == null)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            Encoding.UTF8.GetBytes(data.Status));
                    }
                    else if (dataTypeToRead == 4)
                    {
                        // Read last error
                        var data = await _dataService.ReadRawDataFromDbAsyc<ErrorData>(client, timeStamp1, returnClosestInstead);
                        byte dataFlag = 0b00010000; // store binary data
                        if (data == null)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            BitConverter.GetBytes(data.Error));
                    }
                }
                else if (timeSetting == 2)
                {
                    if (dataTypeToRead == 0)
                    {
                        // Read last temperature
                        var dataList = await _dataService.ReadRawDataFromDbAsyc<TempData>(client, timeStamp1, timeStamp2);
                        byte dataFlag = 0b00010000; // store binary data
                        if (dataList == null || dataList.Count == 0)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes[0] = dataFlag;
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, new byte[] { dataTypeToRead });
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes((long)dataList.Count));
                        foreach (var data in dataList)
                        {
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()));
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(data.Temperature));
                        }
                    }
                    else if (dataTypeToRead == 1)
                    {
                        // Read last stirring RPM
                        var dataList = await _dataService.ReadRawDataFromDbAsyc<StirringData>(client, timeStamp1, timeStamp2);
                        byte dataFlag = 0b00010000; // store binary data
                        if (dataList == null || dataList.Count == 0)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes[0] = dataFlag;
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, new byte[] { dataTypeToRead });
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes((long)dataList.Count));
                        foreach (var data in dataList)
                        {
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()));
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(data.RPM));
                        }
                    }
                    else if (dataTypeToRead == 2)
                    {
                        // Read last PH
                        var dataList = await _dataService.ReadRawDataFromDbAsyc<pHData>(client, timeStamp1, timeStamp2);
                        byte dataFlag = 0b00010000; // store binary data
                        if (dataList == null || dataList.Count == 0)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes[0] = dataFlag;
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, new byte[] { dataTypeToRead });
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes((long)dataList.Count));
                        foreach (var data in dataList)
                        {
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()));
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(data.pH));
                        }
                    }
                    else if (dataTypeToRead == 3)
                    {
                        // Read last device status
                        var dataList = await _dataService.ReadRawDataFromDbAsyc<DeviceStatusData>(client, timeStamp1, timeStamp2);
                        byte dataFlag = 0b00010000; // store binary data
                        if (dataList == null || dataList.Count == 0)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes[0] = dataFlag;
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, new byte[] { dataTypeToRead });
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes((long)dataList.Count));
                        foreach (var data in dataList)
                        {
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()));
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, Encoding.UTF8.GetBytes(data.Status));
                        }
                    }
                    else if (dataTypeToRead == 4)
                    {
                        // Read last error
                        var dataList = await _dataService.ReadRawDataFromDbAsyc<ErrorData>(client, timeStamp1, timeStamp2);
                        byte dataFlag = 0b00010000; // store binary data
                        if (dataList == null || dataList.Count == 0)
                        {
                            return InternalErrorResponse();
                        }

                        dataBytes[0] = dataFlag;
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, new byte[] { dataTypeToRead });
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes((long)dataList.Count));
                        foreach (var data in dataList)
                        {
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()));
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(data.Error));
                        }
                    }
                }

                if (dataBytes == null)
                {
                    return InvalidPacketResponse();
                }

                var encryptedDataBytes = PacketService.Encrypt(dataBytes, client.EncryptionKey, client.EncryptionIV);
                packetModel = DataReturnResponse(client, encryptedDataBytes);
            }
            else if (command == 2)
            {
                var dataTypeToStore = pureData[0];
                long dataCount = BitConverter.ToInt64(pureData, 1);

                try
                {
                    if (dataTypeToStore == 0)
                    {
                        for (int i = 9; i < dataCount * 16; i += 16) // data starts from 9th byte and each packet is 16 bytes
                        {
                            var timeStamp = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(pureData, i)).UtcDateTime;
                            var temperature = BitConverter.ToDouble(pureData, i + 8);

                            var tempData = new TempData()
                            {
                                Client = client,
                                ClientId = client.Id,
                                TimeStamp = timeStamp,
                                Temperature = temperature
                            };

                            await _dataService.StoreDataToDbAsync<TempData>(client, tempData);
                        }
                    }
                    else if (dataTypeToStore == 1)
                    {
                        for (int i = 9; i < dataCount * 16; i += 16) // data starts from 9th byte and each packet is 16 bytes
                        {
                            var timeStamp = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(pureData, i)).UtcDateTime;
                            var rpm = BitConverter.ToDouble(pureData, i + 8);

                            var rpmData = new StirringData()
                            {
                                Client = client,
                                ClientId = client.Id,
                                TimeStamp = timeStamp,
                                RPM = rpm
                            };

                            await _dataService.StoreDataToDbAsync<StirringData>(client, rpmData);
                        }

                    }
                    else if (dataTypeToStore == 2)
                    {
                        for (int i = 9; i < dataCount * 16; i += 16) // data starts from 9th byte and each packet is 16 bytes
                        {
                            var timeStamp = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(pureData, i)).UtcDateTime;
                            var ph = BitConverter.ToDouble(pureData, i + 8);

                            var phData = new pHData()
                            {
                                Client = client,
                                ClientId = client.Id,
                                TimeStamp = timeStamp,
                                pH = ph
                            };

                            await _dataService.StoreDataToDbAsync<pHData>(client, phData);
                        }
                    }
                    else if (dataTypeToStore == 3)
                    {
                        int separateIndex = 9;
                        for (int i = 9; i < pureData.Length; i++) // data starts from 9th byte and each packet is of variable size ending with a semicolon
                        {
                            if (pureData[i] == ';')
                            {
                                var timeStamp = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(pureData, separateIndex)).UtcDateTime;
                                var status = Encoding.UTF8.GetString(pureData[separateIndex..(i + 1)]); // include the ; into the status

                                var deviceStatusData = new DeviceStatusData()
                                {
                                    Client = client,
                                    ClientId = client.Id,
                                    TimeStamp = timeStamp,
                                    Status = status
                                };

                                await _dataService.StoreDataToDbAsync<DeviceStatusData>(client, deviceStatusData);
                                separateIndex = i + 1;
                            }
                        }
                    }
                    else if (dataTypeToStore == 4)
                    {
                        for (int i = 9; i < dataCount * 12; i += 12) // data starts from 9th byte and each packet is 12 bytes
                        {
                            var timeStamp = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(pureData, i)).UtcDateTime;
                            var error = BitConverter.ToInt32(pureData, i + 8);

                            var errorData = new ErrorData()
                            {
                                Client = client,
                                ClientId = client.Id,
                                TimeStamp = timeStamp,
                                Error = error
                            };

                            await _dataService.StoreDataToDbAsync<ErrorData>(client, errorData);
                        }
                    }
                }
                catch
                {
                    return InvalidPacketResponse();
                }

                packetModel = AckResponse(packet.PacketIdentifier);
            }

            if (packetModel != null)
            {
                var packetSignature = _packetService.SignPacket(packetModel, client.SignatureKey);
                if (packetSignature == null)
                {
                    return InternalErrorResponse();
                }
                packetModel.PacketSign = packetSignature;
            }

            return packetModel;
        }


        // Lets see how to parse data, data is any request or reponse from the client
        // Packets are sent in the following format:
        // 1 bytes packet flag
        // rest is data in packet data type format
        // packet flag: 0b00000000
        // first bit is packet type, 0 for data and 1 for error
        // second and third bits are data types 0 for binary, 1, 2 and 3 are reserved
        // fourth to sixth bits are command 0 for ack, 1 for get, 2 for store, 3 to 7 are reserved
        // seventh and eighth bits are reserved

        public async Task<DataPacketModel?> HandleDataAsync(RegisteredClient client, byte[] data, DataPacketModel packet)
        {
            // We are assuming that the packet is confirmed and the data exists and client is authorized

            if (data.Length < 1)
            {
                return InvalidPacketResponse();
            }

            var flag = data[0];
            var packetType = (flag & 0b00000001);
            var dataType = (flag & 0b00000110) >> 1;
            var command = (flag & 0b00111000) >> 3;

            if (packetType == 1)
            {
                return InvalidPacketResponse();
            }

            if (dataType == 0)
            {
                var pureData = data.Length > 1 ? data[1..] : new byte[0];
                return await HandleBinaryData(command, client, pureData, packet);
            }
            else
            {
                return InvalidPacketResponse();
            }
        }

        public async Task<DataPacketModel?> HandleErrorAsync(RegisteredClient client, PacketError error, byte[] data, DataPacketModel packet)
        {
            return AckResponse(packet.PacketIdentifier); // just ack the error for now
        }
    }
}
