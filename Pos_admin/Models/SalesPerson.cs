using System.ComponentModel.DataAnnotations;

namespace Pos.Models
{
    public class SalesPerson
    {
        public int Id { get; set; }
        public required string Person { get; set; }
        public string Email { get; set; }
    }
}
