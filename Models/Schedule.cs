using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KareClass.Models;

namespace KareClass.Models;

public class Schedule
{
    [Key]
    public int ScheduleId { get; set; }
    
    public int ClassId { get; set; }
    public int CourseId { get; set; }
    public string TeacherId { get; set; } // ApplicationUser.Id (Teacher tipindeki kullanıcılar için)
    public int TimeSlotId { get; set; }

    // Navigation properties
    [ForeignKey("ClassId")]
    public Class Class { get; set; }
    
    [ForeignKey("CourseId")]
    public Course Course { get; set; }
    
    [ForeignKey("TeacherId")]
    public ApplicationUser Teacher { get; set; } // Teacher tipindeki ApplicationUser
    
    [ForeignKey("TimeSlotId")]
    public TimeSlot TimeSlot { get; set; }
} 