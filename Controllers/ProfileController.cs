using System.Threading.Tasks;
using KareClass.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KareClass.Data;
using Microsoft.EntityFrameworkCore;

namespace KareClass.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Eğer kullanıcı öğrenci ise sınıf bilgisini yükle
            if (user.UserType == "Student" && user.ClassId.HasValue)
            {
                user.Class = await _context.Classes.FindAsync(user.ClassId);
            }

            // Bölüm bilgisini yükle
            if (user.DepartmentId.HasValue)
            {
                user.Department = await _context.Departments.FindAsync(user.DepartmentId);
            }

            // Mevcut dersi kontrol et
            if (user.UserType == "Student" || user.UserType == "Teacher")
            {
                var currentTime = DateTime.Now.TimeOfDay;
                var currentDay = DateTime.Now.DayOfWeek.ToString();

                // Türkçe gün adına çevir
                var turkishDay = currentDay switch
                {
                    "Monday" => "Pazartesi",
                    "Tuesday" => "Salı",
                    "Wednesday" => "Çarşamba",
                    "Thursday" => "Perşembe",
                    "Friday" => "Cuma",
                    "Saturday" => "Cumartesi",
                    "Sunday" => "Pazar",
                    _ => currentDay
                };

                var currentLesson = await _context.Schedules
                    .Include(s => s.Course)
                    .Include(s => s.Teacher)
                    .Include(s => s.TimeSlot)
                    .Include(s => s.Class)
                    .Where(s => s.TimeSlot.DayOfWeek == turkishDay &&
                              s.TimeSlot.StartTime <= currentTime &&
                              s.TimeSlot.EndTime >= currentTime)
                    .FirstOrDefaultAsync(s => user.UserType == "Student"
                        ? s.ClassId == user.ClassId
                        : s.TeacherId == user.Id);


                if (currentLesson != null)
                {
                    // Aktif yoklama kontrolü
                    var activeAttendance = await _context.Attendances
                        .FirstOrDefaultAsync(a => a.ScheduleId == currentLesson.ScheduleId &&
                                                a.IsActive &&
                                                a.Date.Date == DateTime.Today);
                    ViewBag.CurrentLesson = new
                    {
                        ScheduleId = currentLesson.ScheduleId,
                        LessonName = currentLesson.Course.CourseName,
                        StartTime = currentLesson.TimeSlot.StartTime.ToString(@"hh\:mm"),
                        EndTime = currentLesson.TimeSlot.EndTime.ToString(@"hh\:mm"),
                        TeacherName = $"{currentLesson.Teacher.Title} {currentLesson.Teacher.FirstName} {currentLesson.Teacher.LastName}",
                        ClassName = currentLesson.Class.ClassName,
                        AttendanceId = activeAttendance != null ? activeAttendance.AttendanceId : 0
                    };



                    if (activeAttendance != null)
                    {
                        ViewBag.ActiveAttendance = new
                        {
                            AttendanceId = activeAttendance.AttendanceId
                        };

                        // Öğrenci için yoklamaya katılım kontrolü
                        if (user.UserType == "Student")
                        {
                            var hasJoined = await _context.AttendanceRecords
                                .AnyAsync(ar => ar.AttendanceId == activeAttendance.AttendanceId &&
                                              ar.StudentId == user.Id);
                            ViewBag.HasJoinedAttendance = hasJoined;

                            // Debug için kontrol noktaları
                            Console.WriteLine($"Aktif Yoklama ID: {activeAttendance.AttendanceId}");
                            Console.WriteLine($"Öğrenci Katılım Durumu: {hasJoined}");
                        }
                    }
                    else
                    {
                        // Debug için kontrol noktası
                        Console.WriteLine($"Aktif yoklama bulunamadı. Schedule ID: {currentLesson.ScheduleId}");
                    }
                }
            }

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            ViewData["Classes"] = await _context.Classes.ToListAsync();
            ViewData["Departments"] = await _context.Departments.ToListAsync();

            // Eğer kullanıcı öğrenci ise sınıf bilgisini yükle
            if (user.UserType == "Student" && user.ClassId.HasValue)
            {
                user.Class = await _context.Classes.FindAsync(user.ClassId);
            }

            // Bölüm bilgisini yükle
            if (user.DepartmentId.HasValue)
            {
                user.Department = await _context.Departments.FindAsync(user.DepartmentId);
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string firstName, string lastName, string? schoolNumber, int? departmentId, string? title, int? classId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            ViewData["Classes"] = await _context.Classes.ToListAsync();
            ViewData["Departments"] = await _context.Departments.ToListAsync();

            user.FirstName = firstName;
            user.LastName = lastName;
            user.DepartmentId = departmentId;

            // Kullanıcı tipine göre ek alanları güncelle
            if (user.UserType == "Student")
            {
                user.SchoolNumber = schoolNumber;
                user.ClassId = classId;
            }
            else if (user.UserType == "Teacher")
            {
                user.Title = title;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(user);
        }
    }
}