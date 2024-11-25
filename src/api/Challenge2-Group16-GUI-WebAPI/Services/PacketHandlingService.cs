using Azure;
using Challenge2_Group16_GUI_WebAPI.Interfaces;
using Challenge2_Group16_GUI_WebAPI.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore.Query.Internal;
using NuGet.Packaging;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class PacketHandlingService
    {
        private readonly DataService _dataService;
        private readonly PacketService _packetService;
        private readonly IConfiguration _configuration;
        private readonly SseClientService _sseClientService;
        private readonly WebSocketManagerService _webSocketManagerService;
        private readonly ChainService _chainService;

        public PacketHandlingService(
            DataService dataService,
            PacketService packetService,
            IConfiguration configuration,
            SseClientService sseClientService,
            WebSocketManagerService webSocketManagerService,
            ChainService chainService)
        {
            _dataService = dataService;
            _packetService = packetService;
            _configuration = configuration;
            _sseClientService = sseClientService;
            _webSocketManagerService = webSocketManagerService;
            _chainService = chainService;
        }

        public async Task MalformedPacketResponse(string socketId)
        {
            var packet = DataPacketModel.MalformedPacket();
            await _webSocketManagerService.SendAsync(socketId, packet.GetPacket());
            await _chainService.ExpectAck(packet.ChainIdentifier);
        }

        public async Task InvalidPacketResponse(string socketId)
        {
            var packet = DataPacketModel.InvalidPacket();
            await _webSocketManagerService.SendAsync(socketId, packet.GetPacket());
            await _chainService.ExpectAck(packet.ChainIdentifier);
        }

        public async Task InternalErrorResponse(string socketId)
        {
            var packet = DataPacketModel.InternalError();
            await _webSocketManagerService.SendAsync(socketId, packet.GetPacket());
            await _chainService.ExpectAck(packet.ChainIdentifier);
        }

        public async Task RegisterResponse(string socketId, RegisteredClient client, byte[] secret, byte[] signatureKey)
        {
            var packet = DataPacketModel.RegisterResponse(secret, signatureKey);
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                await InternalErrorResponse(socketId);
                return;
            }

            packet.PacketSignature = packetSignature;

            await _webSocketManagerService.SendAsync(socketId, packet.GetPacket());
            await _chainService.ExpectAck(packet.ChainIdentifier);
        }

        public async Task AuthResponse(string socketId, RegisteredClient client, byte[] authToken)
        {
            var packet = DataPacketModel.AuthResponse(authToken);
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                await InternalErrorResponse(socketId);
                return;
            }

            packet.PacketSignature = packetSignature;

            await _webSocketManagerService.SendAsync(socketId, packet.GetPacket());
            await _chainService.ExpectAck(packet.ChainIdentifier);
        }

        public async Task RevokeAuthResponse(string socketId, RegisteredClient client)
        {
            var packet = DataPacketModel.RevokeAuthResponse();
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                await InternalErrorResponse(socketId);
                return;
            }

            packet.PacketSignature = packetSignature;
            await _webSocketManagerService.SendAsync(socketId, packet.GetPacket());
            await _chainService.ExpectAck(packet.ChainIdentifier);
        }

        public async Task DataReturnResponse(string socketId, RegisteredClient client, byte[] data)
        {
            var packet = DataPacketModel.Data(data);
            var packetSignature = _packetService.SignPacket(packet, client.SignatureKey);
            if (packetSignature == null)
            {
                await InternalErrorResponse(socketId);
                return;
            }

            packet.PacketSignature = packetSignature;
            await _webSocketManagerService.SendAsync(socketId, packet.GetPacket());
            await _chainService.ExpectAck(packet.ChainIdentifier);
        }

        public async Task AckResponse(string socketId, byte[] packetIdentifier)
        {
            var packet = DataPacketModel.Ack(packetIdentifier);
            await _webSocketManagerService.SendAsync(socketId, packet.GetPacket());
            await _chainService.ExpectAck(packet.ChainIdentifier);
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
            4 for Logs

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
        0 for Temperature
        1 for Stirring RPM
        2 for PH
        3 for Device Status
        4 for Logs  

        dataCount:
        Amount of entries of data in Timestamp + data format.

        A store response causes an ack  
        */
        /*
         For commands:
        byte Command;
        byte[] data;
         */


        public async Task HandleBinaryData(string socketId, int command, RegisteredClient client, byte[] pureData, DataPacketModel packet)
        {
            DataPacketModel? packetModel = null;

            if (command == 0)
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
                            await InternalErrorResponse(socketId);
                            return;
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
                            await InternalErrorResponse(socketId);
                            return;
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
                            await InternalErrorResponse(socketId);
                            return;
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
                            await InternalErrorResponse(socketId);
                            return;
                        }

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            BitConverter.GetBytes(data.Status),
                            BitConverter.GetBytes(data.TempTarget),
                            BitConverter.GetBytes(data.PhTarget),
                            BitConverter.GetBytes(data.RPMTarget));
                    }
                    else if (dataTypeToRead == 4)
                    {
                        // Read last log
                        var data = await _dataService.ReadRawDataFromDbAsyc<LogData>(client);
                        byte dataFlag = 0b00010000; // store binary data
                        if (data == null)
                        {
                            await InternalErrorResponse(socketId);
                            return;
                        }

                        var type = Encoding.UTF8.GetBytes(data.Type);
                        var typeLength = BitConverter.GetBytes(type.Length);
                        var message = Encoding.UTF8.GetBytes(data.Message);
                        var messageLength = BitConverter.GetBytes(message.Length);

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            typeLength,
                            type,
                            messageLength,
                            message);
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
                            await InternalErrorResponse(socketId);
                            return;
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
                            await InternalErrorResponse(socketId);
                            return;
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
                            await InternalErrorResponse(socketId);
                            return;
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
                            await InternalErrorResponse(socketId);
                            return;
                        }

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            BitConverter.GetBytes(data.Status),
                            BitConverter.GetBytes(data.TempTarget),
                            BitConverter.GetBytes(data.PhTarget),
                            BitConverter.GetBytes(data.RPMTarget));
                    }
                    else if (dataTypeToRead == 4)
                    {
                        // Read last error
                        var data = await _dataService.ReadRawDataFromDbAsyc<LogData>(client, timeStamp1, returnClosestInstead);
                        byte dataFlag = 0b00010000; // store binary data
                        if (data == null)
                        {
                            await InternalErrorResponse(socketId);
                            return;
                        }

                        var type = Encoding.UTF8.GetBytes(data.Type);
                        var typeLength = BitConverter.GetBytes(type.Length);
                        var message = Encoding.UTF8.GetBytes(data.Message);
                        var messageLength = BitConverter.GetBytes(message.Length);

                        dataBytes = DataPacketModel.CombineByteArrays(
                            new byte[2] { dataFlag, dataTypeToRead },
                            BitConverter.GetBytes((long)1),
                            BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()),
                            typeLength,
                            type,
                            messageLength,
                            message);
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
                            await InternalErrorResponse(socketId);
                            return;
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
                            await InternalErrorResponse(socketId);
                            return;
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
                            await InternalErrorResponse(socketId);
                            return;
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
                            await InternalErrorResponse(socketId);
                            return;
                        }

                        dataBytes[0] = dataFlag;
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, new byte[] { dataTypeToRead });
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes((long)dataList.Count));
                        foreach (var data in dataList)
                        {
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()));
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(data.Status));
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(data.TempTarget));
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(data.PhTarget));
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(data.RPMTarget));
                        }
                    }
                    else if (dataTypeToRead == 4)
                    {
                        // Read last log
                        var dataList = await _dataService.ReadRawDataFromDbAsyc<LogData>(client, timeStamp1, timeStamp2);
                        byte dataFlag = 0b00010000; // store binary data
                        if (dataList == null || dataList.Count == 0)
                        {
                            await InternalErrorResponse(socketId);
                            return;
                        }

                        dataBytes[0] = dataFlag;
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, new byte[] { dataTypeToRead });
                        dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes((long)dataList.Count));
                        foreach (var data in dataList)
                        {
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(new DateTimeOffset(data.TimeStamp).ToUnixTimeSeconds()));
                            var type = Encoding.UTF8.GetBytes(data.Type);
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(type.Length));
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, type);
                            var message = Encoding.UTF8.GetBytes(data.Message);
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, BitConverter.GetBytes(message.Length));
                            dataBytes = DataPacketModel.AppendByteArrays(dataBytes, message);
                        }
                    }
                }

                if (dataBytes == null)
                {
                    await InternalErrorResponse(socketId);
                    return;
                }

                await DataReturnResponse(socketId, client, dataBytes);
            }
            else if (command == 1)
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

                            await _sseClientService.PublishAsJsonAsync("data", new
                            {
                                client_id = client.Identifier,
                                data = new
                                {
                                    data_type = "temperature",
                                    time_stamp = new DateTimeOffset(timeStamp).ToUnixTimeSeconds(),
                                    data = temperature
                                }
                            });
                            await _dataService.StoreDataToDbAsync<TempData>(client, tempData);
                            _chainService.RespondChain(packet.ChainIdentifier, tempData);
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

                            await _sseClientService.PublishAsJsonAsync("data", new
                            {
                                client_id = client.Identifier,
                                data = new
                                {
                                    data_type = "rpm",
                                    time_stamp = new DateTimeOffset(timeStamp).ToUnixTimeSeconds(),
                                    data = rpm
                                }
                            });
                            await _dataService.StoreDataToDbAsync<StirringData>(client, rpmData);
                            _chainService.RespondChain(packet.ChainIdentifier, rpmData);
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

                            await _sseClientService.PublishAsJsonAsync("data", new
                            {
                                client_id = client.Identifier,
                                data = new
                                {
                                    data_type = "ph",
                                    time_stamp = new DateTimeOffset(timeStamp).ToUnixTimeSeconds(),
                                    data = ph
                                }
                            });
                            await _dataService.StoreDataToDbAsync<pHData>(client, phData);
                            _chainService.RespondChain(packet.ChainIdentifier, phData);
                        }
                    }
                    else if (dataTypeToStore == 3)
                    {
                        for (int i = 9; i < dataCount * 16; i += 24) // data starts from 9th byte and each packet is 16 bytes
                        {
                            var timeStamp = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(pureData, i)).UtcDateTime;
                            var status = BitConverter.ToUInt32(pureData, i + 8);
                            var tempTarget = BitConverter.ToUInt32(pureData, i + 12);
                            var phTarget = BitConverter.ToUInt32(pureData, i + 16);
                            var rpmTarget = BitConverter.ToUInt32(pureData, i + 20);

                            var statusData = new DeviceStatusData()
                            {
                                Client = client,
                                ClientId = client.Id,
                                TimeStamp = timeStamp,
                                Status = status,
                                TempTarget = tempTarget,
                                PhTarget = phTarget,
                                RPMTarget = rpmTarget
                            };

                            await _sseClientService.PublishAsJsonAsync("data", new
                            {
                                client_id = client.Identifier,
                                data = new
                                {
                                    data_type = "status",
                                    time_stamp = new DateTimeOffset(timeStamp).ToUnixTimeSeconds(),
                                    data = new { status, tempTarget, phTarget, rpmTarget }
                                }
                            });
                            await _dataService.StoreDataToDbAsync<DeviceStatusData>(client, statusData);
                            _chainService.RespondChain(packet.ChainIdentifier, statusData);
                        }
                    }
                    else if (dataTypeToStore == 4)
                    {
                        for (int i = 9; i < pureData.Length - 9; i++) // data starts from 9th byte and each packet is unknown bytes
                        {
                            var timeStamp = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToInt64(pureData, i)).UtcDateTime;
                            i += 8;
                            var typeLength = BitConverter.ToInt32(pureData, i);
                            i += 4;
                            var type = Encoding.UTF8.GetString(pureData, i, typeLength);
                            i += typeLength;
                            var messageLength = BitConverter.ToInt32(pureData, i);
                            i += 4;
                            var message = Encoding.UTF8.GetString(pureData, i, messageLength);
                            i += messageLength;

                            var logData = new LogData()
                            {
                                Client = client,
                                ClientId = client.Id,
                                TimeStamp = timeStamp,
                                Type = type,
                                Message = message
                            };

                            await _sseClientService.PublishAsJsonAsync("data", new
                            {
                                client_id = client.Identifier,
                                data = new
                                {
                                    data_type = "log",
                                    time_stamp = new DateTimeOffset(timeStamp).ToUnixTimeSeconds(),
                                    log_level = type,
                                    log_message = message
                                }
                            });
                            await _dataService.StoreDataToDbAsync<LogData>(client, logData);
                            _chainService.RespondChain(packet.ChainIdentifier, logData);
                        }
                    }
                }
                catch
                {
                    await InvalidPacketResponse(socketId);
                    return;
                }

                await AckResponse(socketId, packet.ChainIdentifier);
            }

            if (packetModel != null)
            {
                var packetSignature = _packetService.SignPacket(packetModel, client.SignatureKey);
                if (packetSignature == null)
                {
                    await InternalErrorResponse(socketId);
                    return;
                }
                packetModel.PacketSignature = packetSignature;
            }
        }

        // Lets see how to parse data, data is any request or reponse from the client
        // Packets are sent in the following format:
        // 1 bytes packet flag
        // rest is data in packet data type format
        // packet flag: 0b00000000
        // first bit is packet type, 0 for data, 1 is reserved
        // second and third bits are data types 0 for binary, 1, 2 and 3 are reserved
        // fourth to sixth bits are command 0 for get, 1 for store, 2 for command, 3 to 7 are reserved
        // seventh and eighth bits are reserved

        public async Task HandleDataAsync(string socketId, RegisteredClient client, byte[] data, DataPacketModel packet)
        {
            // We are assuming that the packet is confirmed and the data exists and client is authorized

            if (data.Length < 1)
            {
                await InvalidPacketResponse(socketId);
                return;
            }

            var flag = data[0];
            var packetType = (flag & 0b00000001);
            var dataType = (flag & 0b00000110) >> 1;
            var command = (flag & 0b00111000) >> 3;

            if (dataType == 0)
            {
                var pureData = data.Length > 1 ? data[1..] : new byte[0];
                await HandleBinaryData(socketId, command, client, pureData, packet);
            }
            else
            {
                await InvalidPacketResponse(socketId);
                return;
            }
        }

        public async Task HandleErrorAsync(string socketId, RegisteredClient client, PacketError error, byte[] data, DataPacketModel packet)
        {
            await _sseClientService.PublishAsJsonAsync("error", new 
            { 
                client_id = client.Identifier, 
                time_stamp = packet.SentAt,
                error = error 
            }); 

            await AckResponse(socketId, packet.ChainIdentifier); // just ack the error for now
        }
    }
}
