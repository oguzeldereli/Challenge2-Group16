using System.ComponentModel.DataAnnotations.Schema;

namespace Challenge2_Group16_GUI_WebAPI.Models
{
    public class RefreshToken
    {
        public string Id { get; set; }
        public string? Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser User { get; set; }
        public bool IsRevoked { get; set; }
        public bool IsUsed { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        public RefreshToken()
        {
             Id = Guid.NewGuid().ToString();
        }
    }
}
