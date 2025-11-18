using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pos.Data;
using Pos.Models;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Pos.Controllers;
[Authorize(Roles ="Admin,Manager")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;
    public async Task<IActionResult> Index(string? q, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 25)
    {
        var query = _db.Sales
            .Include(s => s.Lines)!.ThenInclude(l => l.Service)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s =>
                s.SalesPerson.Contains(q) ||
                s.Lines!.Any(l => l.Service!.Name.Contains(q)));

        if (startDate.HasValue)
        {
            var from = startDate.Value.Date;
            query = query.Where(s => s.SaleDate >= from);
        }

        if (endDate.HasValue)
        {
            var to = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(s => s.SaleDate <= to);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Query = q;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

        return View(items);
    }

    // GET /Admin/ExportSalesPdf  (exports exactly what's on screen)
    public async Task<IActionResult> ExportSalesPdf(string? q, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 25)
    {
        // Reuse same filtering/paging as Index
        var query = _db.Sales
            .Include(s => s.Lines)!.ThenInclude(l => l.Service)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s =>
                s.SalesPerson.Contains(q) ||
                s.Lines!.Any(l => l.Service!.Name.Contains(q)));

        if (startDate.HasValue) query = query.Where(s => s.SaleDate >= startDate.Value.Date);
        if (endDate.HasValue) query = query.Where(s => s.SaleDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));

        var items = await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var culture = CultureInfo.GetCultureInfo("en-ZA");
        QuestPDF.Settings.License = LicenseType.Community; // required by QuestPDF

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text($"Sales ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})")
                    .FontSize(16).SemiBold();
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(50);   // #
                        cols.RelativeColumn(1);    // Date
                        cols.RelativeColumn(1);    // Sales Person
                        cols.RelativeColumn(2);    // Items
                        cols.RelativeColumn(1);    // Total
                    });

                    // header
                    table.Header(h =>
                    {
                        h.Cell().Element(Th).Text("#");
                        h.Cell().Element(Th).Text("Date");
                        h.Cell().Element(Th).Text("Sales Person");
                        h.Cell().Element(Th).Text("Items");
                        h.Cell().Element(Th).Text("Total");
                        static IContainer Th(IContainer c) => c.DefaultTextStyle(x => x.SemiBold()).Padding(5).Background(Colors.Grey.Lighten3);
                    });

                    decimal grand = 0m;

                    foreach (var s in items)
                    {
                        var totalAmt = s.Lines?.Sum(l => l.Quantity * l.UnitPrice) ?? 0m;
                        grand += totalAmt;

                        table.Cell().Padding(5).Text(s.Id.ToString());
                        table.Cell().Padding(5).Text(s.SaleDate.ToString("yyyy-MM-dd HH:mm"));
                        table.Cell().Padding(5).Text(s.SalesPerson ?? "");
                        table.Cell().Padding(5).Text(string.Join("\n",
                            (s.Lines ?? []).Select(l => $"{l.Quantity} × {l.Service?.Name} ({l.UnitPrice.ToString("C", culture)})")));
                        table.Cell().Padding(5).Text(totalAmt.ToString("C", culture));
                    }

                    // footer total (current page)
                    table.Footer(f =>
                    {
                        f.Cell().ColumnSpan(4).AlignRight().Padding(5).Text("Total of current sales:");
                        var grand = items.Sum(s => s.Lines?.Sum(l => l.Quantity * l.UnitPrice) ?? 0m);
                        f.Cell().Padding(5).Text(grand.ToString("C", culture)).SemiBold();
                    });
                });
                page.Footer().AlignRight().Text(txt =>
                {
                    txt.Span("Generated: ").Light();
                    txt.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                });
            });
        });

        var pdfBytes = doc.GeneratePdf();
        var filename = $"sales_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }
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
