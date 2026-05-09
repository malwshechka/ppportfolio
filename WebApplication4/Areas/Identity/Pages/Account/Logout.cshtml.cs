using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication4.Models;

namespace WebApplication4.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Пользователь вышел из системы.");

            returnUrl ??= Url.Content("~/");   // ← на главную страницу

            return LocalRedirect(returnUrl);
        }
    }
}