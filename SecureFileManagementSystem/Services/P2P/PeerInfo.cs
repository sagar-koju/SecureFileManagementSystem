namespace SecureFileManagementSystem.Services
{
    public class PeerInfo
    {
        public required string Username { get; set; }
        public required string ConnectionId { get; set; } // Identifies the browser tab/session
        public DateTime LastSeen { get; set; }
    }
}