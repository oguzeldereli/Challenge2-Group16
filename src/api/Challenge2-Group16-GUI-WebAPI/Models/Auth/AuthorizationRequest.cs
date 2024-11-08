namespace Challenge2_Group16_GUI_WebAPI.Models.Auth
{
    public class AuthorizationRequest
    {
        public string client_id { get; set; }
        public string response_type { get; set; }
        public string redirect_uri { get; set; }
        public string scope { get; set; }
        public string state { get; set; }
        public string code_challenge { get; set; }
        public string code_challenge_method { get; set; }
    }
}
