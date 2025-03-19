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
#if DEBUG

                var currentTime = DateTime.Now.TimeOfDay;
                var currentDay = DateTime.Now.DayOfWeek.ToString();
#else
                var currentTime = DateTime.Now.AddHours(8).TimeOfDay;
                var currentDay = DateTime.Now.AddHours(8).DayOfWeek.ToString();
#endif
                ViewBag.Time = currentTime;
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
                                                a.Date.Date == DateTime.Today);
                    ViewBag.CurrentLesson = new
                    {
                        ScheduleId = currentLesson.ScheduleId,
                        LessonName = currentLesson.Course.CourseName,
                        StartTime = currentLesson.TimeSlot.StartTime.ToString(@"hh\:mm"),
                        EndTime = currentLesson.TimeSlot.EndTime.ToString(@"hh\:mm"),
                        TeacherName = $"{currentLesson.Teacher.Title} {currentLesson.Teacher.FirstName} {currentLesson.Teacher.LastName}",
                        ClassName = currentLesson.Class.ClassName,
                        AttendanceId = activeAttendance != null ? activeAttendance.AttendanceId : 0,
                        IsActive = activeAttendance != null && activeAttendance.IsActive ? 1 : 0
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
        [Route("Profile/RollCall/{id}")]
        public async Task<IActionResult> RollCall(int id)
        {
            Console.WriteLine($"Gelen scheduleId: {id}");

            var schedule = await _context.Schedules
                .Include(s => s.Course)
                .Include(s => s.Class)
                .Include(s => s.TimeSlot)
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null)
            {
                Console.WriteLine("Schedule bulunamadı!");
                ViewBag.Error = "Program bulunamadı!";
                return View();
            }

            Console.WriteLine($"Course: {schedule.Course?.CourseName ?? "null"}");
            Console.WriteLine($"Class: {schedule.Class?.ClassName ?? "null"}");
            Console.WriteLine($"TimeSlot: {schedule.TimeSlot?.StartTime.ToString() ?? "null"} - {schedule.TimeSlot?.EndTime.ToString() ?? "null"}");


            ViewBag.ClassName = schedule.Class.ClassName;
            ViewBag.CourseName = schedule.Course.CourseName;
            ViewBag.StartTime = schedule.TimeSlot.StartTime.ToString(@"hh\:mm");
            ViewBag.EndTime = schedule.TimeSlot.EndTime.ToString(@"hh\:mm");
            ViewBag.TeacherName = $"{schedule.Teacher.Title} {schedule.Teacher.FirstName} {schedule.Teacher.LastName}";
            var attendance = _context.Attendances.FirstOrDefault(a => a.ScheduleId == id && a.Date.Date == DateTime.Today);
            if (attendance != null)
            {
                ViewBag.AttendanceId = attendance.AttendanceId;
                ViewBag.Error = "Bu Dersin Yoklaması Daha Önce Oluşturulmuş!";
                return View();
            }
            else
            {
                attendance = new Attendance
                {
                    ScheduleId = id,
                    Date = DateTime.Today,
                    IsActive = true
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();
            }
            ViewBag.AttendanceId = attendance.AttendanceId;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> TakeRollCall([FromBody] RollCallData data)
        {
            try
            {
                Console.WriteLine($"Gelen attendanceId: {data.attendanceId}");
                Console.WriteLine($"Gelen rollCall: {string.Join(", ", data.rollCall)}");

                var attendance = await _context.Attendances
                    .Include(a => a.Schedule)
                    .ThenInclude(s => s.Class)
                    .FirstOrDefaultAsync(a => a.AttendanceId == data.attendanceId);

                if (attendance == null)
                {
                    Console.WriteLine("Attendance bulunamadı!");
                    return Json(new { success = false, message = "Yoklama bulunamadı." });
                }

                var _user = new ApplicationUser();
                foreach (var studentId in data.rollCall)
                {
                    _user = await _context.Users.FirstOrDefaultAsync(u => u.SchoolNumber == studentId.ToString());
                    if (_user == null)
                    {
                        Console.WriteLine($"Öğrenci bulunamadı! ID: {studentId}");
                        continue;
                    }
                    var existingRecord = await _context.AttendanceRecords
                        .FirstOrDefaultAsync(ar => ar.AttendanceId == data.attendanceId && ar.StudentId == _user.Id);
                    if (existingRecord != null)
                    {
                        // Yoklama kaydı zaten varsa güncelle
                        existingRecord.IsPresent = true;
                        existingRecord.JoinTime = DateTime.Now;
                        _context.AttendanceRecords.Update(existingRecord);
                    }
                    else
                    {
                        // Yoklama kaydı yoksa yeni bir kayıt oluştur

                        var attendanceRecord = new AttendanceRecord
                        {
                            AttendanceId = data.attendanceId,
                            StudentId = _user.Id,
                            JoinTime = DateTime.Now,
                            IsPresent = true
                        };

                        _context.AttendanceRecords.Add(attendanceRecord);
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HATA: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        public class RollCallData
        {
            public int attendanceId { get; set; }
            public int[] rollCall { get; set; }
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