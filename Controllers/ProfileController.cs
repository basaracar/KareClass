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