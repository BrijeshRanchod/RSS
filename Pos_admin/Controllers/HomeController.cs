using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pos.Models;
using Pos.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Authorization;

namespace Pos.Controllers;
[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _db;

    public HomeController(ILogger<HomeController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }
        public class WebsiteController : Controller
    {
        // GET: /Website/Index
        public IActionResult Index() => View();
    }
    // ---------------- GET: /Home/Index ----------------
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var services = await _db.Services.OrderBy(s => s.Name).ToListAsync();
        var salesPeople = await _db.SalesPeople.OrderBy(sp => sp.Person).ToListAsync();


        var vm = new PosViewModel
        {
            SalesPeople = salesPeople,
            Services = services,
            Lines = new List<SaleLineInput> { new() } // start with 1 line
        };

        return View(vm);
    }

        [HttpGet]
    public IActionResult Calendar()
    {
        return View(); // looks for Views/Home/Calendar.cshtml
    }

    // ---------------- POST: /Home/CreateSale ----------------
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateSale(PosViewModel? model)
{
    if (model is null)
    {
        ModelState.AddModelError("", "Invalid submission.");
        model = new PosViewModel();
    }

    if (string.IsNullOrWhiteSpace(model.SalesPersonId))
        ModelState.AddModelError(nameof(model.SalesPersonId), "Sales person is required.");

    var lines = model.Lines ?? new List<SaleLineInput>();
    var validLines = lines.Where(l => l.ServiceId.HasValue && l.Quantity > 0).ToList();

    if (validLines.Count == 0)
        ModelState.AddModelError("", "Add at least one service line.");

    if (!ModelState.IsValid)
    {
        model.Services = await _db.Services.OrderBy(s => s.Name).ToListAsync();
        return View("Index", model);
    }

    var priceMap = await _db.Services.ToDictionaryAsync(s => s.Id, s => s.Price);

    var sale = new Sale
    {
        SalesPerson = model.SalesPersonId!,
        SaleDate = DateTime.Now,
        Lines = validLines.Select(l => new SaleLine
        {
            ServiceId = l.ServiceId!.Value,
            Quantity  = l.Quantity,
            UnitPrice = priceMap.TryGetValue(l.ServiceId!.Value, out var p) ? p : 0m
        }).ToList()
    };

    _db.Sales.Add(sale);
    await _db.SaveChangesAsync();

    // Back to POS screen with a fresh form
    var newVm = new PosViewModel
    {
        Services = await _db.Services.OrderBy(s => s.Name).ToListAsync(),
        Lines = new List<SaleLineInput> { new() },
        SalesPersonId = ""
    };

    ViewBag.Success = $"Sale #{sale.Id} recorded.";
    return View("Index", newVm);
}


    // ---------------- GET: /Home/Price/{id} ----------------
    [HttpGet]
    public async Task<IActionResult> Price(int id)
    {
        var svc = await _db.Services.FindAsync(id);
        if (svc == null) return NotFound();
        return Json(new { price = svc.Price });
    }

    // ---------------- GET: /Home/Receipt/{id} ----------------
    [HttpGet]
    public async Task<IActionResult> Receipt(int id)
    {
        var sale = await _db.Sales
            .Include(s => s.Lines)
            .ThenInclude(l => l.Service)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sale == null) return NotFound();
        return View(sale);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

// ---------------- Supporting ViewModels ----------------
// Controllers/HomeController.cs (same file, below your controller)
// or move to a separate folder/namespace if you prefer

public class PosViewModel
{
    public string? SalesPersonId { get; set; }
    public List<Service> Services { get; set; } = new();
    public List<SaleLineInput> Lines { get; set; } = new();

    public List<SalesPerson> SalesPeople { get; set; } = new();

    // For initial render & server re-render; JS keeps it updated client-side too
    public decimal GrandTotal =>
        Lines?.Where(l => l.ServiceId.HasValue)
              .Sum(l => l.Quantity * l.UnitPrice) ?? 0m;
}

public class SaleLineInput
{
    public int? ServiceId { get; set; }
    public int Quantity { get; set; } = 1;

    // Needed by the view for display/edit. The server will overwrite this on POST.
    public decimal UnitPrice { get; set; } = 0m;
}
