namespace Challenge2_Group16_GUI_WebAPI.Models.Auth
{
    public class TokenRequest
    {
        public string grant_type { get; set; }
        public string code { get; set; }
        public string redirect_uri { get; set; }
        public string client_id { get; set; }
        public string code_verifier { get; set; }
    }
}