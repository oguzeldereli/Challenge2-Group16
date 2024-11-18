namespace Challenge2_Group16_GUI_WebAPI.Models
{
    public class RefreshRequest
    {
        public string grant_type { get; set; }
        public string refresh_token { get; set; }
        public string scope { get; set; }
    }
}
