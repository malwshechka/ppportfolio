using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication4.Models;

namespace WebApplication4.Areas.Identity.Pages.Account
{
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<ExternalLoginModel> logger)
        {
            _signInManager = signInManager;

            _userManager = userManager;

            _logger = logger;
        }

        public IActionResult OnPost(
            string provider,
            string returnUrl = null)
        {
            var redirectUrl = Url.Page(
                "./ExternalLogin",
                pageHandler: "Callback",
                values: new { returnUrl });

            var properties =
                _signInManager
                .ConfigureExternalAuthenticationProperties(
                    provider,
                    redirectUrl);

            return new ChallengeResult(
                provider,
                properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(
            string returnUrl = null,
            string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                return RedirectToPage("./Login");
            }

            var info =
                await _signInManager
                .GetExternalLoginInfoAsync();

            if (info == null)
            {
                return RedirectToPage("./Login");
            }

            var result =
                await _signInManager.ExternalLoginSignInAsync(
                    info.LoginProvider,
                    info.ProviderKey,
                    false,
                    true);

            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }

            var email =
                info.Principal.FindFirstValue(
                    ClaimTypes.Email);

            if (email == null)
            {
                return RedirectToPage("./Login");
            }

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    RegisteredAt = DateTime.UtcNow,
                    DisplayName = email.Split('@')[0]
                };

                var createResult =
                    await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    return RedirectToPage("./Login");
                }
            }

            await _userManager.AddLoginAsync(user, info);

            await _signInManager.SignInAsync(user, false);

            return LocalRedirect(returnUrl);
        }
    }
}
