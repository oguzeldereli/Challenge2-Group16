﻿using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using System.Security.Cryptography;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class RegisteredClientService
    {
        private readonly ApplicationDbContext _context;

        public RegisteredClientService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RegisteredClient?> RegisterClientAsync(byte[] clientIdentifier, ClientType type)
        {
            if(type == ClientType.@public) // refuse public clients, only confidential clients are allowed
            {
                return null;
            }

            if (_context.Clients.Any(c => c.Identifier.SequenceEqual(clientIdentifier)))
            {
                return null;
            }

            RegisteredClient? client = RegisteredClient.Create(clientIdentifier, type);
            if(client == null)
            {
                return null;
            }


            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return client;
        }

        public async Task<byte[]?> AuthorizeClientAsync(byte[] clientIdentifier, byte[] clientSecret)
        {
            if(clientIdentifier.Length != 16 || clientSecret.Length != 32)
            {
                return null;
            }

            var registeredClient = _context.Clients.FirstOrDefault(c => c.Identifier.SequenceEqual(clientIdentifier) && c.Secret.SequenceEqual(clientSecret));
            if (registeredClient == null)
            {
                return null;
            }

            var authToken = new byte[16];
            RandomNumberGenerator.Fill(authToken);
            registeredClient.TemporaryAuthToken = authToken;
            await _context.SaveChangesAsync();

            return authToken;
        }

        public async Task<bool> RevokeClientAsync(byte[] authToken)
        {
            if (authToken.Length != 16)
            {
                return false;
            }

            var registeredClient = _context.Clients.FirstOrDefault(c => c.TemporaryAuthToken == authToken);
            if (registeredClient == null)
            {
                return false;
            }

            registeredClient.TemporaryAuthToken = new byte[16];
            await _context.SaveChangesAsync();
            return true;
        }
    }
}