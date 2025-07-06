namespace SecureFileManagementSystem.Models
{
    public class FileMetaData
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? Sender { get; set; }
        public string? Receiver { get; set; }
        public required string EncryptedAESKey { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}
