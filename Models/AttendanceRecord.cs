using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KareClass.Models;

public class AttendanceRecord
{
    [Key]
    public int AttendanceRecordId { get; set; }
    
    public int AttendanceId { get; set; }
    
    public string StudentId { get; set; }
    
    public bool IsPresent { get; set; }
    
    public DateTime? JoinTime { get; set; }
    
    [ForeignKey("AttendanceId")]
    public Attendance Attendance { get; set; }
    
    [ForeignKey("StudentId")]
    public ApplicationUser Student { get; set; }
} 