using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using Challenge2_Group16_GUI_WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public IActionResult Index()
        {
            return Ok(_webSocketManagerService.GetAllBoundClients().Select(x => x.Identifier));
        }

        [Authorize]
        [HttpGet("events")]
        public async Task GetEventsAsync(CancellationToken cancellationToken)
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            Guid clientId = _sseClientService.AddClient(Response);

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
            byte[] idBase64Decode = Convert.FromBase64String(id);
            var connectedRegisteredClient = _webSocketManagerService.GetAllBoundClients().Where(x => x.Identifier == idBase64Decode).FirstOrDefault();
            if (connectedRegisteredClient == null)
            {
                return BadRequest(new
                {
                    error = "device_not_connected"
                });
            }

            var databaseRegisteredClient = await _context.Clients.Include(x => x.DeviceStatusData).FirstOrDefaultAsync(c => c.Identifier == idBase64Decode);
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

            return Ok(new { response.Status, response.TempTarget, response.PhTarget, response.RPMTarget });
        }

        [Authorize]
        [HttpPost("{id}/status")]
        public IActionResult SetTargets(string id, [FromForm] uint status, [FromForm] uint temperatureTarget, [FromForm] uint phTarget, [FromForm] uint rpmTarget)
        {
            return Ok();
        }
    }
}