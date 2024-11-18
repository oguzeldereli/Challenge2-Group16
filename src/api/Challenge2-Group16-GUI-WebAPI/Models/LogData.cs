using Challenge2_Group16_GUI_WebAPI.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace Challenge2_Group16_GUI_WebAPI.Models
{
    public class LogData : IDataPacket
    {
        public string Id { get; set; }
        public RegisteredClient Client { get; set; }
        [ForeignKey("Client")]
        public string ClientId { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }

        public LogData()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
