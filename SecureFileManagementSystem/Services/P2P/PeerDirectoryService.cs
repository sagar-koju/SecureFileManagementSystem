using System.Collections.Concurrent;

namespace SecureFileManagementSystem.Services
{
    public class PeerDirectoryService
    {
        // A thread-safe dictionary is essential for a multi-user web app
        private readonly ConcurrentDictionary<string, PeerInfo> _activePeers = new();

        // Announce or update a user's presence
        public void Announce(string username, string connectionId)
        {
            var peerInfo = new PeerInfo { Username = username, ConnectionId = connectionId, LastSeen = DateTime.UtcNow };
            _activePeers.AddOrUpdate(username, peerInfo, (key, oldInfo) => peerInfo);
        }

        // Get a list of all active peers, removing any that have timed out
        public List<PeerInfo> GetActivePeers()
        {
            var now = DateTime.UtcNow;
            // Remove peers not seen in over a minute (adjust as needed)
            var stalePeers = _activePeers.Where(p => (now - p.Value.LastSeen).TotalSeconds > 60).ToList();
            foreach (var peer in stalePeers)
            {
                _activePeers.TryRemove(peer.Key, out _);
            }
            return _activePeers.Values.ToList();
        }
    }
}