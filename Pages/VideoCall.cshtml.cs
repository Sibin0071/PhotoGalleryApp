using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PhotoGalleryApp.Pages
{
    [Authorize]
    public class VideoCallModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
