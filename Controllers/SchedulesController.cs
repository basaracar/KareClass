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
            .Include(s => s.Teacher)
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

        var @class = await _context.Classes.FindAsync(ClassId);
        if (@class == null)
        {
            return NotFound("Sınıf bulunamadı");
        }

        // Gelen dizilerin aynı uzunlukta olduğunu kontrol et
        if (TimeSlotId.Length != CourseId.Length || TimeSlotId.Length != TeacherId.Length)
        {
            ModelState.AddModelError(string.Empty, "Veri tutarsızlığı: Tüm seçimler yapılmalıdır.");
            
            // Öğretmen tipindeki kullanıcıları getir
            var teachers = await _userManager.Users
                .Where(u => u.UserType == "Teacher")
                .Select(u => new { Id = u.Id, Name = u.Title + " " + u.FirstName + " " + u.LastName })
                .ToListAsync();
            
            // Sınıfa ait mevcut programları al
            var existingSchedules = await _context.Schedules
                .Include(s => s.Teacher)
                .Where(s => s.ClassId == ClassId)
                .ToDictionaryAsync(s => s.TimeSlotId, s => s);
            
            ViewData["ClassId"] = ClassId;
            ViewData["ClassName"] = @class.ClassName;
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName");
            ViewData["TeacherId"] = new SelectList(teachers, "Id", "Name");
            ViewData["TimeSlotId"] = new SelectList(_context.TimeSlots, "TimeSlotId", "DisplayText");
            ViewBag.ExistingSchedules = existingSchedules;
            
            return View();
        }

        // En az bir geçerli ders/öğretmen seçimi olup olmadığını kontrol et
        bool hasValidEntry = false;
        for (int i = 0; i < TimeSlotId.Length; i++)
        {
            if (CourseId[i] > 0 && !string.IsNullOrWhiteSpace(TeacherId[i]))
            {
                hasValidEntry = true;
                break;
            }
        }

        if (!hasValidEntry)
        {
            ModelState.AddModelError(string.Empty, "En az bir zaman dilimi için ders ve öğretmen seçimi yapmalısınız.");
            
            // Öğretmen tipindeki kullanıcıları getir
            var teachers = await _userManager.Users
                .Where(u => u.UserType == "Teacher")
                .Select(u => new { Id = u.Id, Name = u.Title + " " + u.FirstName + " " + u.LastName })
                .ToListAsync();
            
            // Sınıfa ait mevcut programları al
            var existingSchedules = await _context.Schedules
                .Include(s => s.Teacher)
                .Where(s => s.ClassId == ClassId)
                .ToDictionaryAsync(s => s.TimeSlotId, s => s);
            
            ViewData["ClassId"] = ClassId;
            ViewData["ClassName"] = @class.ClassName;
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName");
            ViewData["TeacherId"] = new SelectList(teachers, "Id", "Name");
            ViewData["TimeSlotId"] = new SelectList(_context.TimeSlots, "TimeSlotId", "DisplayText");
            ViewBag.ExistingSchedules = existingSchedules;
            
            return View();
        }

        // Transaction ile işlemi yap
        using var transaction = await _context.Database.BeginTransactionAsync();
        try 
        {
            // Her bir zaman dilimi için program kaydı oluştur
            for (int i = 0; i < TimeSlotId.Length; i++)
            {
                // Boş seçim kontrolü - Ders veya öğretmen seçilmemişse bu kaydı atla
                if (CourseId[i] <= 0 || string.IsNullOrWhiteSpace(TeacherId[i]))
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
            await transaction.CommitAsync();
            
            TempData["SuccessMessage"] = "Ders programı başarıyla kaydedildi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            
            // Hata durumunda formu yeniden göster
            ModelState.AddModelError(string.Empty, $"Ders programı kaydedilirken bir hata oluştu: {ex.Message}");
            
            // Öğretmen tipindeki kullanıcıları getir
            var teachers = await _userManager.Users
                .Where(u => u.UserType == "Teacher")
                .Select(u => new { Id = u.Id, Name = u.Title + " " + u.FirstName + " " + u.LastName })
                .ToListAsync();
            
            // Sınıfa ait mevcut programları al
            var existingSchedules = await _context.Schedules
                .Include(s => s.Teacher)
                .Where(s => s.ClassId == ClassId)
                .ToDictionaryAsync(s => s.TimeSlotId, s => s);
            
            ViewData["ClassId"] = ClassId;
            ViewData["ClassName"] = @class.ClassName;
            ViewData["CourseId"] = new SelectList(_context.Courses, "CourseId", "CourseName");
            ViewData["TeacherId"] = new SelectList(teachers, "Id", "Name");
            ViewData["TimeSlotId"] = new SelectList(_context.TimeSlots, "TimeSlotId", "DisplayText");
            ViewBag.ExistingSchedules = existingSchedules;
            
            return View();
        }
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
        if (courseId <= 0)
        {
            return Json(new List<object>());
        }

        try
        {
            // Önce ders bilgisini al
            var course = _context.Courses
                .Include(c => c.Department)
                .FirstOrDefault(c => c.CourseId == courseId);
                
            if (course == null)
            {
                return Json(new List<object>());
            }

            // Bu dersin bağlı olduğu departman var mı?
            int? departmentId = course.DepartmentId;

            // Öğretmen sorgusunu oluştur - sadece Teacher tipindeki kullanıcıları getir
            var query = _userManager.Users
                .AsNoTracking()
                .Where(u => u.UserType == "Teacher");

            // Departman varsa, o departmana ait öğretmenleri filtrele
            if (departmentId.HasValue)
            {
                query = query.Where(t => t.DepartmentId == departmentId.Value);
            }

            // Öncelikle bu dersi daha önce veren öğretmenleri bul
            var courseTeacherIds = _context.Schedules
                .AsNoTracking()
                .Where(s => s.CourseId == courseId)
                .Select(s => s.TeacherId)
                .Distinct()
                .ToList();

            // Öğretmenleri departmana ve daha önce dersi verme durumuna göre sırala
            var teachers = query
                .Select(u => new { 
                    value = u.Id, 
                    text = u.Title + " " + u.FirstName + " " + u.LastName,
                    isPreviousTeacher = courseTeacherIds.Contains(u.Id)
                })
                .OrderByDescending(t => t.isPreviousTeacher) // Önce bu dersi daha önce verenler
                .ThenBy(t => t.text) // Sonra alfabetik sıralama
                .Select(t => new { value = t.value, text = t.text })
                .ToList();

            return Json(teachers);
        }
        catch (Exception ex)
        {
            // Hata durumunda boş liste dön
            return Json(new List<object>());
        }
    }
} 