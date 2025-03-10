using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KareClass.Data;
using KareClass.Models;
using Microsoft.AspNetCore.Authorization;

namespace KareClass.Controllers;

//[Authorize(Roles = "Admin")]
public class ClassesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ClassesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Classes
            .Include(c => c.Department)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var @class = await _context.Classes
            .Include(c => c.Department)
            .FirstOrDefaultAsync(m => m.ClassId == id);
        if (@class == null)
        {
            return NotFound();
        }

        return View(@class);
    }

    public IActionResult Create()
    {
        ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ClassId,ClassName,GradeLevel,DepartmentId")] Class @class)
    {
        if (ModelState.IsValid)
        {
            _context.Add(@class);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", @class.DepartmentId);
        return View(@class);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var @class = await _context.Classes.FindAsync(id);
        if (@class == null)
        {
            return NotFound();
        }
        ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", @class.DepartmentId);
        return View(@class);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ClassId,ClassName,GradeLevel,DepartmentId")] Class @class)
    {
        if (id != @class.ClassId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(@class);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClassExists(@class.ClassId))
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
        ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", @class.DepartmentId);
        return View(@class);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var @class = await _context.Classes.FindAsync(id);
        if (@class != null)
        {
            _context.Classes.Remove(@class);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        return Json(new { success = false });
    }

    private bool ClassExists(int id)
    {
        return _context.Classes.Any(e => e.ClassId == id);
    }
} 