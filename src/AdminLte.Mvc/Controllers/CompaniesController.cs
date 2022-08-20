using AdminLte.Mvc.Models;
using Microsoft.AspNetCore.Mvc;

namespace AdminLte.Mvc.Controllers;

public class CompaniesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CompaniesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Companies
    public async Task<IActionResult> Index()
    {
        return _context.Companies != null ?
            View(await _context.Companies.ToListAsync()) :
            Problem("Entity set 'ApplicationDbContext.Companies'  is null.");
    }

    // GET: Companies/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null || _context.Companies == null)
        {
            return NotFound();
        }

        var company = await _context.Companies
            .FirstOrDefaultAsync(m => m.Id == id);
        if (company == null)
        {
            return NotFound();
        }

        return View(company);
    }

    // GET: Companies/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Companies/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,Description")] Company company)
    {
        if (ModelState.IsValid)
        {
            _context.Add(company);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(company);
    }

    // GET: Companies/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null || _context.Companies == null)
        {
            return NotFound();
        }

        var company = await _context.Companies.FindAsync(id);
        if (company == null)
        {
            return NotFound();
        }
        return View(company);
    }

    // POST: Companies/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Company company)
    {
        if (id != company.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(company);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CompanyExists(company.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(company);
    }

    // GET: Companies/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null || _context.Companies == null)
        {
            return NotFound();
        }

        var company = await _context.Companies
            .FirstOrDefaultAsync(m => m.Id == id);
        if (company == null)
        {
            return NotFound();
        }

        return View(company);
    }

    // POST: Companies/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (_context.Companies == null)
        {
            return Problem("Entity set 'ApplicationDbContext.Companies'  is null.");
        }
        var company = await _context.Companies.FindAsync(id);
        if (company != null)
        {
            _context.Companies.Remove(company);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool CompanyExists(int id)
    {
        return (_context.Companies?.Any(e => e.Id == id)).GetValueOrDefault();
    }
}