using Challenge2_Group16_GUI_WebAPI.Interfaces;

namespace Challenge2_Group16_GUI_WebAPI.Models
{
    public class LogAggregateData : IAggregateDataPacket
    {
        public string Id { get; set; }
        public RegisteredClient Client { get; set; }
        public string ClientId { get; set; }
        public DateTime TimeStamp { get; set; }
        public byte[] DataTimeStamps { get; set; } // get 8 bytes at a time, cast into Datetime
        public string Logs { get; set; } // in type:message; format

        public LogAggregateData()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
