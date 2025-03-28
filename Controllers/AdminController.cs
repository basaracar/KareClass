using System.Linq;
using System.Threading.Tasks;
using KareClass.Data;
using KareClass.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KareClass.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.UserType != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var users = await _userManager.Users.OrderBy(x=>x.UserType).ToListAsync();
            
            // Öğrenci kullanıcıları için sınıf bilgilerini yükle
            foreach (var user in users.Where(u => u.UserType == "Student" && u.ClassId.HasValue))
            {
                user.Class = await _context.Classes.FindAsync(user.ClassId);
            }
            
            // Bölüm bilgilerini yükle
            foreach (var user in users.Where(u => u.DepartmentId.HasValue))
            {
                user.Department = await _context.Departments.FindAsync(user.DepartmentId);
            }
            
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.UserType != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            // Öğrenci ise sınıf bilgisini yükle
            if (user.UserType == "Student" && user.ClassId.HasValue)
            {
                user.Class = await _context.Classes.FindAsync(user.ClassId);
            }
            
            // Bölüm bilgisini yükle
            if (user.DepartmentId.HasValue)
            {
                user.Department = await _context.Departments.FindAsync(user.DepartmentId);
            }
            
            ViewData["Classes"] = await _context.Classes.ToListAsync();
            ViewData["Departments"] = await _context.Departments.ToListAsync();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, string firstName, string lastName, string userType, string? schoolNumber, int? departmentId, string? title, int? classId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.UserType != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            ViewData["Classes"] = await _context.Classes.ToListAsync();
            ViewData["Departments"] = await _context.Departments.ToListAsync();

            user.FirstName = firstName;
            user.LastName = lastName;
            user.UserType = userType;
            user.DepartmentId = departmentId;
            
            // Kullanıcı tipine göre ek alanları güncelle
            if (userType == "Student")
            {
                user.SchoolNumber = schoolNumber;
                user.ClassId = classId;
                user.Title = null; // Öğrenci için unvan temizle
            }
            else if (userType == "Teacher")
            {
                user.Title = title;
                user.SchoolNumber = null; // Öğretmen için öğrenci numarasını temizle
                user.ClassId = null; // Öğretmen için sınıf ilişkisini temizle
            }
            else
            {
                // Admin veya User için ek alanları temizle
                user.SchoolNumber = null;
                user.Title = null;
                user.ClassId = null;
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

        [HttpGet]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.UserType != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            // Öğrenci ise sınıf bilgisini yükle
            if (user.UserType == "Student" && user.ClassId.HasValue)
            {
                user.Class = await _context.Classes.FindAsync(user.ClassId);
            }
            
            // Bölüm bilgisini yükle
            if (user.DepartmentId.HasValue)
            {
                user.Department = await _context.Departments.FindAsync(user.DepartmentId);
            }
            var record=_context.AttendanceRecords.Where(x=>x.StudentId==id).ToList();
            ViewBag.delete=record.Any()?true:false;
            return View(user);
        }

        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || currentUser.UserType != "Admin")
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
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