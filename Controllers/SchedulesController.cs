using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KareClass.Data;
using KareClass.Models;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace KareClass.Controllers;

public class SchedulesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public SchedulesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Schedules
   // 
    public async Task<IActionResult> Index()
    {
        // Tüm sınıfları ve ilgili programları getir
        var classes = await _context.Classes
            .Include(c => c.Schedules)
                .ThenInclude(s => s.Course)
            .Include(c => c.Schedules)
                .ThenInclude(s => s.Teacher)
            .Include(c => c.Schedules)
                .ThenInclude(s => s.TimeSlot)
            .ToListAsync();

        return View(classes);
    }

    // GET: Schedules/Details/5
    //
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var schedule = await _context.Schedules
            .Include(s => s.Class)
            .Include(s => s.Course)
            .Include(s => s.Teacher)
            .Include(s => s.TimeSlot)
            .FirstOrDefaultAsync(m => m.ScheduleId == id);

        if (schedule == null)
        {
            return NotFound();
        }

        return View(schedule);
    }

    // GET: Schedules/Create
    
    public async Task<IActionResult> Create(int id)
    {
        var @class = await _context.Classes.FindAsync(id);
        if (@class == null)
        {
            return NotFound();
        }

        // Sınıfa ait mevcut programları al
        var existingSchedules = await _context.Schedules
            .Where(s => s.ClassId == id)
            .ToDictionaryAsync(s => s.TimeSlotId, s => s);

        // Öğretmen tipindeki kullanıcıları getir
        var teachers = await _userManager.Users
            .Where(u => u.UserType == "Teacher")
            .Select(u => new { Id = u.Id, Name = u.Title + " " + u.FirstName + " " + u.LastName })
            .ToListAsync();

        ViewData["ClassId"] = id;
        ViewData["ClassName"] = @class.ClassName;
        ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName");
        ViewData["TeacherId"] = new SelectList(teachers, "Id", "Name");
        ViewData["TimeSlotId"] = new SelectList(_context.TimeSlots, "TimeSlotId", "DisplayText");
        ViewBag.ExistingSchedules = existingSchedules;

        return View();
    }

    // POST: Schedules/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    //
    public async Task<IActionResult> Create([Bind("ScheduleId,ClassId,CourseId,TeacherId,TimeSlotId")] Schedule schedule)
    {
        // ... existing code ...
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: Schedules/Edit/5
    //
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var schedule = await _context.Schedules
            .Include(s => s.Class)
            .FirstOrDefaultAsync(s => s.ScheduleId == id);

        if (schedule == null)
        {
            return NotFound();
        }

        // Öğretmen tipindeki kullanıcıları getir
        var teachers = await _userManager.Users
            .Where(u => u.UserType == "Teacher")
            .Select(u => new { Id = u.Id, Name = u.Title + " " + u.FirstName + " " + u.LastName })
            .ToListAsync();

        ViewData["ClassId"] = schedule.ClassId;
        ViewData["ClassName"] = schedule.Class.ClassName;
        ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName", schedule.CourseId);
        ViewData["TeacherId"] = new SelectList(teachers, "Id", "Name", schedule.TeacherId);
        ViewData["TimeSlotId"] = new SelectList(_context.TimeSlots, "TimeSlotId", "DisplayText", schedule.TimeSlotId);

        return View(schedule);
    }

    // POST: Schedules/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    //
    public async Task<IActionResult> Edit(int id, [Bind("ScheduleId,ClassId,CourseId,TeacherId,TimeSlotId")] Schedule schedule)
    {
        if (id != schedule.ScheduleId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(schedule);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ScheduleExists(schedule.ScheduleId))
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

        // Öğretmen tipindeki kullanıcıları getir
        var teachers = await _userManager.Users
            .Where(u => u.UserType == "Teacher")
            .Select(u => new { Id = u.Id, Name = u.Title + " " + u.FirstName + " " + u.LastName })
            .ToListAsync();

        ViewData["ClassId"] = schedule.ClassId;
        ViewData["ClassName"] = _context.Classes.Find(schedule.ClassId)?.ClassName;
        ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName", schedule.CourseId);
        ViewData["TeacherId"] = new SelectList(teachers, "Id", "Name", schedule.TeacherId);
        ViewData["TimeSlotId"] = new SelectList(_context.TimeSlots, "TimeSlotId", "DisplayText", schedule.TimeSlotId);

        return View(schedule);
    }

    // GET: Schedules/Delete/5
    //
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var schedule = await _context.Schedules
            .FirstOrDefaultAsync(m => m.ScheduleId == id);

        if (schedule == null)
        {
            return NotFound();
        }

        return View(schedule);
    }

    // POST: Schedules/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    //
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var schedule = await _context.Schedules.FindAsync(id);
        _context.Schedules.Remove(schedule);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    //[Authorize(Roles = "Student")]
    public async Task<IActionResult> MySchedule()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || user.UserType != "Student" || !user.ClassId.HasValue)
        {
            return Unauthorized();
        }

        var schedules = await _context.Schedules
            .Include(s => s.Class)
            .Include(s => s.Course)
            .Include(s => s.Teacher)
            .Include(s => s.TimeSlot)
            .Where(s => s.ClassId == user.ClassId)
            .ToListAsync();

        return View("StudentSchedule", schedules);
    }

    //[Authorize(Roles = "Teacher")]
    public async Task<IActionResult> TeacherSchedule()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || user.UserType != "Teacher")
        {
            return Unauthorized();
        }

        var schedules = await _context.Schedules
            .Include(s => s.Class)
            .Include(s => s.Course)
            .Include(s => s.Teacher)
            .Include(s => s.TimeSlot)
            .Where(s => s.TeacherId == user.Id)
            .ToListAsync();

        return View("TeacherSchedule", schedules);
    }

    private bool ScheduleExists(int id)
    {
        return _context.Schedules.Any(e => e.ScheduleId == id);
    }

    public IActionResult GetTeachersByCourse(int courseId)
    {
        var teachers = _userManager.Users
            .Where(u => u.UserType == "Teacher")
            .Select(u => new { value = u.Id.ToString(), text = u.Title + " " + u.FirstName + " " + u.LastName })
            .ToList();
        
        return Json(teachers);
    }
} 