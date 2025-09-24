using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PhotoGalleryApp.Data;
using PhotoGalleryApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoGalleryApp.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserListModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserListModel(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<UserWithLastLogin> UsersWithLoginInfo { get; set; }

        public class UserWithLastLogin
        {
            public string Id { get; set; }
            public string Email { get; set; }
            public DateTime? LastLoginTime { get; set; }
        }

        public async Task OnGetAsync()
        {
            var users = _userManager.Users.ToList();

            // Get latest login for each user
            var loginHistory = await _context.UserLoginHistories
                .GroupBy(x => x.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    LastLogin = g.Max(x => x.LoginTime)
                })
                .ToDictionaryAsync(x => x.UserId, x => x.LastLogin);

            UsersWithLoginInfo = users.Select(u => new UserWithLastLogin
            {
                Id = u.Id,
                Email = u.Email,
                LastLoginTime = loginHistory.ContainsKey(u.Id)
    ? TimeZoneInfo.ConvertTimeFromUtc(loginHistory[u.Id], TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"))
    : null

            })
            .OrderByDescending(u => u.LastLoginTime)
            .ToList();
        }
    }
}
