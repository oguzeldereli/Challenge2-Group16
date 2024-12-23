﻿using Challenge2_Group16_GUI_WebAPI.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace Challenge2_Group16_GUI_WebAPI.Models
{
    public class DeviceStatusData : IDataPacket
    {
        public string Id { get; set; }
        public RegisteredClient Client { get; set; }
        [ForeignKey("Client")]
        public string ClientId { get; set; }
        public DateTime TimeStamp { get; set; }
        public uint Status { get; set; }
        public float TempTarget { get; set; }
        public float PhTarget { get; set; }
        public float RPMTarget { get; set; }
            
        public DeviceStatusData()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
