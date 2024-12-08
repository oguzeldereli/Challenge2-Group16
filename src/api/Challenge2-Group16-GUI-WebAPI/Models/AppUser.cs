using Microsoft.AspNetCore.Identity;

namespace Challenge2_Group16_GUI_WebAPI.Models
{
    public class AppUser : IdentityUser
    {
        public RefreshToken RefreshToken { get; set; }
    }
}
