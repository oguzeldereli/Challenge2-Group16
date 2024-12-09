using Azure;
using Challenge2_Group16_GUI_WebAPI.Data;
using Challenge2_Group16_GUI_WebAPI.Models;
using Challenge2_Group16_GUI_WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
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
        private readonly DataService _dataService;

        public DevicesController(WebSocketManagerService webSocketManagerService,
            SseClientService sseClientService,
            PacketManagingService packetManagingService,
            ApplicationDbContext context,
            WebSocketHandlerService webSocketHandlerService,
            DataService dataService)
        {
            _webSocketManagerService = webSocketManagerService;
            _sseClientService = sseClientService;
            _packetManagingService = packetManagingService;
            _context = context;
            _webSocketHandlerService = webSocketHandlerService;
            _dataService = dataService;
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

            await _sseClientService.PublishAsJsonAsync("data", new
            {
                client_id = Convert.ToHexString(connectedRegisteredClient.Identifier).ToLowerInvariant(),
                data = new
                {
                    data_type = "log",
                    time_stamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                    log_level = "Information",
                    log_message = $"Client {Convert.ToHexString(connectedRegisteredClient.Identifier).ToLowerInvariant()} report a status of '{response.Status switch { 0 => "Not Ready", 1 => "Operational", 2 => "Paused", _ => "Invalid" }}' with targets temp = {response.TempTarget} ph = {response.PhTarget} rpm = {response.RPMTarget}."
                }
            });
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
            await _sseClientService.PublishAsJsonAsync("data", new
            {
                client_id = Convert.ToHexString(connectedRegisteredClient.Identifier).ToLowerInvariant(),
                data = new
                {
                    data_type = "log",
                    time_stamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                    log_level = "Information",
                    log_message = $"Client {Convert.ToHexString(connectedRegisteredClient.Identifier).ToLowerInvariant()} {(result ? "succeeded" : "failed")} in setting the target. The target was '{targetRequest.dataType switch { 0 => "Temperature", 1 => "pH", 2 => "RPM", _ => "Invalid" }}' = {targetRequest.target}."
                }
            });
            return Ok(new { success = result } );
        }


        [Authorize]
        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartDevice(string id)
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

            var result = await _packetManagingService.StartRequest(socketId, databaseRegisteredClient);
            if (result == false)
            {
                return BadRequest(new
                {
                    error = "device_refused"
                });
            }

            await _sseClientService.PublishAsJsonAsync("data", new
            {
                client_id = Convert.ToHexString(connectedRegisteredClient.Identifier).ToLowerInvariant(),
                data = new
                {
                    data_type = "log",
                    time_stamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                    log_level = "Information",
                    log_message = $"Client {Convert.ToHexString(connectedRegisteredClient.Identifier).ToLowerInvariant()} {(result ? "succeeded" : "failed")} in starting."
                }
            });

            return Ok(new { success = result });
        }

        [Authorize]
        [HttpPost("{id}/pause")]
        public async Task<IActionResult> PauseDevice(string id)
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

            var result = await _packetManagingService.PauseRequest(socketId, databaseRegisteredClient);
            if (result == false)
            {
                return BadRequest(new
                {
                    error = "device_refused"
                });
            }

            await _sseClientService.PublishAsJsonAsync("data", new
            {
                client_id = Convert.ToHexString(connectedRegisteredClient.Identifier).ToLowerInvariant(),
                data = new
                {
                    data_type = "log",
                    time_stamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                    log_level = "Information",
                    log_message = $"Client {Convert.ToHexString(connectedRegisteredClient.Identifier).ToLowerInvariant()} {(result ? "succeeded" : "failed")} in pausing."
                }
            });

            return Ok(new { success = result });
        }

        [Authorize]
        [HttpGet("{id}/{type}")]
        public async Task<IActionResult> GetDeviceData(string id, string type, [FromQuery] long timeStamp1, [FromQuery] long timeStamp2)
        {
            byte[] idHex = Convert.FromHexString(id);
            var client = await _context.Clients.FirstOrDefaultAsync(x => x.Identifier.SequenceEqual(idHex));
            if (client == null)
            {
                return BadRequest(new
                {
                    error = "device_not_registered"
                });
            }
            var date1 = DateTimeOffset.FromUnixTimeSeconds(timeStamp1).UtcDateTime;
            var date2 = DateTimeOffset.FromUnixTimeSeconds(timeStamp2).UtcDateTime;

            if(date1 >= date2)
            {
                return BadRequest(new
                {
                    error = "invalid_dates"
                });
            }

            object? sendObject = null;
            switch(type) {
                case "temp":
                {
                    var data = await _dataService.ReadRawDataFromDbAsyc<TempData>(client, date1, date2);
                    sendObject = data?.Select(x => new { client_id = id, data_type = "temperature", time_stamp = ((DateTimeOffset)x.TimeStamp.ToUniversalTime()).ToUnixTimeSeconds(), value = x.Temperature });
                    break;
                }
                case "ph":
                {
                    var data = await _dataService.ReadRawDataFromDbAsyc<pHData>(client, date1, date2);
                        sendObject = data?.Select(x => new { client_id = id, data_type = "ph", time_stamp = ((DateTimeOffset)x.TimeStamp.ToUniversalTime()).ToUnixTimeSeconds(), value = x.pH });
                        break;
                }
                case "rpm":
                {
                    var data = await _dataService.ReadRawDataFromDbAsyc<StirringData>(client, date1, date2);
                    sendObject = data?.Select(x => new { client_id = id, data_type = "rpm", time_stamp = ((DateTimeOffset)x.TimeStamp.ToUniversalTime()).ToUnixTimeSeconds(), value = x.RPM });
                    break;
                }
            };

            if (sendObject == null)
            {
                return BadRequest(new
                {
                    error = "internal_error"
                });
            }

            return Ok(sendObject);
        }

    }
}