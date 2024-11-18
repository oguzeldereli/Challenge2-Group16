using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Interfaces;
using Challenge2_Group16_GUI_WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class DataService
    {
        private readonly ApplicationDbContext _context;

        public DataService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Write Raw Data

        public async Task StoreDataToDbAsync<T>(RegisteredClient client, T data)
        {
            if (data == null)
                return;

            if(typeof(T) == typeof(TempData) && data as TempData != null)
            {
                client.TempData.Add(data as TempData);
            }
            else if(typeof(T) == typeof(StirringData) && data as StirringData != null)
            {
                client.StirringData.Add(data as StirringData);
            }
            else if(typeof(T) == typeof(pHData) && data as pHData != null)
            {
                client.pHData.Add(data as pHData);
            }
            else if(typeof(T) == typeof(DeviceStatusData) && data as DeviceStatusData != null)
            {
                client.DeviceStatusData.Add(data as DeviceStatusData);
            }
            else if(typeof(T) == typeof(LogData) && data as LogData != null)
            {
                client.ErrorData.Add(data as LogData);
            }
            else
            {
                return;
            }
            await _context.SaveChangesAsync();

        }

        // Read Raw Data

        public async Task<T?> ReadRawDataFromDbAsyc<T>(RegisteredClient client) where T : class, IDataPacket
        {
            var set = _context.GetDbSet<T>();
            if (set == null)
                return null;

            return await set.OrderByDescending(x => x.TimeStamp).FirstOrDefaultAsync(x => x.ClientId == client.Id);
        }

        public async Task<T?> ReadRawDataFromDbAsyc<T>(RegisteredClient client, DateTime AtTime, bool returnClosestInstead = true) where T : class, IDataPacket
        {
            var set = _context.GetDbSet<T>();
            if (set == null)
                return null;

            if (returnClosestInstead)
            {
                return await set
                    .Where(x => x.ClientId == client.Id)
                    .OrderBy(x => Math.Abs((x.TimeStamp - AtTime).TotalSeconds))
                    .FirstOrDefaultAsync();
            }

            return await set
                .FirstOrDefaultAsync(x => x.ClientId == client.Id && x.TimeStamp == AtTime);
        }

        public async Task<List<T>?> ReadRawDataFromDbAsyc<T>(RegisteredClient client, DateTime StartTime, DateTime EndTime) where T : class, IDataPacket
        {
            var set = _context.GetDbSet<T>();
            if (set == null)
                return null;

            return await set
                .Where(x => x.ClientId == client.Id && x.TimeStamp >= StartTime && x.TimeStamp <= EndTime)
                .ToListAsync();
        }

        // Aggregated Data

        private async Task<List<T2>?> DeaggregateDataAsync<T1, T2>(RegisteredClient client, T1 aggregateData) where T1 : class, IAggregateDataPacket where T2 : class, IDataPacket
        {
            var deaggregatedData = new List<T2>();

            int dataCount = aggregateData.DataTimeStamps.Length / 8;

            if (typeof(T1) == typeof(TempAggregateData) && typeof(T2) == typeof(TempData))
            {
                var tempDataAggregate = aggregateData as TempAggregateData;
                for (int i = 0; i < dataCount; i++)
                {
                    deaggregatedData.Add(new TempData
                    {
                        TimeStamp = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(tempDataAggregate?.DataTimeStamps[(i * 8)..(i * 8 + 8)] ?? new byte[8])).UtcDateTime,
                        Temperature = Convert.ToDouble(tempDataAggregate?.TemperatureAggregate[(i * 8 + 8)..(i * 8 + 16)] ?? new byte[8]),
                        ClientId = client.Id
                    } as T2);
                }
            }
            else if (typeof(T1) == typeof(StirringAggregateData) && typeof(T2) == typeof(StirringData))
            {
                var stirringAggregateData = aggregateData as StirringAggregateData;
                for (int i = 0; i < dataCount; i++)
                {
                    deaggregatedData.Add(new StirringData
                    {
                        TimeStamp = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(stirringAggregateData?.DataTimeStamps[(i * 8)..(i * 8 + 8)] ?? new byte[8])).UtcDateTime,
                        RPM = Convert.ToDouble(stirringAggregateData?.RPMAggregate[(i * 8 + 8)..(i * 8 + 16)] ?? new byte[8]),
                        ClientId = client.Id
                    } as T2);
                }
            }
            else if (typeof(T1) == typeof(pHAggregateData) && typeof(T2) == typeof(pHData))
            {
                var pHAggregateData = aggregateData as pHAggregateData;
                for (int i = 0; i < dataCount; i++)
                {
                    deaggregatedData.Add(new pHData
                    {
                        TimeStamp = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(pHAggregateData?.DataTimeStamps[(i * 8)..(i * 8 + 8)] ?? new byte[8])).UtcDateTime,
                        pH = Convert.ToDouble(pHAggregateData?.pHAggregate[(i * 8 + 8)..(i * 8 + 16)] ?? new byte[8]),
                        ClientId = client.Id
                    } as T2);
                }
            }
            else if (typeof(T1) == typeof(DeviceStatusAggregateData) && typeof(T2) == typeof(DeviceStatusData))
            {
                var deviceStatusAggregateData = aggregateData as DeviceStatusAggregateData;
                for (int i = 0; i < dataCount; i++)
                {
                    deaggregatedData.Add(new DeviceStatusData
                    {
                        TimeStamp = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(deviceStatusAggregateData?.DataTimeStamps[(i * 8)..(i * 8 + 8)] ?? new byte[8])).UtcDateTime,
                        Status = Convert.ToUInt32(deviceStatusAggregateData?.StatusAggregate[(i * 4 + 8)..(i * 4 + 12)] ?? new byte[4]),
                        ClientId = client.Id
                    } as T2);
                }
            }
            else if (typeof(T1) == typeof(LogAggregateData) && typeof(T2) == typeof(LogData))
            {
                var logAggregateData = aggregateData as LogAggregateData;
                var lastColonIndex = -1;
                var lastSemiColonIndex = -1;
                for (int i = 0; i < dataCount; i++)
                {
                    var cIndex = logAggregateData?.Logs.IndexOf(':', lastColonIndex + 1) ?? lastColonIndex;
                    var scIndex = logAggregateData?.Logs.IndexOf(';', lastSemiColonIndex + 1) ?? lastSemiColonIndex;
                    
                    deaggregatedData.Add(new LogData
                    {
                        TimeStamp = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(logAggregateData?.DataTimeStamps[(i * 8)..(i * 8 + 8)] ?? new byte[8])).UtcDateTime,
                        Type = logAggregateData?.Logs.Substring(lastSemiColonIndex + 1, cIndex - lastSemiColonIndex - 1) ?? "",
                        Message = logAggregateData?.Logs.Substring(cIndex + 1, scIndex - cIndex - 1) ?? "",
                        ClientId = client.Id
                    } as T2);

                    lastColonIndex = cIndex;
                    lastSemiColonIndex = scIndex;
                }
            }
            else
            {
                return null;
            }

            return deaggregatedData;
        }

        public async Task<List<T2>?> ReadAggregateDataFromDbAsyc<T1, T2>(RegisteredClient client) where T1 : class, IAggregateDataPacket where T2 : class, IDataPacket
        {
            var set = _context.GetDbSet<T1>();
            if (set == null)
                return null;

            // this returns last day's aggregated data
            var aggregateData = await set.OrderByDescending(x => x.TimeStamp).FirstOrDefaultAsync(x => x.ClientId == client.Id);
            if (aggregateData == null)
            {
                return null;
            }

            return await DeaggregateDataAsync<T1, T2>(client, aggregateData);
        }

        public async Task<List<T2>?> ReadAggregateDataFromDbAsyc<T1, T2>(RegisteredClient client, DateTime AtTime, bool returnClosestInstead = true) where T1 : class, IAggregateDataPacket where T2 : class, IDataPacket
        {
            var set = _context.GetDbSet<T1>();
            if (set == null)
                return null;

            DateTime startOfDay = AtTime.Date;

            T1? aggregateData = null;
            if (returnClosestInstead)
            {
                aggregateData = await set
                    .Where(x => x.ClientId == client.Id)
                    .OrderBy(x => Math.Abs((x.TimeStamp.Date - startOfDay).TotalDays))
                    .FirstOrDefaultAsync();
            }
            else
            {
                aggregateData = await set
                    .FirstOrDefaultAsync(x => x.ClientId == client.Id && x.TimeStamp.Date == startOfDay);
            }

            if (aggregateData == null)
            {
                return null;
            }

            return await DeaggregateDataAsync<T1, T2>(client, aggregateData);
        }

        public async Task<List<T2>?> ReadAggregateDataFromDbAsyc<T1, T2>(RegisteredClient client, DateTime StartTime, DateTime EndTime) where T1 : class, IAggregateDataPacket where T2 : class, IDataPacket
        {
            var set = _context.GetDbSet<T1>();
            if (set == null)
                return null;

            DateTime startOfDay = StartTime.Date;
            DateTime endOfDay = EndTime.Date;

            var aggregateData = await set
                .Where(x => x.ClientId == client.Id &&
                    x.TimeStamp.Date >= startOfDay &&
                    x.TimeStamp.Date <= endOfDay)
                .ToListAsync();

            if (aggregateData == null)
            {
                return null;
            }

            var deAggregatedList = new List<T2>();
            foreach (var aggregated in aggregateData)
            {
                deAggregatedList.AddRange(await DeaggregateDataAsync<T1, T2>(client, aggregated) ?? new List<T2>());
            }

            return deAggregatedList;
        }
    }
}
