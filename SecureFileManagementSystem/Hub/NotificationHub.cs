using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SecureFileManagementSystem.Hubs
{
    public class NotificationHub : Hub
    {
        // When a user connects, add them to a "group" named after their username.
        // This allows us to send a message specifically to that user.
        public override Task OnConnectedAsync()
        {
            // We will get the username from the session for simplicity
            var username = Context.GetHttpContext()?.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                Groups.AddToGroupAsync(Context.ConnectionId, username);
            }
            return base.OnConnectedAsync();
        }
    }
}