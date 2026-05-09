using Microsoft.AspNetCore.Identity;
using WebApplication4.Models;

namespace WebApplication4.Middleware
{
    public class BannedUserMiddleware
    {
        private readonly RequestDelegate _next;

        public BannedUserMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = userManager.GetUserId(context.User);
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user != null && user.IsBanned)
                    {
                        var controller = context.Request.RouteValues["controller"]?.ToString();
                        var action = context.Request.RouteValues["action"]?.ToString();
                        var method = context.Request.Method;

                        bool isIdentityPath = context.Request.Path.StartsWithSegments("/Identity");
                        bool isPublicGet = method == "GET" &&
                                          (string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(controller, "Projects", StringComparison.OrdinalIgnoreCase) &&
                                           (string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(action, "Details", StringComparison.OrdinalIgnoreCase)));

                        if (!isIdentityPath && !isPublicGet)
                        {
                            context.Response.Redirect("/?banned=true");
                            return;
                        }

                        if (method != "GET")
                        {
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsync("Ваш аккаунт заблокирован администратором.");
                            return;
                        }
                    }
                }
            }
            await _next(context);
        }
    }

    public static class BannedUserMiddlewareExtensions
    {
        public static IApplicationBuilder UseBannedUserMiddleware(this IApplicationBuilder builder) =>
            builder.UseMiddleware<BannedUserMiddleware>();
    }
}