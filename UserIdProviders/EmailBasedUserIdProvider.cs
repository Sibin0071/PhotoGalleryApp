using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace PhotoGalleryApp.UserIdProviders
{
    public class EmailBasedUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            // Use ClaimTypes.Email for accurate user matching
            return connection.User?.FindFirst(ClaimTypes.Email)?.Value ?? "";
        }
    }
}
