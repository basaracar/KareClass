using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KareClass.Models;

public class Attendance
{
    [Key]
    public int AttendanceId { get; set; }
    
    public int ScheduleId { get; set; }
    public DateTime Date { get; set; }
    public bool IsActive { get; set; }
    public DateTime? EndTime { get; set; }
    
    [ForeignKey("ScheduleId")]
    public Schedule Schedule { get; set; }
    
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; }
} 