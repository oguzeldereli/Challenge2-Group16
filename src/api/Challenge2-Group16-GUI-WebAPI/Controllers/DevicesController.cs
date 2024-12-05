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
            var deviceIds = _webSocketManagerService.GetAllBoundClients().Select(x => BitConverter.ToString(x.Identifier).Replace("-", "").ToLowerInvariant());
            List<(string deviceId, uint status, float tempTarget, float phTarget, float rpmTarget)> values = new();
            _webSocketManagerService.GetAllBoundClients().ToList().ForEach(async (device) =>
            {
                var socket = _webSocketManagerService.GetBoundSocket(device);
                if (socket == null)
                {
                    return;
                }

                var status = await _packetManagingService.DeviceStatusRequest(socket, device);
                if (status == null)
                {
                    return;
                }

                values.Add((BitConverter.ToString(device.Identifier).Replace("-", "").ToLowerInvariant(), status.Status, status.TempTarget, status.PhTarget, status.RPMTarget));
            });
            return Ok(values);
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
            var connectedRegisteredClient = _webSocketManagerService.GetAllBoundClients().Where(x => x.Identifier == idHex).FirstOrDefault();
            if (connectedRegisteredClient == null)
            {
                return BadRequest(new
                {
                    error = "device_not_connected"
                });
            }

            var databaseRegisteredClient = await _context.Clients.Include(x => x.DeviceStatusData).FirstOrDefaultAsync(c => c.Identifier == idHex);
            if (databaseRegisteredClient == null)
            {
                return BadRequest(new
                {
                    error = "device_not_registered"
                });
            }

            if (databaseRegisteredClient.Identifier != connectedRegisteredClient.Identifier)
            {
                return BadRequest(new
                {
                    error = "device_not_matching"
                });
            }

            var lastDeviceStatus = databaseRegisteredClient.DeviceStatusData.OrderByDescending(x => x.TimeStamp).FirstOrDefault();
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

            return Ok(new { status = response.Status, tempTarget = response.TempTarget, phTarget = response.PhTarget, rpmTargget = response.RPMTarget });
        }

        [Authorize]
        [HttpPost("{id}/status")]
        public IActionResult SetTargets(string id, [FromForm] uint status, [FromForm] uint temperatureTarget, [FromForm] uint phTarget, [FromForm] uint rpmTarget)
        {
            return Ok();
        }
    }
}