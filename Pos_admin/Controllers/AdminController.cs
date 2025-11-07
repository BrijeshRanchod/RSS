using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pos.Data;
using Pos.Models;

namespace Pos.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    // ---------------- SALES LIST ----------------
    // GET: /Admin
    // GET: /Admin/Sales?q=abc&page=1&pageSize=25
    [HttpGet]
    public async Task<IActionResult> Index(string? q = null, int page = 1, int pageSize = 25)
    {
        var query = _db.Sales
            .Include(s => s.Lines).ThenInclude(l => l.Service)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(s => s.SalesPerson!.Contains(term) ||
                                     s.Lines.Any(l => l.Service!.Name.Contains(term)));
        }

        var total = await query.CountAsync();
        var sales = await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Query = q;

        return View(sales); // Views/Admin/Index.cshtml
    }

    // ---------------- SALES EDIT ----------------
    // GET: /Admin/EditSale/5
    [HttpGet]
    public async Task<IActionResult> EditSale(int id)
    {
        var sale = await _db.Sales
            .Include(s => s.Lines).ThenInclude(l => l.Service)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null) return NotFound();

        var vm = new AdminSaleEditVm
        {
            Id = sale.Id,
            SalesPerson = sale.SalesPerson ?? "",
            SaleDate = sale.SaleDate,
            Lines = sale.Lines.Select(l => new AdminSaleLineVm
            {
                Id = l.Id,
                ServiceId = l.ServiceId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList(),
            Services = await _db.Services.OrderBy(s => s.Name).ToListAsync()
        };

        return View(vm); // Views/Admin/EditSale.cshtml
    }

    // POST: /Admin/EditSale/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSale(int id, AdminSaleEditVm vm)
    {
        // Inline add/remove row behavior
        if (vm.Command == "AddLine")
        {
            vm.Lines ??= new();
            vm.Lines.Add(new AdminSaleLineVm());
            vm.Services = await _db.Services.OrderBy(s => s.Name).ToListAsync();
            ModelState.Clear();
            return View(vm);
        }
        if (vm.Command == "RemoveLine" && vm.CommandIndex is int idx && idx >= 0 && idx < (vm.Lines?.Count ?? 0))
        {
            vm.Lines!.RemoveAt(idx);
            vm.Services = await _db.Services.OrderBy(s => s.Name).ToListAsync();
            ModelState.Clear();
            return View(vm);
        }

        if (!ModelState.IsValid)
        {
            vm.Services = await _db.Services.OrderBy(s => s.Name).ToListAsync();
            return View(vm);
        }

        var sale = await _db.Sales
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null) return NotFound();

        // header
        sale.SalesPerson = vm.SalesPerson;
        sale.SaleDate = vm.SaleDate;

        // existing lines map
        var existingById = sale.Lines.ToDictionary(l => l.Id);
        var keepIds = new HashSet<int>();

        // upsert posted lines
        foreach (var l in vm.Lines ?? Enumerable.Empty<AdminSaleLineVm>())
        {
            if (l.Id is int lid && existingById.TryGetValue(lid, out var found))
            {
                found.ServiceId = l.ServiceId ?? found.ServiceId;
                found.Quantity = l.Quantity;
                found.UnitPrice = l.UnitPrice;
                keepIds.Add(lid);
            }
            else
            {
                sale.Lines.Add(new SaleLine
                {
                    ServiceId = l.ServiceId ?? 0,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice
                });
            }
        }

        // delete removed lines
        var toRemove = sale.Lines.Where(x => !keepIds.Contains(x.Id)).ToList();
        if (vm.Lines is not null && vm.Lines.Any()) // only prune when there was a postback list
        {
            foreach (var r in toRemove) _db.SaleLines.Remove(r);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Sale #{sale.Id} updated.";
        return RedirectToAction(nameof(EditSale), new { id = sale.Id });
    }

    // ---------------- SALES DETAILS ----------------
    // GET: /Admin/Sale/5
    [HttpGet]
    public async Task<IActionResult> Sale(int id)
    {
        var sale = await _db.Sales
            .Include(s => s.Lines).ThenInclude(l => l.Service)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null) return NotFound();
        return View(sale); // Views/Admin/Sale.cshtml (optional)
    }

    // ---------------- SALES DELETE ----------------
    // POST: /Admin/DeleteSale/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSale(int id)
    {
        var sale = await _db.Sales.Include(s => s.Lines).FirstOrDefaultAsync(s => s.Id == id);
        if (sale == null) return NotFound();

        _db.SaleLines.RemoveRange(sale.Lines);
        _db.Sales.Remove(sale);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Sale #{id} deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- SERVICES LIST ----------------
    // GET: /Admin/Services
    [HttpGet]
    public async Task<IActionResult> Services()
    {
        var services = await _db.Services.OrderBy(s => s.Name).ToListAsync();
        return View(services); // Views/Admin/Services.cshtml
    }

    // ---------------- SERVICE EDIT ----------------
    // GET: /Admin/EditService/3
    [HttpGet]
    public async Task<IActionResult> EditService(int id)
    {
        var svc = await _db.Services.FindAsync(id);
        if (svc == null) return NotFound();
        return View(svc); // Views/Admin/EditService.cshtml
    }

    // POST: /Admin/EditService/3
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditService(int id, Service model)
    {
        var svc = await _db.Services.FindAsync(id);
        if (svc == null) return NotFound();

        if (!ModelState.IsValid) return View(model);

        svc.Name = model.Name;
        svc.Price = model.Price;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Service updated.";
        return RedirectToAction(nameof(Services));
    }
    // GET: /Admin/CreateService
[HttpGet]
public IActionResult CreateService()
{
    // Only used when you want to render a separate page or return validation errors to the same list view
    return View(new Service());
}

// POST: /Admin/CreateService
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateService([Bind("Name,Price")] Service model)
{
    if (!ModelState.IsValid)
    {
        // If you’re using the inline form on the Services page:
        // re-fetch list and return the Services view with errors.
        var services = await _db.Services.OrderBy(s => s.Name).ToListAsync();
        ViewData["InlineCreateError"] = true; // flag to re-open the form
        return View("Services", services);
    }

    _db.Services.Add(model);
    await _db.SaveChangesAsync();
    TempData["Success"] = $"Service '{model.Name}' created.";
    return RedirectToAction(nameof(Services));
}

}
