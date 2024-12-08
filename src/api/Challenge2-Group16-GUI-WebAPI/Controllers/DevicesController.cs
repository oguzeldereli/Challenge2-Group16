using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using Challenge2_Group16_GUI_WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Challenge2_Group16_GUI_WebAPI.Controllers
{
    [Route("/devices")]
    public class DevicesController : Controller
    {
        private readonly SseClientService _sseClientService;
        private readonly WebSocketManagerService _webSocketManagerService;
        private readonly WebSocketHandlerService _webSocketHandlerService;
        private readonly PacketManagingService _packetManagingService;
        private readonly ApplicationDbContext _context;

        public DevicesController(WebSocketManagerService webSocketManagerService,
            SseClientService sseClientService,
            PacketManagingService packetManagingService,
            ApplicationDbContext context,
            WebSocketHandlerService webSocketHandlerService)
        {
            _webSocketManagerService = webSocketManagerService;
            _sseClientService = sseClientService;
            _packetManagingService = packetManagingService;
            _context = context;
            _webSocketHandlerService = webSocketHandlerService;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var deviceIds = (await _webSocketManagerService.GetAllBoundClients()).Select(x => BitConverter.ToString(x.Identifier).Replace("-", "").ToLowerInvariant());

            var values = new List<(string deviceId, uint status, float tempTarget, float phTarget, float rpmTarget)>();

            foreach (var device in await _webSocketManagerService.GetAllBoundClients())
            {
                var socket = _webSocketManagerService.GetBoundSocket(device);
                if (socket == null)
                {
                    continue;
                }

                var status = await _packetManagingService.DeviceStatusRequest(socket, device);
                if (status == null)
                {
                    continue;
                }

                values.Add((
                    BitConverter.ToString(device.Identifier).Replace("-", "").ToLowerInvariant(),
                    status.Status,
                    status.TempTarget,
                    status.PhTarget,
                    status.RPMTarget
                ));
            }

            return Ok(values.Select(x => new { x.deviceId, x.status, x.tempTarget, x.phTarget, x.rpmTarget }));
        }

        [Authorize]
        [HttpGet("events")]
        public async Task GetEventsAsync(CancellationToken cancellationToken)
        {
            Guid clientId = await _sseClientService.AddClient(Response);

            try
            {   
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Client Disconnected
            }
            finally
            {
                _sseClientService.RemoveClient(clientId);
            }
        }

        [Authorize]
        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetTargets(string id)
        {
            byte[] idHex = Convert.FromHexString(id);
            var connectedRegisteredClient = (await _webSocketManagerService.GetAllBoundClients()).FirstOrDefault(x => x.Identifier.SequenceEqual(idHex));
            if (connectedRegisteredClient == null)
            {
                return BadRequest(new
                {
                    error = "device_not_connected"
                });
            }

            var databaseRegisteredClient = await _context.Clients.Include(x => x.DeviceStatusData).FirstOrDefaultAsync(c => c.Identifier.SequenceEqual(idHex));
            if (databaseRegisteredClient == null)
            {
                return BadRequest(new
                {
                    error = "device_not_registered"
                });
            }

            if (!databaseRegisteredClient.Identifier.SequenceEqual(connectedRegisteredClient.Identifier))
            {
                return BadRequest(new
                {
                    error = "device_not_matching"
                });
            }

            var socketId = _webSocketManagerService.GetBoundSocket(connectedRegisteredClient);
            if (socketId == null)
            {
                return BadRequest(new
                {
                    error = "no_socket"
                });
            }

            var response = await _packetManagingService.DeviceStatusRequest(socketId, databaseRegisteredClient);
            if(response == null)
            {
                return BadRequest(new
                {
                    error = "device_no_response"
                });
            }

            return Ok(new { deviceId = Convert.ToHexString(connectedRegisteredClient.Identifier).ToLowerInvariant(), status = response.Status, tempTarget = response.TempTarget, phTarget = response.PhTarget, rpmTarget = response.RPMTarget });
        }

        [Authorize] 
        [HttpPost("{id}/target")]
        public async Task<IActionResult> SetTargets(string id, [FromBody] TargetRequest targetRequest)
        {
            byte[] idHex = Convert.FromHexString(id);
            var connectedRegisteredClient = (await _webSocketManagerService.GetAllBoundClients()).FirstOrDefault(x => x.Identifier.SequenceEqual(idHex));
            if (connectedRegisteredClient == null)
            {
                return BadRequest(new
                {
                    error = "device_not_connected"
                });
            }

            var databaseRegisteredClient = await _context.Clients.Include(x => x.DeviceStatusData).FirstOrDefaultAsync(c => c.Identifier.SequenceEqual(idHex));
            if (databaseRegisteredClient == null)
            {
                return BadRequest(new
                {
                    error = "device_not_registered"
                });
            }

            if (!databaseRegisteredClient.Identifier.SequenceEqual(connectedRegisteredClient.Identifier))
            {
                return BadRequest(new
                {
                    error = "device_not_matching"
                });
            }

            var socketId = _webSocketManagerService.GetBoundSocket(connectedRegisteredClient);
            if (socketId == null)
            {
                return BadRequest(new
                {
                    error = "no_socket"
                });
            }

            var result = await _packetManagingService.SetTargetRequest(socketId, databaseRegisteredClient, targetRequest.dataType, targetRequest.target);
            if (result == false)
            {
                return BadRequest(new
                {
                    error = "device_refused"
                });
            }

            return Ok(new { success = result } );
        }
    }
}