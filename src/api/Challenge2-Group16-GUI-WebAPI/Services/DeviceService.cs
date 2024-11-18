using Challenge2_Group16_GUI_WebAPI.Models;

namespace Challenge2_Group16_GUI_WebAPI.Services
{
    public class DeviceService
    {
        private readonly WebSocketManagerService _webSocketManagerService;
        private RegisteredClient? _selectedClient = null;

        public DeviceService(WebSocketManagerService webSocketManagerService)
        {
            _webSocketManagerService = webSocketManagerService;
        }

        public IEnumerable<string> GetAllBoundClients()
        {
            return _webSocketManagerService.GetAllBoundClients().Select(x => x.Id);
        }

        public bool SelectClient(string id)
        {
            var client = _webSocketManagerService.GetBoundClient(id);
            if(client == null)
            {
                return false;
            }

            _selectedClient = client;

            return true;
        }

        public RegisteredClient GetSelectedClient()
        {
            return _selectedClient;
        }
    }
}
