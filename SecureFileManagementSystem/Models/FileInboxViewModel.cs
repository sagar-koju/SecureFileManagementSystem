using System;

namespace SecureFileManagementSystem.Models
{
    public class FileInboxViewModel
    {
        public int Id { get; set; }
        public string SenderUsername { get; set; }
        public string OriginalFileName { get; set; }
        public string EncryptedFileName { get; set; }

        public string SharedMethod { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
