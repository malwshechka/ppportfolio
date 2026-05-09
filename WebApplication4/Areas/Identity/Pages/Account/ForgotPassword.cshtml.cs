using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using WebApplication4.Models;

namespace WebApplication4.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public class InputModel
        {
            [Required(ErrorMessage = "Введите Email")]
            [EmailAddress(ErrorMessage = "Неверный формат Email")]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);

                // Отправляем письмо только если пользователь найден И почта подтверждена
                if (user != null && await _userManager.IsEmailConfirmedAsync(user))
                {
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code, email = Input.Email },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(
                        Input.Email,
                        "Сброс пароля — DevPortfolio",
                        $"<p>Здравствуйте!</p><p>Для сброса пароля перейдите по ссылке:</p><p><a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Сбросить пароль</a></p><p>Если вы не запрашивали сброс, проигнорируйте это письмо.</p>");
                }

                // Всегда показываем одинаковое сообщение (безопасность)
                return RedirectToPage("./ForgotPasswordConfirmation");
            }
            return Page();
        }
    }
}