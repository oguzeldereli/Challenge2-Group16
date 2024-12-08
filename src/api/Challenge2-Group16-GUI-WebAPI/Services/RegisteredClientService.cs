using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Signing;
using System.Security.Cryptography;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class RegisteredClientService
    {
        private readonly ApplicationDbContext _context;
        private readonly SseClientService _sseClientService;
        private readonly WebSocketManagerService _webSocketManagerService;

        public RegisteredClientService(ApplicationDbContext context,
            SseClientService sseClientService,
            WebSocketManagerService webSocketManagerService)
        {
            _context = context;
            _sseClientService = sseClientService;
            _webSocketManagerService = webSocketManagerService;
        }

        public async Task<RegisteredClient?> GetRegisteredClientAsync(byte[] authToken)
        {
            if (authToken.Length != 16)
            {
                return null;
            }

            return await _context.Clients.FirstOrDefaultAsync(c => c.TemporaryAuthToken.SequenceEqual(authToken));
        }

        public async Task<RegisteredClient?> RegisterClientAsync(byte[] clientIdentifier, ClientType type)
        {
            if (type == ClientType.@public) // refuse public clients, only confidential clients are allowed
            {
                return null;
            }

            var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.Identifier.SequenceEqual(clientIdentifier));
            if (existingClient != null)
            {
                return existingClient;
            }

            RegisteredClient? client = RegisteredClient.Create(clientIdentifier, type);
            if (client == null)
            {
                return null;
            }


            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return client;
        }

        public async Task<byte[]?> AuthorizeClientAsync(string socketId, byte[] clientIdentifier, byte[] clientSecret)
        {
            if (clientIdentifier.Length != 32 || clientSecret.Length != 32)
            {
                return null;
            }

            var registeredClient = await _context.Clients.FirstOrDefaultAsync(c => c.Identifier.SequenceEqual(clientIdentifier) && c.Secret.SequenceEqual(clientSecret));
            if (registeredClient == null)
            {
                return null;
            }

            var authToken = new byte[16];
            RandomNumberGenerator.Fill(authToken);
            registeredClient.TemporaryAuthToken = authToken;
            await _webSocketManagerService.BindClient(socketId, registeredClient);
            await _context.SaveChangesAsync();

            return authToken;
        }

        public async Task<bool> RevokeClientAsync(byte[] authToken)
        {
            if (authToken.Length != 16)
            {
                return false;
            }

            var registeredClient = await _context.Clients.FirstOrDefaultAsync(c => c.TemporaryAuthToken == authToken);
            if (registeredClient == null)
            {
                return false;
            }


            registeredClient.TemporaryAuthToken = new byte[16];
            await _webSocketManagerService.UnbindClient(registeredClient);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
