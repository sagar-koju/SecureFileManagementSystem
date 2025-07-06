using System.ComponentModel.DataAnnotations;

namespace SecureFileManagementSystem.Models
{
    public class UserRecord
    {
        [Key]  
        public int Id { get; set; }
        public string? Username { get; set; }
        public required string PasswordHash { get; set; }
        public required string PublicKey { get; set; }
        public required string PrivateKey { get; set; }
    }
}
