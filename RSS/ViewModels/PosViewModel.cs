using RSSPOS.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using RSSPOS.Models;
namespace RSSPOS.ViewModels;
public class PosViewModel
{
    private readonly AppDbContext _db;

    public ObservableCollection<Service> Services { get; } = new();
    public ObservableCollection<SalesPerson> SalesPeople { get; } = new();

    public PosViewModel(AppDbContext db)
    {
        _db = db;
        LoadLookups();
    }

    private void LoadLookups()
    {
        foreach (var s in _db.Services.AsNoTracking().OrderBy(x => x.Name))
            Services.Add(s);

        foreach (var sp in _db.SalesPeople.AsNoTracking().OrderBy(x => x.Person))
            SalesPeople.Add(sp);
    }

    public void SaveSale(Sale sale, IEnumerable<SaleLine> lines)
    {
        _db.Sales.Add(sale);
        foreach (var l in lines) { l.SaleId = sale.Id; _db.SaleLines.Add(l); }
        _db.SaveChanges();
    }
}
