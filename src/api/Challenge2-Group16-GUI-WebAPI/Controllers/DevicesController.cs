using Challenge2_Group16_GUI_WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Challenge2_Group16_GUI_WebAPI.Controllers
{
    [Route("/devices")]
    public class DevicesController : Controller
    {
        private readonly DeviceService _deviceService;
        private readonly SseClientService _sseClientService;

        public DevicesController(
            DeviceService deviceService,
            SseClientService sseClientService)
        {
            _deviceService = deviceService;
            _sseClientService = sseClientService;
        }

        [Authorize]
        public IActionResult Index()
        {
            return Ok(_deviceService.GetAllBoundClients());
        }

        [Authorize]
        [HttpGet("select")]
        public IActionResult GetSelectedDevice()
        {
            var selectedClient = _deviceService.GetSelectedClient();
            if (selectedClient == null)
            {
                return BadRequest(new { error = "no_selected_device" });
            }

            return Ok(new { clientId = selectedClient.Identifier });
        }

        [Authorize]
        [HttpPost("select")]
        public IActionResult SelectDevice([FromBody] string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                return BadRequest(new { error = "missing_device_id" });
            }

            if (!_deviceService.SelectClient(clientId))
            {
                return BadRequest(new { error = "invalid_device_id" });
            }

            return Ok();
        }

        [Authorize]
        [HttpGet("device-events")]
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
    }
}