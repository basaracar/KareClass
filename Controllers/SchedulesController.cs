using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KareClass.Data;
using KareClass.Models;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

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

    // GET: Schedules/Create/5 (5 = ClassId)
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
    public async Task<IActionResult> Create(int ClassId, int[] TimeSlotId, int[] CourseId, string[] TeacherId)
    {
        if (ClassId <= 0)
        {
            return BadRequest("Geçersiz sınıf ID'si");
        }

        // Gelen dizilerin aynı uzunlukta olduğunu kontrol et
        if (TimeSlotId.Length != CourseId.Length || TimeSlotId.Length != TeacherId.Length)
        {
            ModelState.AddModelError(string.Empty, "Veri tutarsızlığı: Tüm seçimler yapılmalıdır.");
            
            var @class = await _context.Classes.FindAsync(ClassId);
            
            // Öğretmen tipindeki kullanıcıları getir
            var teachers = await _userManager.Users
                .Where(u => u.UserType == "Teacher")
                .Select(u => new { Id = u.Id, Name = u.Title + " " + u.FirstName + " " + u.LastName })
                .ToListAsync();
            
            ViewData["ClassId"] = ClassId;
            ViewData["ClassName"] = @class?.ClassName;
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName");
            ViewData["TeacherId"] = new SelectList(teachers, "Id", "Name");
            ViewData["TimeSlotId"] = new SelectList(_context.TimeSlots, "TimeSlotId", "DisplayText");
            
            return View();
        }

        // Her bir zaman dilimi için program kaydı oluştur
        for (int i = 0; i < TimeSlotId.Length; i++)
        {
            // Boş seçim kontrolü
            if (CourseId[i] <= 0 || string.IsNullOrEmpty(TeacherId[i]))
            {
                continue; // Boş seçimleri atla
            }

            // Çakışma kontrolü - aynı sınıf ve zaman diliminde başka bir ders var mı?
            var existingSchedule = await _context.Schedules
                .FirstOrDefaultAsync(s => s.ClassId == ClassId && s.TimeSlotId == TimeSlotId[i]);

            if (existingSchedule != null)
            {
                // Mevcut kaydı güncelle
                existingSchedule.CourseId = CourseId[i];
                existingSchedule.TeacherId = TeacherId[i];
                _context.Update(existingSchedule);
            }
            else
            {
                // Yeni kayıt oluştur
                var schedule = new Schedule
                {
                    ClassId = ClassId,
                    TimeSlotId = TimeSlotId[i],
                    CourseId = CourseId[i],
                    TeacherId = TeacherId[i]
                };
                _context.Add(schedule);
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: Schedules/Edit/5
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