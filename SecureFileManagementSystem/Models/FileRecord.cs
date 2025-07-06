using System;

namespace SecureFileManagementSystem.Models
{
    public class FileRecord
    {
        public int Id { get; set; }
        public string SenderUsername { get; set; }
        public string ReceiverUsername { get; set; }
        public string OriginalFileName { get; set; }
        public string EncryptedFileName { get; set; }
        public string EncryptedAESKeyBase64 { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
