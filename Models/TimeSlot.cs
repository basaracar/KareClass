using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KareClass.Models;

namespace KareClass.Models;

public class TimeSlot
{
    [Key]
    public int TimeSlotId { get; set; }
    
    [Required]
    [MaxLength(10)]
    public string DayOfWeek { get; set; }
    
    [Required]
    public TimeSpan StartTime { get; set; }
    
    [Required]
    public TimeSpan EndTime { get; set; }

    // Display property
    [NotMapped]
    public string DisplayText => $"{DayOfWeek} {StartTime.ToString(@"hh\:mm")} - {EndTime.ToString(@"hh\:mm")}";

    // Navigation property
    public ICollection<Schedule> Schedules { get; set; }
} 