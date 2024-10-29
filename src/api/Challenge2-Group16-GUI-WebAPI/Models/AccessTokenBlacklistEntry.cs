namespace Challenge2_Group16_GUI_WebAPI.Models
{
    public class AccessTokenBlacklistEntry
    {
        public string Id { get; set; }
        public string AccessToken { get; set; }

        public AccessTokenBlacklistEntry()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
