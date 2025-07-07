using Microsoft.AspNetCore.Mvc;
using SecureFileManagementSystem.Services;

public class PeersController : Controller
{
    private readonly PeerDirectoryService _peerDirectory;

    public PeersController(PeerDirectoryService peerDirectory)
    {
        _peerDirectory = peerDirectory;
    }

    // This action will display the list of online peers
    public IActionResult Index()
    {
        var myUsername = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(myUsername))
        {
            return RedirectToAction("Login", "Account");
        }

        // Get all peers and filter out the current user
        var otherPeers = _peerDirectory.GetActivePeers()
                                      .Where(p => p.Username != myUsername)
                                      .ToList();

        ViewBag.CurrentUser = myUsername;
        return View(otherPeers); // Pass the list of peers to the view
    }

    [HttpPost]
    public IActionResult Announce()
    {
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username)) return Unauthorized();

        _peerDirectory.Announce(username, HttpContext.Session.Id);
        return Ok(); // Just send a success status
    }
}