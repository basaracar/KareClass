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
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;

        public AccountController(
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
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (ModelState.IsValid)
            {
                // E-posta ile kullanıcıyı bulun
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    // Şifre kontrolü
                    var result = await _signInManager.PasswordSignInAsync(user.UserName, password, rememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Kullanıcı giriş yaptı: " + email);
                        return RedirectToLocal(returnUrl);
                    }
                }
                
                ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre.");
                return View();
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Classes"] = await _context.Classes.ToListAsync();
            ViewData["Departments"] = await _context.Departments.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string firstName, string lastName, string userType, string? schoolNumber, string? title, int? departmentId, int? classId, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Classes"] = await _context.Classes.ToListAsync();
            ViewData["Departments"] = await _context.Departments.ToListAsync();
            
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser 
                { 
                    UserName = email, 
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    UserType = userType, // Kullanıcı tipi formdan geliyor
                    SchoolNumber = schoolNumber,
                    Title = title,
                    DepartmentId = departmentId
                };
                
                // Eğer kullanıcı öğrenci ise sınıf ataması yap
                if (userType == "Student" && classId.HasValue)
                {
                    user.ClassId = classId;
                }
                
                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Kullanıcı yeni bir hesap oluşturdu.");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToLocal(returnUrl);
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Kullanıcı çıkış yaptı.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
} 