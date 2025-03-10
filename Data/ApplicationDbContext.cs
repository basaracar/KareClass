using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KareClass.Models;
using Microsoft.AspNetCore.Identity;

namespace KareClass.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Class> Classes { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<TimeSlot> TimeSlots { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<Department> Departments { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<Schedule>()
            .HasOne(s => s.Class)
            .WithMany(c => c.Schedules)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Schedule>()
            .HasOne(s => s.Course)
            .WithMany(c => c.Schedules)
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Schedule>()
            .HasOne(s => s.Teacher)
            .WithMany(t => t.Schedules)
            .HasForeignKey(s => s.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Schedule>()
            .HasOne(s => s.TimeSlot)
            .WithMany(ts => ts.Schedules)
            .HasForeignKey(s => s.TimeSlotId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Class ve Department arasındaki ilişki konfigürasyonu
        builder.Entity<Class>()
            .HasOne(c => c.Department)
            .WithMany()
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Course ve Department arasındaki ilişki konfigürasyonu
        builder.Entity<Course>()
            .HasOne(c => c.Department)
            .WithMany()
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public async Task SeedCourses()
    {
        if (!Courses.Any())
        {
            var courses = new[]
            {
                "Matematik",
                "Türkçe",
                "Fizik",
                "Kimya",
                "Biyoloji",
                "Tarih",
                "Coğrafya",
                "İngilizce",
                "Almanca",
                "Fransızca",
                "Beden Eğitimi",
                "Müzik",
                "Görsel Sanatlar",
                "Felsefe",
                "Din Kültürü ve Ahlak Bilgisi"
            }
            .Select(courseName => new Course
            {
                CourseName = courseName
            });

            await Courses.AddRangeAsync(courses);
            await SaveChangesAsync();
        }
    }

    public async Task SeedDepartments()
    {
        if (!Departments.Any())
        {
            var departments = new List<Department>
            {
                new Department { DepartmentName = "Bilişim Teknolojileri" },
                new Department { DepartmentName = "Elektrik-Elektronik Teknolojileri" },
                new Department { DepartmentName = "Mobilya Teknolojileri" },
                new Department { DepartmentName = "Metal Teknolojileri" },
                new Department { DepartmentName = "Gıda Teknolojileri" },
                new Department { DepartmentName = "Matematik" },
                new Department { DepartmentName = "Fizik" },
                new Department { DepartmentName = "Kimya" },
                new Department { DepartmentName = "Biyoloji" },
                new Department { DepartmentName = "Tarih" },
                new Department { DepartmentName = "Coğrafya" },
                new Department { DepartmentName = "İngilizce" },
                new Department { DepartmentName = "Almanca" },
                new Department { DepartmentName = "Fransızca" },
                new Department { DepartmentName = "Beden Eğitimi" },
                new Department { DepartmentName = "Müzik" },
                new Department { DepartmentName = "Görsel Sanatlar" },
                new Department { DepartmentName = "Felsefe" },
                new Department { DepartmentName = "Din Kültürü ve Ahlak Bilgisi" }
                
            };

            await Departments.AddRangeAsync(departments);
            await SaveChangesAsync();
        }
    }
public async Task SeedTimeSlots()
    {
        if (!TimeSlots.Any())
        {
            var timeSlots = new List<TimeSlot>();
            var times = new[]
            {
                (start: "08:30", end: "09:10"),
                (start: "09:20", end: "10:00"),
                (start: "10:10", end: "10:50"),
                (start: "11:00", end: "11:40"),
                (start: "11:50", end: "12:30"),
                (start: "13:30", end: "14:10"),
                (start: "14:20", end: "15:00"),
                (start: "15:10", end: "15:50"),
                (start: "16:00", end: "16:40"),
                (start: "16:50", end: "17:30")
            };

            var days = new[] { "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma" };

            foreach (var day in days)
            {
                foreach (var time in times)
                {
                    timeSlots.Add(new TimeSlot
                    {
                        DayOfWeek = day,
                        StartTime = TimeSpan.Parse(time.start),
                        EndTime = TimeSpan.Parse(time.end)
                    });
                }
            }

            await TimeSlots.AddRangeAsync(timeSlots);
            await SaveChangesAsync();
        }
    }

    public async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
    {
        // Bölümlerin oluşturulduğundan emin olalım
        await SeedDepartments();
        
        // Bilgisayar Mühendisliği bölümünü alalım
        var computerEngDepartment = await Departments.FirstOrDefaultAsync(d => d.DepartmentName == "Bilgisayar Mühendisliği");
        
        // Admin kullanıcısı oluştur
        var adminEmail = "admin@kareclass.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                UserType = "Admin",
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (!result.Succeeded)
            {
                throw new Exception("Admin kullanıcısı oluşturulamadı: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        
        // Örnek öğretmen kullanıcısı oluştur
        var teacherEmail = "teacher@kareclass.com";
        var teacherUser = await userManager.FindByEmailAsync(teacherEmail);
        
        if (teacherUser == null)
        {
            teacherUser = new ApplicationUser
            {
                UserName = teacherEmail,
                Email = teacherEmail,
                FirstName = "Örnek",
                LastName = "Öğretmen",
                UserType = "Teacher",
                Title = "Dr.",
                DepartmentId = computerEngDepartment?.DepartmentId,
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(teacherUser, "Teacher123!");
            if (!result.Succeeded)
            {
                throw new Exception("Öğretmen kullanıcısı oluşturulamadı: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        


        
        // Örnek öğrenci kullanıcısı oluştur
        var studentEmail = "student@kareclass.com";
        var studentUser = await userManager.FindByEmailAsync(studentEmail);
        
        if (studentUser == null)
        {
            // 9-A sınıfını alalım
            var class9A = await Classes.FirstOrDefaultAsync(c => c.ClassName == "9-A");
            
            studentUser = new ApplicationUser
            {
                UserName = studentEmail,
                Email = studentEmail,
                FirstName = "Örnek",
                LastName = "Öğrenci",
                UserType = "Student",
                SchoolNumber = "2023001",
                DepartmentId = computerEngDepartment?.DepartmentId,
                ClassId = class9A?.ClassId,
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(studentUser, "Student123!");
            if (!result.Succeeded)
            {
                throw new Exception("Öğrenci kullanıcısı oluşturulamadı: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    public async Task SeedClasses()
    {
        // Önce departmanların oluşturulduğundan emin olalım
        await SeedDepartments();
        
        if (!Classes.Any())
        {
            // Departmanları alalım
            var matDept = await Departments.FirstOrDefaultAsync(d => d.DepartmentName == "Bilişim Teknolojileri");
            var fizikDept = await Departments.FirstOrDefaultAsync(d => d.DepartmentName == "Elektrik-Elektronik Teknolojileri");
            var kimyaDept = await Departments.FirstOrDefaultAsync(d => d.DepartmentName == "Metal Teknolojileri");
            var biyoDept = await Departments.FirstOrDefaultAsync(d => d.DepartmentName == "Mobilya Teknolojileri");
            
            var classes = new List<Class>
            {
                new Class { ClassName = "9-A", GradeLevel = 9, DepartmentId = matDept?.DepartmentId },
                new Class { ClassName = "9-B", GradeLevel = 9, DepartmentId = fizikDept?.DepartmentId },
                new Class { ClassName = "10-A", GradeLevel = 10, DepartmentId = kimyaDept?.DepartmentId },
                new Class { ClassName = "10-B", GradeLevel = 10, DepartmentId = biyoDept?.DepartmentId },
                new Class { ClassName = "11-A", GradeLevel = 11, DepartmentId = matDept?.DepartmentId },
                new Class { ClassName = "11-B", GradeLevel = 11, DepartmentId = fizikDept?.DepartmentId },
                new Class { ClassName = "12-A", GradeLevel = 12, DepartmentId = kimyaDept?.DepartmentId },
                new Class { ClassName = "12-B", GradeLevel = 12, DepartmentId = biyoDept?.DepartmentId }
            };

            await Classes.AddRangeAsync(classes);
            await SaveChangesAsync();
        }
    }
}
