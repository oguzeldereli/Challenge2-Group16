using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class SseClientService
    {
        private readonly ConcurrentDictionary<Guid, HttpResponse> _sseClients = new();

        public Guid AddClient(HttpResponse response)
        {
            Guid guid = Guid.NewGuid();
            _sseClients.TryAdd(guid, response);

            return guid;
        }

        public void RemoveClient(Guid guid)
        {
            _sseClients.TryRemove(guid, out _);
        }

        public async Task PublishAsync(string eventType, string data)
        {
            foreach (var client in _sseClients.Values)
            {
                await client.WriteAsync($"event: {eventType}\n");
                await client.WriteAsync($"data: {data}\n\n");
                await client.Body.FlushAsync();
            }
        }

        public async Task PublishAsJsonAsync(string eventType, object data)
        {
            foreach (var client in _sseClients.Values)
            {
                await client.WriteAsync($"event: {eventType}\n");
                await client.WriteAsync($"data: ");
                await client.WriteAsJsonAsync(data);
                await client.WriteAsync($"\n\n");
                await client.Body.FlushAsync();
            }
        }
    }
}