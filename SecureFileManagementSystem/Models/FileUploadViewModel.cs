using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SecureFileManagementSystem.Models
{
    public class FileUploadViewModel
    {
        [Required]
        public string? ReceiverUsername { get; set; }

        [Required]
        [Display(Name = "Select Files")]
        public List<IFormFile>? Files { get; set; }  // <-- Changed to List<IFormFile>

        public bool UseP2P { get; set; } // New toggle
    }
}
