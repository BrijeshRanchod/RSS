using Microsoft.EntityFrameworkCore;
using PosApp.Data;
using PosApp.Models;

namespace PosApp.Services;
public class SaleService : ISaleService
{
    private readonly PosDb _db;
    public SaleService(PosDb db) => _db = db;

    public async Task<int> SaveSaleAsync(Sale sale)
    {
        // Reduce stock
        foreach (var line in sale.Lines)
        {
            var product = await _db.Products.FirstAsync(p => p.Id == line.ProductId);
            product.Stock -= line.Qty;
        }
        _db.Sales.Add(sale);
        await _db.SaveChangesAsync();
        return sale.Id;
    }
}
