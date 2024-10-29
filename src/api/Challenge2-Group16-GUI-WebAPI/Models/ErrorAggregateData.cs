using Challenge2_Group16_GUI_WebAPI.Interfaces;

namespace Challenge2_Group16_GUI_WebAPI.Models
{
    public class ErrorAggregateData : IAggregateDataPacket
    {
        public string Id { get; set; }
        public RegisteredClient Client { get; set; }
        public string ClientId { get; set; }
        public DateTime TimeStamp { get; set; }
        public byte[] DataTimeStamps { get; set; } // get 8 bytes at a time, cast into Datetime
        public byte[] Errors { get; set; } // get 4 bytes at a time, cast into int

        public ErrorAggregateData()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
