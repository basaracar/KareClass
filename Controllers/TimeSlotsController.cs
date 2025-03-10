using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KareClass.Data;
using KareClass.Models;
using Microsoft.AspNetCore.Authorization;

namespace KareClass.Controllers;

//[Authorize(Roles = "Admin")]
public class TimeSlotsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TimeSlotsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: TimeSlots
    public async Task<IActionResult> Index()
    {
        return View(await _context.TimeSlots.ToListAsync());
    }

    // GET: TimeSlots/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var timeSlot = await _context.TimeSlots
            .FirstOrDefaultAsync(m => m.TimeSlotId == id);
        if (timeSlot == null)
        {
            return NotFound();
        }

        return View(timeSlot);
    }

    // GET: TimeSlots/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: TimeSlots/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TimeSlotId,DayOfWeek,StartTime,EndTime")] TimeSlot timeSlot)
    {
        if (ModelState.IsValid)
        {
            _context.Add(timeSlot);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(timeSlot);
    }

    // GET: TimeSlots/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var timeSlot = await _context.TimeSlots.FindAsync(id);
        if (timeSlot == null)
        {
            return NotFound();
        }
        return View(timeSlot);
    }

    // POST: TimeSlots/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("TimeSlotId,DayOfWeek,StartTime,EndTime")] TimeSlot timeSlot)
    {
        if (id != timeSlot.TimeSlotId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(timeSlot);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TimeSlotExists(timeSlot.TimeSlotId))
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
        return View(timeSlot);
    }

    // GET: TimeSlots/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var timeSlot = await _context.TimeSlots
            .FirstOrDefaultAsync(m => m.TimeSlotId == id);
        if (timeSlot == null)
        {
            return NotFound();
        }

        return View(timeSlot);
    }

    // POST: TimeSlots/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var timeSlot = await _context.TimeSlots.FindAsync(id);
        if (timeSlot != null)
        {
            _context.TimeSlots.Remove(timeSlot);
            await _context.SaveChangesAsync();
        }
        
        return RedirectToAction(nameof(Index));
    }

    private bool TimeSlotExists(int id)
    {
        return _context.TimeSlots.Any(e => e.TimeSlotId == id);
    }
} 