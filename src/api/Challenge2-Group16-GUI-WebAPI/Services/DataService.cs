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
                _context.Entry(client)
                    .Collection(b => b.TempData)
                    .Load();
                client.TempData.Add(data as TempData);
            }
            else if(typeof(T) == typeof(StirringData) && data as StirringData != null)
            {
                _context.Entry(client)
                    .Collection(b => b.StirringData)
                    .Load();
                client.StirringData.Add(data as StirringData);
            }
            else if(typeof(T) == typeof(pHData) && data as pHData != null)
            {
                _context.Entry(client)
                    .Collection(b => b.pHData)
                    .Load();
                client.pHData.Add(data as pHData);
            }
            else if(typeof(T) == typeof(DeviceStatusData) && data as DeviceStatusData != null)
            {
                _context.Entry(client)
                    .Collection(b => b.DeviceStatusData)
                    .Load();
                client.DeviceStatusData.Add(data as DeviceStatusData);
            }
            else if(typeof(T) == typeof(LogData) && data as LogData != null)
            {
                _context.Entry(client)
                    .Collection(b => b.ErrorData)
                    .Load();
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
    }
}
