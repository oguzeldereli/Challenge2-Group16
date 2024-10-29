using Challenge2_Group16_GUI_WebAPI.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace Challenge2_Group16_GUI_WebAPI.Models
{
    public class DeviceStatusAggregateData : IAggregateDataPacket
    {
        public string Id { get; set; }
        public RegisteredClient Client { get; set; }
        [ForeignKey("Client")]
        public string ClientId { get; set; }
        public DateTime TimeStamp { get; set; }
        public byte[] DataTimeStamps { get; set; } // read by 8 bytes at a time
        public string Status { get; set; } // separated by semicolons

        public DeviceStatusAggregateData()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
