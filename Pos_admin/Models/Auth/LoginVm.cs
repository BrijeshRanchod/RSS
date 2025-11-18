using System.ComponentModel.DataAnnotations;

namespace Pos.Models.Auth
{
    public class LoginVm
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        // optional: return url to redirect after login
        public string? ReturnUrl { get; set; }
    }
}
