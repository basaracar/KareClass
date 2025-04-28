using System.Threading.Tasks;
using KareClass.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using KareClass.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace KareClass.Controllers
{
    public class FaceController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;

        public FaceController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // public IActionResult FaceDetect()
        // {
        //     return View();
        // }

        [HttpGet]
        [Route("api/Face/GetAllFaces")]
        public async Task<IActionResult> GetAllFaces()
        {
            var faces = await _context.FaceModels.ToListAsync();
            return Json(faces);
        }

        [HttpPost]
        public async Task<IActionResult> SaveFace([FromBody] FaceModel faceModel)
        {
            if (faceModel == null || string.IsNullOrEmpty(faceModel.Name) || string.IsNullOrEmpty(faceModel.Descriptor))
            {
                return BadRequest("Geçersiz yüz verisi");
            }

            try
            {
                var existingFace = await _context.FaceModels
                    .FirstOrDefaultAsync(f => f.Name.Equals(faceModel.Name));
                if (existingFace != null){
                    return Ok(new {message="Bu numara ile bir yüz zaten mevcut"});
                }

                _context.FaceModels.Add(faceModel);
                await _context.SaveChangesAsync();
                return Ok(new { message = $"{faceModel.Name} numaralı kişinin yüz bilgileri başarıyla kaydedildi!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Yüz kaydetme hatası");
                return StatusCode(500, "Yüz kaydedilirken bir hata oluştu");
            }
        }
        public class Student{
            public string? SchoolNumber { get; set; }
        }
        [HttpPost]
        public async Task<IActionResult> GetStudentDetail([FromBody]Student _student)
        {
            if (_student == null)
            {
                return BadRequest("Geçersiz yüz verisi");
            }

            try
            {
                var student = await _context.Users
                .FirstOrDefaultAsync(u => u.SchoolNumber.Equals(_student.SchoolNumber));
                if (student != null)
                {
                    var className = await _context.Classes.FindAsync(student.ClassId);
                    var sinif="9A";
                    if (className != null)
                    {
                        sinif=className.ClassName;
                    }
                    return Json(new { status = "OK", 
                    data = student.FirstName+" " + student.LastName,
                    schoolNumber = student.SchoolNumber,
                    className = sinif
                         });    
                }
                else
                {
                    return Json(new { status = "FAIL", data = "Öğrenci bulunamadı!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öğrenci detay alma hatası");
                return Json(new { status = "FAIL", data = ex.Message });
            }
        }

    [HttpGet]
        [Route("Face/FaceDetect/{id}")]
        public async Task<IActionResult> FaceDetect(int id)
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

  var faces = await _context.FaceModels.ToListAsync();
            ViewBag.FaceData= Json(faces);
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


    }

}
