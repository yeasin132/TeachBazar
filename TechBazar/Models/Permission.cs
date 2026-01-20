using System.ComponentModel.DataAnnotations;

namespace TechBazar.Models
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; } // e.g., "ManageProducts", "DeleteProducts"

        [Required]
        [StringLength(255)]
        public required string Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation property for many-to-many relationship
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
