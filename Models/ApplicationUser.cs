using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KareClass.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserType { get; set; } // "Admin" veya "User" veya "Student" veya "Teacher"
        
        // Student ve Teacher için gerekli alanlar
        public string? SchoolNumber { get; set; } // Öğrenci numarası (Student için)
        public string? Title { get; set; } // Unvan (Teacher için)
        
        // Öğrenci için sınıf ilişkisi
        public int? ClassId { get; set; } // Öğrencinin sınıf ID'si (Student için)
        
        [ForeignKey("ClassId")]
        public virtual Class? Class { get; set; } // Öğrencinin sınıfı (Student için)
        
        // Bölüm ilişkisi
        public int? DepartmentId { get; set; }
        
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
          // Navigation property
    public ICollection<Schedule> Schedules { get; set; }
    }
}