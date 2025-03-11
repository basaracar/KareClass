using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KareClass.Data;
using KareClass.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace KareClass.Controllers;

//[Authorize(Roles = "Admin")]
public class TimeSlotsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TimeSlotsController> _logger;

    public TimeSlotsController(ApplicationDbContext context, ILogger<TimeSlotsController> logger)
    {
        _context = context;
        _logger = logger;
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
        _logger.LogInformation($"Edit action başladı. ID: {id}, TimeSlotId: {timeSlot.TimeSlotId}");
        
        if (id != timeSlot.TimeSlotId)
        {
            _logger.LogWarning($"ID uyuşmazlığı. Route ID: {id}, Model ID: {timeSlot.TimeSlotId}");
            return Json(new { success = false, message = "ID uyuşmazlığı" });
        }

        if (ModelState.IsValid)
        {
            try
            {
                _logger.LogInformation($"Model geçerli. Güncelleme başlıyor. Değerler: Gün={timeSlot.DayOfWeek}, Başlangıç={timeSlot.StartTime}, Bitiş={timeSlot.EndTime}");
                _context.Update(timeSlot);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Güncelleme başarılı.");
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError($"Güncelleme hatası: {ex.Message}");
                if (!TimeSlotExists(timeSlot.TimeSlotId))
                {
                    return Json(new { success = false, message = "Kayıt bulunamadı" });
                }
                else
                {
                    return Json(new { success = false, message = "Güncelleme sırasında bir hata oluştu" });
                }
            }
        }
        
        var errors = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
            
        _logger.LogWarning($"Model doğrulama hataları: {string.Join(", ", errors)}");
        return Json(new { success = false, message = "Doğrulama hataları", errors = errors });
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