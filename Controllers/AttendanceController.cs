using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KareClass.Data;
using KareClass.Models;

namespace KareClass.Controllers
{
    [IgnoreAntiforgeryToken]
    public class AttendanceController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AttendanceController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IActionResult ScanQR()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> JoinAttendanceWithQR(int attendanceId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı." });
                }

                // Öğrencinin zaten yoklamaya katılıp katılmadığını kontrol et
                var existingAttendanceRecord = await _context.AttendanceRecords
                    .FirstOrDefaultAsync(ar => ar.AttendanceId == attendanceId && ar.StudentId == user.Id);

                if (existingAttendanceRecord != null)
                {
                    return Json(new { success = false, message = "Bu yoklamaya zaten katıldınız." });
                }

                // Yeni yoklama kaydı oluştur
                var attendanceRecord = new AttendanceRecord
                {
                    AttendanceId = attendanceId,
                    StudentId = user.Id,
                    JoinTime = DateTime.Now
                };

                _context.AttendanceRecords.Add(attendanceRecord);
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

        [HttpPost]
        public async Task<IActionResult> StartAttendance(int scheduleId)
        {
            try
            {
                Console.WriteLine($"Gelen scheduleId: {scheduleId}");

                var schedule = await _context.Schedules
                    .Include(s => s.Course)
                    .Include(s => s.Class)
                    .Include(s => s.TimeSlot)
                    .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    Console.WriteLine("Schedule bulunamadı!");
                    return Json(new { success = false, message = "Ders programı bulunamadı." });
                }

                Console.WriteLine($"Course: {schedule.Course?.CourseName ?? "null"}");
                Console.WriteLine($"Class: {schedule.Class?.ClassName ?? "null"}");
                Console.WriteLine($"TimeSlot: {schedule.TimeSlot?.StartTime.ToString() ?? "null"} - {schedule.TimeSlot?.EndTime.ToString() ?? "null"}");
               
                
                var attendance=_context.Attendances.FirstOrDefault(a=>a.ScheduleId==scheduleId && a.Date.Date==DateTime.Today);
                if(attendance!=null){
                    
                    return Json(new { success = false, message = "Bu ders için zaten yoklama alınmış." });
                }else{
               
                attendance = new Attendance
                {
                    ScheduleId = scheduleId,
                    Date = DateTime.Today,
                    IsActive = true
                };
                
                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();
                }
                var response = new { 
                    success = true,
                    attendanceId = attendance.AttendanceId
                
                };

                Console.WriteLine($"Başarılı yanıt: {System.Text.Json.JsonSerializer.Serialize(response)}");
                return Json(response); 
                
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HATA: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Bir hata oluştu: {ex.Message}" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> StopAttendance(int attendanceId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı." });
                }

                var attendance = await _context.Attendances
                    .Include(a => a.Schedule)
                    .FirstOrDefaultAsync(a => a.AttendanceId == attendanceId);

                if (attendance == null){
                    return Json(new { success = false, message = "Yoklama Bulunamadı." });
                }

                attendance.IsActive = false;
                attendance.EndTime = DateTime.Now;
                _context.Update(attendance);
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

        [HttpGet]
        [Route("Attendance/GetParticipants/{attendanceId}")]
        public async Task<IActionResult> GetParticipants(int attendanceId)
        {
            try
            {
                var participants = await _context.AttendanceRecords
                    .Where(ar => ar.AttendanceId == attendanceId)
                    .Include(ar => ar.Student)
                    .Select(ar => new
                    {
                        ar.Student.FirstName,
                        ar.Student.LastName,
                        ar.Student.SchoolNumber,
                        ar.JoinTime
                    })
                    .OrderByDescending(ar => ar.JoinTime)
                    .ToListAsync();

                return Json(participants);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HATA: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Katılımcılar getirilemedi: {ex.Message}" });
            }
        }
    }
} 