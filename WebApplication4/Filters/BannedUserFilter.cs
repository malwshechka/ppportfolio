using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WebApplication4.Models;

namespace WebApplication4.Filters
{
    public class BannedUserFilter : IAsyncActionFilter
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public BannedUserFilter(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(context.HttpContext.User);
                if (!string.IsNullOrEmpty(userId))
                {
                    // 🔹 Прямой запрос в БД, минуя кэш UserManager
                    var user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.Id == userId);

                    if (user != null && user.IsBanned)
                    {
                        var controller = context.RouteData.Values["controller"]?.ToString();
                        var action = context.RouteData.Values["action"]?.ToString();
                        var method = context.HttpContext.Request.Method;

                        // 🔹 Разрешаем только безопасные действия
                        bool isAllowed =
                            (string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase) &&
                             string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase) &&
                             method == "GET") ||
                            (string.Equals(controller, "Projects", StringComparison.OrdinalIgnoreCase) &&
                             (string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(action, "Details", StringComparison.OrdinalIgnoreCase)) &&
                             method == "GET") ||
                            (string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase) &&
                             string.Equals(action, "Logout", StringComparison.OrdinalIgnoreCase) &&
                             method == "POST") ||
                            context.HttpContext.Request.Path.StartsWithSegments("/Identity");

                        if (!isAllowed)
                        {
                            // 🔹 Перенаправляем с флагом бана
                            context.HttpContext.Response.Redirect("/?banned=1");
                            return;
                        }
                    }
                }
            }
            await next();
        }
    }
}