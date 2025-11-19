using System.ComponentModel.DataAnnotations;
using Pos.Models;

namespace Pos.Models;

public class AdminSaleEditVm
{
    public int Id { get; set; }

    [Required, Display(Name = "Sales Person")]
    public string SalesPerson { get; set; } = "";

    [Display(Name = "Sale Date")]
    public DateTime SaleDate { get; set; }

    public List<AdminSaleLineVm> Lines { get; set; } = new();

    // For dropdowns
    public List<Service> Services { get; set; } = new();

    public decimal GrandTotal => Lines?.Sum(l => l.Quantity * l.UnitPrice) ?? 0m;

    public string? Command { get; set; } 
    public int? CommandIndex { get; set; }
    
}

public class AdminSaleLineVm
{
    public int? Id { get; set; }       

    [Display(Name = "Service")]
    public int? ServiceId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [Range(0, double.MaxValue), Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }
}


