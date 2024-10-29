namespace Challenge2_Group16_GUI_WebAPI.Models.Auth
{
    public class AuthorizationRequest
    {
        public string ResponseType { get; set; }
        public string ClientId { get; set; }
        public string RedirectUri { get; set; }
        public string Scope { get; set; }
        public string State { get; set; }
        public string CodeChallenge { get; set; }
        public string CodeChallengeMethod { get; set; }
    }
}
