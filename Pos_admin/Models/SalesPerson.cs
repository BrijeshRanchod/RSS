using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Pos.Models
{
    public enum SalesRole { Sales = 0, Manager = 1, Admin = 2 } // optional

    public class SalesPerson
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public required string Person { get; set; }

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = string.Empty;

        // Link to AspNetUsers (Identity)
        [StringLength(450)]
        public string? IdentityUserId { get; set; }
        public IdentityUser? IdentityUser { get; set; }

        // Optional: mirror role here for convenience/queries
        public SalesRole? Role { get; set; }
    }
}
