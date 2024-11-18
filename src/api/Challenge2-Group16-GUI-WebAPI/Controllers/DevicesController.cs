using Challenge2_Group16_GUI_WebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Challenge2_Group16_GUI_WebAPI.Controllers
{
    [Route("/devices")]
    public class DevicesController : Controller
    {
        private readonly DeviceService _deviceService;

        public DevicesController(DeviceService deviceService)
        {
            _deviceService = deviceService;
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

            return Ok(new {clientId = selectedClient.Identifier});
        }

        [Authorize]
        [HttpPost("select")]
        public IActionResult SelectDevice([FromBody] string clientId)
        {
            if(string.IsNullOrEmpty(clientId))
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
        [HttpGet("events")]
        public async Task<IActionResult> GetEventsAsync()
        {
            var selectedClient = _deviceService.GetSelectedClient();
            if (selectedClient == null)
            {
                return BadRequest(new { error = "no_selected_device" });
            }



            return Ok();
        }
    }
}