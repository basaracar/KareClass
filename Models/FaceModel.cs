using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KareClass.Models
{
    public class FaceModel{
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Descriptor { get; set; }
        }
}