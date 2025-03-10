using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace KareClass.Models
{
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string DepartmentName { get; set; }
        
        // Navigation properties
        public ICollection<ApplicationUser>? Users { get; set; }
    }
} 