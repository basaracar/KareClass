using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KareClass.Models;

namespace KareClass.Models;

public class Class
{
    [Key]
    public int ClassId { get; set; }

    [Required]
    [MaxLength(10)]
    public string ClassName { get; set; }

    public int? GradeLevel { get; set; }
    
    // Bölüm ilişkisi
    public int? DepartmentId { get; set; }
   
    // Navigation properties
    public ICollection<Schedule>? Schedules { get; set; }

    // ApplicationUser referansı (öğrenciler için)
    public ICollection<ApplicationUser>? Students { get; set; }
    
    // Department ilişkisi
    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }
   
}