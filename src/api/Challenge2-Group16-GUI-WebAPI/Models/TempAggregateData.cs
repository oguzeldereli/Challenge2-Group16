using Challenge2_Group16_GUI_WebAPI.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace Challenge2_Group16_GUI_WebAPI.Models
{
    public class TempAggregateData : IAggregateDataPacket
    {
        public string Id { get; set; }
        public RegisteredClient Client { get; set; }
        [ForeignKey("Client")]
        public string ClientId { get; set; }
        public DateTime TimeStamp { get; set; }
        public byte[] DataTimeStamps { get; set; } // get 8 bytes at a time, cast into Datetime
        public byte[] TemperatureAggregate { get; set; } // get 8 bytes at a time, cast into double

        public TempAggregateData()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
