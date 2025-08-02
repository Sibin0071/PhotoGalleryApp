using Microsoft.AspNetCore.Mvc;
using PhotoGalleryApp.Data;
using PhotoGalleryApp.Models;
using Microsoft.EntityFrameworkCore;

namespace PhotoGalleryApp.Controllers
{
    [Route("chat/history")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ChatController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult GetHistory([FromQuery] string user1, [FromQuery] string user2)
        {
            var messages = _db.ChatMessages
                .Where(m => (m.FromUserEmail == user1 && m.ToUserEmail == user2) ||
                            (m.FromUserEmail == user2 && m.ToUserEmail == user1))
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.FromUserEmail,
                    m.ToUserEmail,
                    m.Message,
                    m.Timestamp
                })
                .ToList();

            return Ok(messages);
        }
    }
}
