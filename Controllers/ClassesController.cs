using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using KareClass.Data;
using KareClass.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace KareClass.Controllers;

//[Authorize(Roles = "Admin")]
public class ClassesController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;
    private readonly ApplicationDbContext _context;

    public ClassesController(
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

    public async Task<IActionResult> Index()
    {
        return View(await _context.Classes
            .Include(c => c.Department)
            .ToListAsync());
    }

    // MetadataModel sınıfını güncelle
    public class MetadataModel
    {
        public string tfjsVersion { get; set; }
        public string tmVersion { get; set; }
        public string packageVersion { get; set; }
        public string packageName { get; set; }
        public string timeStamp { get; set; }
        public object userMetadata { get; set; }
        public string modelName { get; set; }
        public string[] labels { get; set; }  // List<ModelStudent> yerine ModelStudent[] kullan
        public int imageSize { get; set; }
    }

    // ModelStudent sınıfını güncelle
    public class ModelStudent
    {
        [JsonPropertyName("Nu")]
        public string Nu { get; set; }

        [JsonPropertyName("Firstname")]
        public string Firstname { get; set; }

        [JsonPropertyName("Surname")]
        public string Surname { get; set; }

        [JsonPropertyName("Class")]
        public string Class { get; set; }
    }
    private static string RemoveTurkishCharacters(string input)
    {
        var replacements = new Dictionary<char, string>
    {
        { 'ı', "i" }, { 'ş', "s" }, { 'ğ', "g" }, { 'ü', "u" }, { 'ç', "c" }, { 'ö', "o" },
        { 'İ', "I" }, { 'Ş', "S" }, { 'Ğ', "G" }, { 'Ü', "U" }, { 'Ç', "C" }, { 'Ö', "O" }
    };

        foreach (var replacement in replacements)
        {
            input = input.Replace(replacement.Key.ToString(), replacement.Value);
        }
        return input;
    }
    // RegisterFromModel metodunu güncelle
    public async Task<IActionResult> RegisterFromModel()
    {
        var message = "";
        try
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(),
                "wwwroot", "js", "models", "metadata.json");

            if (!System.IO.File.Exists(path))
            {
                return Json(new { success = false, message = "Metadata dosyası bulunamadı" });
            }

            
                string jsonContent = await System.IO.File.ReadAllTextAsync(path);
                
                var options = new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var metadata = JsonSerializer.Deserialize<MetadataModel>(jsonContent, options);
           

            if (metadata?.labels == null || metadata.labels.Length == 0)
            {
                return Json(new { success = false, message = "Metadata içinde öğrenci bilgisi bulunamadı" });
            }
            // Öğrenci bilgilerini işleme
            var students = metadata.labels.Select(label => JsonSerializer.Deserialize<ModelStudent>(label)).ToList();

            foreach (var student in students)
            {
                if (student != null)
                {
                    var classId = await _context.Classes.FirstOrDefaultAsync(c => c.ClassName == student.Class);
                    // E-posta adresini oluştur (örneğin: ahmet.yilmaz@kareclass.com)
                    var studentEmail = RemoveTurkishCharacters($"{student.Firstname.ToLower()}.{student.Surname.ToLower()}@kareclass.com");

                    // Kullanıcı adını oluştur (özel karakterler olmadan, örneğin: ahmetyilmaz)
                    var studentUserName = RemoveTurkishCharacters($"{student.Firstname.ToLower()}{student.Surname.ToLower()}".Replace(".", "").Replace("@", "").Replace(" ", ""));
                    // Kullanıcıyı kontrol et
                    var studentUser = await _userManager.FindByEmailAsync(studentEmail);
                    var number=_context.Users.FirstOrDefault(x=>x.SchoolNumber==student.Nu);
                    if(number!=null){
                        message += $"Öğrenci kullanıcısı zaten var: {studentUser.UserName}\n";
                    }
                    else
                    if (studentUser == null)
                    {
                        studentUser = new ApplicationUser();
                        
                            studentUser.UserName = studentEmail;
                            studentUser.Email = studentEmail;
                            studentUser.FirstName = student.Firstname;
                            studentUser.LastName = student.Surname;
                            studentUser.UserType = "Student";
                            studentUser.SchoolNumber = student.Nu;
                            studentUser.DepartmentId = 1;
                            studentUser.ClassId = classId.ClassId;
                            studentUser.EmailConfirmed = true;
                        
                        var result = await _userManager.CreateAsync(studentUser, "Student123!");
                        if (!result.Succeeded)
                        {
                            throw new Exception("Öğrenci kullanıcısı oluşturulamadı: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                        message += $"Öğrenci kullanıcısı oluşturuldu: {studentUser.UserName}\n";
                        Console.WriteLine($"Öğrenci No: {student.Nu}, " +
                                            $"Ad: {student.Firstname}, " +
                                            $"Soyad: {student.Surname}, " +
                                            $"Sınıf: {student.Class}");
                    }else{
                        message += $"Öğrenci kullanıcısı zaten var: {studentUser.UserName}\n";
                    }
                



                }
            }

            
        }
        
            catch (JsonException ex) {
                message=$"JSON deserialization error: {ex.Message}";
                // Handle JSON error
            }
            catch (Exception ex) {
                message=$"Unexpected error: {ex.Message}"; 
                // Handle other errors
            }
            return Json(new
            {
                success = true,
                message = message,

            });
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var @class = await _context.Classes
            .Include(c => c.Department)
            .FirstOrDefaultAsync(m => m.ClassId == id);
        if (@class == null)
        {
            return NotFound();
        }

        return View(@class);
    }

    public IActionResult Create()
    {
        ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ClassId,ClassName,GradeLevel,DepartmentId")] Class @class)
    {
        if (ModelState.IsValid)
        {
            _context.Add(@class);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", @class.DepartmentId);
        return View(@class);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var @class = await _context.Classes.FindAsync(id);
        if (@class == null)
        {
            return NotFound();
        }
        ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", @class.DepartmentId);
        return View(@class);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ClassId,ClassName,GradeLevel,DepartmentId")] Class @class)
    {
        if (id != @class.ClassId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(@class);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClassExists(@class.ClassId))
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
        ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", @class.DepartmentId);
        return View(@class);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var @class = await _context.Classes.FindAsync(id);
        if (@class != null)
        {
            _context.Classes.Remove(@class);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        return Json(new { success = false });
    }

    private bool ClassExists(int id)
    {
        return _context.Classes.Any(e => e.ClassId == id);
    }
}