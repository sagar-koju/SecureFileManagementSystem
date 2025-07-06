using System.ComponentModel.DataAnnotations;

namespace SecureFileManagementSystem.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

         [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }
    }
}
