using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication4.Data;
using WebApplication4.Models;

namespace WebApplication4.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 🔹 Проверка бана
        private async Task<string?> GetBannedNoticeAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
                return null;

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return null;

            var user = await _userManager.FindByIdAsync(userId);

            return user?.IsBanned == true
                ? "⛔ Ваш аккаунт временно заблокирован администратором."
                : null;
        }

        // 🔹 Главная
        public async Task<IActionResult> Index(
            string? search,
            int? appTypeId,
            int? complexityId,
            int? statusId,
            string? sortBy)
        {
            ViewBag.BannedNotice = await GetBannedNoticeAsync();

            var currentUserId = _userManager.GetUserId(User);

            var blockedUserIds = new List<string>();

            if (!string.IsNullOrEmpty(currentUserId))
            {
                blockedUserIds = await _context.UserBlocks
                    .Where(b =>
                        b.BlockerId == currentUserId ||
                        b.BlockedUserId == currentUserId)
                    .Select(b =>
                        b.BlockerId == currentUserId
                            ? b.BlockedUserId
                            : b.BlockerId)
                    .ToListAsync();
            }

            // 🔥 БЕЗ Include — чтобы не было timeout на Render/Supabase
            IQueryable<Project> projectsQuery = _context.Projects
                .AsNoTracking()
                .Where(p =>
                    p.ModerationStatus == ModerationStatus.Approved &&
                    p.ApplicationUser != null &&
                    !p.ApplicationUser.IsBanned);

            // 🔹 Блокировки
            if (blockedUserIds.Any())
            {
                projectsQuery = projectsQuery
                    .Where(p => !blockedUserIds.Contains(p.ApplicationUserId!));
            }

            // 🔹 Поиск
            if (!string.IsNullOrWhiteSpace(search))
            {
                projectsQuery = projectsQuery.Where(p =>
                    p.Title.Contains(search) ||
                    p.Description.Contains(search));
            }

            // 🔹 Фильтры
            if (appTypeId.HasValue)
            {
                projectsQuery = projectsQuery
                    .Where(p => p.AppTypeId == appTypeId.Value);
            }

            if (complexityId.HasValue)
            {
                projectsQuery = projectsQuery
                    .Where(p => p.ComplexityLevelId == complexityId.Value);
            }

            if (statusId.HasValue)
            {
                projectsQuery = projectsQuery
                    .Where(p => p.StatusId == statusId.Value);
            }

            // 🔹 Сортировка
            projectsQuery = sortBy switch
            {
                "date" => projectsQuery.OrderByDescending(p => p.CreatedAt),

                "rating" => projectsQuery
                    .OrderByDescending(p =>
                        p.Reviews.Any()
                            ? p.Reviews.Average(r => r.Rating)
                            : 0),

                "popularity" => projectsQuery
                    .OrderByDescending(p => p.Reviews.Count),

                _ => projectsQuery.OrderByDescending(p => p.CreatedAt)
            };

            // 🔥 Только 12 записей
            var model = await projectsQuery
                .Take(12)
                .ToListAsync();

            // 🔥 Загружаем отдельно
            ViewBag.AppTypes = await _context.AppTypes
                .AsNoTracking()
                .ToListAsync();

            ViewBag.ComplexityLevels = await _context.ComplexityLevels
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Statuses = await _context.Statuses
                .AsNoTracking()
                .ToListAsync();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentSort = sortBy;

            return View(model);
        }

        // 🔹 О нас
        public async Task<IActionResult> About()
        {
            ViewBag.BannedNotice = await GetBannedNoticeAsync();
            ViewData["Title"] = "О нас";

            return View();
        }

        // 🔹 Privacy
        public async Task<IActionResult> Privacy()
        {
            ViewBag.BannedNotice = await GetBannedNoticeAsync();

            return View();
        }

        // 🔹 Смена языка
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(
                    new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1)
                });

            return LocalRedirect(returnUrl);
        }

        // 🔹 Лента
        [Authorize]
        public async Task<IActionResult> Feed()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user?.IsBanned == true)
            {
                return RedirectToAction("Index");
            }

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var blockedUserIds = await _context.UserBlocks
                .Where(b =>
                    b.BlockerId == userId ||
                    b.BlockedUserId == userId)
                .Select(b =>
                    b.BlockerId == userId
                        ? b.BlockedUserId
                        : b.BlockerId)
                .ToListAsync();

            var followedUserIds = await _context.UserSubscriptions
                .Where(s =>
                    s.SubscriberId == userId &&
                    !blockedUserIds.Contains(s.FollowedUserId))
                .Select(s => s.FollowedUserId)
                .ToListAsync();

            if (!followedUserIds.Any())
            {
                TempData["Info"] =
                    "Вы пока ни на кого не подписаны.";

                return RedirectToAction("Index");
            }

            // 🔥 Без Include
            var projects = await _context.Projects
                .AsNoTracking()
                .Where(p =>
                    followedUserIds.Contains(p.ApplicationUserId!) &&
                    !blockedUserIds.Contains(p.ApplicationUserId!) &&
                    p.ModerationStatus == ModerationStatus.Approved &&
                    p.ApplicationUser != null &&
                    !p.ApplicationUser.IsBanned)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

            return View(projects);
        }
    }
}