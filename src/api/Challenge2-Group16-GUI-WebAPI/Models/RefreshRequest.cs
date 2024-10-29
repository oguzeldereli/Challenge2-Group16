namespace Challenge2_Group16_GUI_WebAPI.Models
{
    public class RefreshRequest
    {
        public string GrantType { get; set; }
        public string RefreshToken { get; set; }
        public string Scope { get; set; }
    }
}
