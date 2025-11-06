using Microsoft.EntityFrameworkCore;
using PosApp.Data;
using PosApp.Models;

namespace PosApp.Services;
public class ProductService : IProductService
{
    private readonly PosDb _db;
    public ProductService(PosDb db) => _db = db;

    public Task<Product?> GetByBarcodeAsync(string barcode) =>
        _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Barcode == barcode);

    public Task<List<Product>> SearchAsync(string term)
    {
        term = term?.Trim().ToLower() ?? "";
        return _db.Products.AsNoTracking()
            .Where(p => p.Name.ToLower().Contains(term) || p.Barcode.Contains(term))
            .OrderBy(p => p.Name)
            .Take(50)
            .ToListAsync();
    }
}
