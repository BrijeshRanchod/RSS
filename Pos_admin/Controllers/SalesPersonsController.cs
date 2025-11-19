using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pos.Data;
using Pos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pos.Controllers
{
    public class SalesPersonsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _users;
        private readonly RoleManager<IdentityRole> _roles;

        public SalesPersonsController(AppDbContext context,
                                   UserManager<IdentityUser> users,
                                  RoleManager<IdentityRole> roles)
        {
            _context = context;
            _users = users;
            _roles = roles;
        }

        // GET: SalesPersons
        public async Task<IActionResult> Index()
        {
            return View(await _context.SalesPeople.ToListAsync());
        }

        // GET: SalesPersons/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salesPerson = await _context.SalesPeople
                .FirstOrDefaultAsync(m => m.Id == id);
            if (salesPerson == null)
            {
                return NotFound();
            }

            return View(salesPerson);
        }

        // GET: SalesPersons/Create
        public IActionResult Create()
        {
            return View();
        }
        private async Task EnsureIdentityUserAndRoleAsync(SalesPerson sp)
        {
            if (string.IsNullOrWhiteSpace(sp.Email)) return;

            // Ensure Identity roles exist
            foreach (var r in new[] { "Admin", "Manager", "Sales" })
                if (!await _roles.RoleExistsAsync(r))
                    await _roles.CreateAsync(new IdentityRole(r));

            // Create or find user
            var user = sp.IdentityUserId != null
                ? await _users.FindByIdAsync(sp.IdentityUserId)
                : await _users.FindByEmailAsync(sp.Email);

            if (user is null)
            {
                user = new IdentityUser { UserName = sp.Email, Email = sp.Email, EmailConfirmed = true };
                var create = await _users.CreateAsync(user, "ICNails2025!"); // temp; reset later
                if (!create.Succeeded) return;
            }

            // Link back if not linked
            if (sp.IdentityUserId != user.Id)
            {
                sp.IdentityUserId = user.Id;
                _context.Update(sp);
                await _context.SaveChangesAsync();
            }

            var desired = sp.Role switch
            {
                SalesRole.Admin => "Admin",
                SalesRole.Manager => "Manager",
                _ => "Sales"
            };

            // Ensure user is in desired role and not in the others
            foreach (var r in new[] { "Admin", "Manager", "Sales" })
            {
                var inRole = await _users.IsInRoleAsync(user, r);
                if (r == desired && !inRole) await _users.AddToRoleAsync(user, r);
                if (r != desired && inRole) await _users.RemoveFromRoleAsync(user, r);
            }
        }

        // POST: SalesPersons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Person,Email,Role")] SalesPerson salesPerson)
        {
            if (!ModelState.IsValid) return View(salesPerson);

            _context.Add(salesPerson);
            await _context.SaveChangesAsync();

            // OPTIONAL: sync Identity role now (instead of waiting for startup sync)
            await EnsureIdentityUserAndRoleAsync(salesPerson);

            TempData["Success"] = "Staff member created.";
            return RedirectToAction(nameof(Index));
        }



        // GET: SalesPersons/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salesPerson = await _context.SalesPeople.FindAsync(id);
            if (salesPerson == null)
            {
                return NotFound();
            }
            return View(salesPerson);
        }

        // POST: SalesPersons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Person,Email,Role")] SalesPerson input)
        {
            if (id != input.Id) return NotFound();
            if (!ModelState.IsValid) return View(input);

            var entity = await _context.SalesPeople.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return NotFound();

            entity.Person = input.Person;
            entity.Email = input.Email;
            entity.Role = input.Role;

            await _context.SaveChangesAsync();

            // OPTIONAL: sync Identity role
            await EnsureIdentityUserAndRoleAsync(entity);

            TempData["Success"] = "Staff member updated.";
            return RedirectToAction(nameof(Index));
        }


        // GET: SalesPersons/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var salesPerson = await _context.SalesPeople
                .FirstOrDefaultAsync(m => m.Id == id);
            if (salesPerson == null)
            {
                return NotFound();
            }

            return View(salesPerson);
        }

        // POST: SalesPersons/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var salesPerson = await _context.SalesPeople.FindAsync(id);
            if (salesPerson != null)
            {
                _context.SalesPeople.Remove(salesPerson);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SalesPersonExists(int id)
        {
            return _context.SalesPeople.Any(e => e.Id == id);
        }
    }
}
