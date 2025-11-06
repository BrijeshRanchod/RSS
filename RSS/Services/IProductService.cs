using RSSPOS.Models;

namespace RSSPOS.Services;
public interface IProductService
{
    Task<Service?> GetByBarcodeAsync(string barcode);
    Task<List<Service>> SearchAsync(string term);
}
