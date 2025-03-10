using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KareClass.Models;

namespace KareClass.Models;

public class Course
{
    [Key]
    public int CourseId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string CourseName { get; set; }
    
    public int? GradeLevel { get; set; }
    
    // Bölüm ilişkisi
    public int? DepartmentId { get; set; }

    // Navigation property
    public ICollection<Schedule>? Schedules { get; set; }
    
    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }
} 