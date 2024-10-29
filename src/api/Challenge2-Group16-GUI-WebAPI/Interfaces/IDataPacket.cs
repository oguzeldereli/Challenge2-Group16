using Challenge2_Group16_GUI_WebAPI.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Challenge2_Group16_GUI_WebAPI.Interfaces
{
    public interface IDataPacket
    {
        public string Id { get; set; }
        public RegisteredClient Client { get; set; }
        public string ClientId { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public interface IAggregateDataPacket
    {
        public string Id { get; set; }
        public RegisteredClient Client { get; set; }
        public string ClientId { get; set; }
        public DateTime TimeStamp { get; set; }
        public byte[] DataTimeStamps { get; set; }
    }
}
