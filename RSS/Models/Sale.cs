using System;
using System.Collections.Generic;

namespace RSSPOS.Models;
public class Sale
{
    public int Id { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public string SalesPerson { get; set; } = "";
    public ICollection<SaleLine> Lines { get; set; } = new List<SaleLine>();
}
