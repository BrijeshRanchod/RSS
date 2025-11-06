using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSSPOS.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; } // Rands
    }
}
