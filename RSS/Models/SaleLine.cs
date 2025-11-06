using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSSPOS.Models
{
    public class SaleLine
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }

        public int ServiceId { get; set; }
        public Service? Service { get; set; }

        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }          // copy from Service.Price at time of sale
        public decimal LineTotal => Quantity * UnitPrice;
    }
}
