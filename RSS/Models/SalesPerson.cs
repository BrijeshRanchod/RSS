using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSSPOS.Models
{
    public class SalesPerson
    {
        public int Id { get; set; }
        public required string Person { get; set; }
        public string Email { get; set; }
    }
}
