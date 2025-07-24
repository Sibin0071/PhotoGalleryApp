using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PhotoGalleryApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public IndexModel(
            ILogger<IndexModel> logger,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty(SupportsGet = true)]
        public string? UserId { get; set; }

        public string EffectiveUserId { get; set; } = "";
        public string EffectiveUserEmail { get; set; } = "";
        public bool IsImpersonating { get; set; } = false;

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(currentUser);
            var isAdmin = roles.Contains("Admin");

            EffectiveUserId = UserId ?? currentUser.Id;

            if (!isAdmin && EffectiveUserId != currentUser.Id)
            {
                return Forbid();
            }

            var effectiveUser = await _userManager.FindByIdAsync(EffectiveUserId);
            if (effectiveUser == null)
            {
                return NotFound();
            }

            EffectiveUserEmail = effectiveUser.Email ?? "Unknown";
            IsImpersonating = isAdmin && EffectiveUserId != currentUser.Id;

            return Page();
        }
    }
}
