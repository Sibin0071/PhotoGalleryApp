using Microsoft.AspNetCore.SignalR;
using PhotoGalleryApp.Data;
using PhotoGalleryApp.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Concurrent;

namespace PhotoGalleryApp.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        // Tracks connectionId per userEmail
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();

        public ChatHub(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task RegisterUser(string userEmail)
        {
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                var normalizedEmail = userEmail.ToLowerInvariant();
                UserConnections[normalizedEmail] = Context.ConnectionId;
                Console.WriteLine($"✅ Registered: {normalizedEmail} => {Context.ConnectionId}");

                // ✅ FIX: Await the async method
                await BroadcastOnlineUsers();
            }
        }

        public async Task SendMessage(string senderEmail, string receiverEmail, string message)
        {
            // Save message to DB (only once)
            var chatMsg = new ChatMessage
            {
                FromUserEmail = senderEmail,
                ToUserEmail = receiverEmail,
                Message = message,
                Timestamp = DateTime.UtcNow
            };
            _db.ChatMessages.Add(chatMsg);
            await _db.SaveChangesAsync();

            // Send to Receiver
            if (UserConnections.TryGetValue(receiverEmail.ToLowerInvariant(), out var receiverConnId))
            {
                await Clients.Client(receiverConnId)
                    .SendAsync("ReceiveMessage", senderEmail, message, receiverEmail); // FIXED: Added receiverEmail
            }

            // Send to Sender
            if (UserConnections.TryGetValue(senderEmail.ToLowerInvariant(), out var senderConnId))
            {
                await Clients.Client(senderConnId)
                    .SendAsync("ReceiveMessage", senderEmail, message, receiverEmail); // FIXED: Added receiverEmail
            }
        }

        public async Task Typing(string senderEmail, string receiverEmail)
        {
            if (UserConnections.TryGetValue(receiverEmail.ToLowerInvariant(), out var connectionId))
            {
                await Clients.Client(connectionId)
                    .SendAsync("ShowTypingIndicator", senderEmail);
            }
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var user = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (!string.IsNullOrEmpty(user))
            {
                UserConnections.TryRemove(user, out _);
                Console.WriteLine($"❌ Disconnected: {user}");
            }

            return base.OnDisconnectedAsync(exception);
        }
        private Task BroadcastOnlineUsers()
        {
            var onlineEmails = UserConnections.Keys.ToList();
            return Clients.All.SendAsync("UpdateOnlineUsers", onlineEmails);
        }
    }
}
