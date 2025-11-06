using PosApp.Models;

namespace PosApp.Services;
public interface ISaleService
{
    Task<int> SaveSaleAsync(Sale sale);
}
