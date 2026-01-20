using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechBazar.Models
{
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string RoleId { get; set; }

        [Required]
        public int PermissionId { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("RoleId")]
        public required IdentityRole Role { get; set; }

        [ForeignKey("PermissionId")]
        public required Permission Permission { get; set; }
    }
}
