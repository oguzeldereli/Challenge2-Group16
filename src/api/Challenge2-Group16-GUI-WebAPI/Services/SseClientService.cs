using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class SseClientService
    {
        private readonly ConcurrentDictionary<Guid, HttpResponse> _sseClients = new();

        public async Task<Guid> AddClient(HttpResponse response)
        {
            Guid guid = Guid.NewGuid();
            response.ContentType = "text/event-stream";
            response.Headers["Cache-Control"] = "no-cache";
            if (response.HttpContext.Request.Protocol == "HTTP/1.1")
            {
                response.Headers["Connection"] = "keep-alive";
            }
            await response.Body.FlushAsync();

            _sseClients.TryAdd(guid, response);
                
            return guid;
        }

        public void RemoveClient(Guid guid)
        {
            _sseClients.TryRemove(guid, out _);
        }

        public async Task PublishAsync(string eventType, string data)
        {
            foreach (var kvp in _sseClients)
            {
                var client = kvp.Value;
                try
                {
                    await client.WriteAsync($"event: {eventType}\n");
                    await client.WriteAsync($"data: {data}\n\n");
                    await client.Body.FlushAsync();
                }
                catch (Exception)
                {
                    _sseClients.TryRemove(kvp.Key, out _);
                }
            }
        }

        public async Task PublishAsJsonAsync(string eventType, object data)
        {
            foreach (var kvp in _sseClients)
            {
                var client = kvp.Value;
                try
                {
                    var message = $"event: {eventType}\ndata: {JsonSerializer.Serialize(data)}\n\n";
                    await client.WriteAsync(message);
                    await client.Body.FlushAsync();
                }
                catch (Exception)
                {
                    _sseClients.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}