using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebApplication4.Data;
using WebApplication4.Models;

namespace WebApplication4.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<string?> GetBannedNoticeAsync()
        {
            if (User.Identity?.IsAuthenticated != true) return null;

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId)) return null;

            var user = await _userManager.FindByIdAsync(userId);

            return user?.IsBanned == true
                ? "⛔ Ваш аккаунт временно заблокирован администратором. Доступны только публичные просмотры, разделы «О нас» и «Конфиденциальность»."
                : null;
        }

        public async Task<IActionResult> Index(
            string search,
            int? appTypeId,
            int? complexityId,
            int? statusId,
            string sortBy)
        {
            ViewBag.BannedNotice = await GetBannedNoticeAsync();

            var currentUserId = _userManager.GetUserId(User);

            var blockedUserIds = new List<string>();

            if (currentUserId != null)
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

            var projects = _context.Projects
                .Include(p => p.ApplicationUser)
                .Where(p =>
                    p.ModerationStatus == ModerationStatus.Approved &&
                    p.ApplicationUser != null &&
                    !p.ApplicationUser.IsBanned)
                .Include(p => p.AppType)
                .Include(p => p.Status)
                .Include(p => p.ComplexityLevel)
                .Include(p => p.ProjectTechnologies!)
                .ThenInclude(pt => pt!.Technology)
                .Include(p => p.Reviews)
                .Include(p => p.Images)
                .Include(p => p.Favorites)
                .AsQueryable();

            if (blockedUserIds.Any())
            {
                projects = projects.Where(p =>
                    !blockedUserIds.Contains(p.ApplicationUserId!));
            }

            if (!string.IsNullOrEmpty(search))
            {
                projects = projects.Where(p =>
                    p.Title.Contains(search) ||
                    p.Description.Contains(search));
            }

            if (appTypeId.HasValue)
            {
                projects = projects.Where(p =>
                    p.AppTypeId == appTypeId.Value);
            }

            if (complexityId.HasValue)
            {
                projects = projects.Where(p =>
                    p.ComplexityLevelId == complexityId.Value);
            }

            if (statusId.HasValue)
            {
                projects = projects.Where(p =>
                    p.StatusId == statusId.Value);
            }

            projects = sortBy switch
            {
                "rating" => projects.OrderByDescending(p =>
                    p.Reviews.Any()
                        ? p.Reviews.Average(r => r.Rating)
                        : 0),

                "popularity" => projects.OrderByDescending(p =>
                    p.Reviews.Count),

                "date" => projects.OrderByDescending(p =>
                    p.CreatedAt),

                _ => projects.OrderByDescending(p =>
                    p.CreatedAt)
            };

            var model = await projects
                .Take(12)
                .ToListAsync();

            ViewBag.AppTypes =
                await _context.AppTypes.ToListAsync();

            ViewBag.ComplexityLevels =
                await _context.ComplexityLevels.ToListAsync();

            ViewBag.Statuses =
                await _context.Statuses.ToListAsync();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentSort = sortBy;

            return View(model);
        }

        public async Task<IActionResult> About()
        {
            ViewBag.BannedNotice =
                await GetBannedNoticeAsync();

            ViewData["Title"] = "О нас";

            return View();
        }

        [HttpPost]
        public IActionResult SetLanguage(
            string culture,
            string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(
                    new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires =
                        DateTimeOffset.UtcNow.AddYears(1)
                });

            return LocalRedirect(returnUrl);
        }

        public async Task<IActionResult> Privacy()
        {
            ViewBag.BannedNotice =
                await GetBannedNoticeAsync();

            return View();
        }

        [Authorize]
        public async Task<IActionResult> Feed()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user?.IsBanned == true)
            {
                return RedirectToAction(
                    "Index",
                    new { banned = 1 });
            }

            var userId =
                _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var blockedUserIds =
                await _context.UserBlocks
                    .Where(b =>
                        b.BlockerId == userId ||
                        b.BlockedUserId == userId)
                    .Select(b =>
                        b.BlockerId == userId
                            ? b.BlockedUserId
                            : b.BlockerId)
                    .ToListAsync();

            var followedUserIds =
                await _context.UserSubscriptions
                    .Where(s =>
                        s.SubscriberId == userId &&
                        !blockedUserIds.Contains(
                            s.FollowedUserId))
                    .Select(s => s.FollowedUserId)
                    .ToListAsync();

            if (!followedUserIds.Any())
            {
                TempData["Info"] =
                    "Вы пока ни на кого не подписаны. Найдите интересных разработчиков!";

                return RedirectToAction("Index");
            }

            var projects = await _context.Projects
                .Include(p => p.ApplicationUser)
                .Where(p =>
                    followedUserIds.Contains(
                        p.ApplicationUserId!) &&
                    !blockedUserIds.Contains(
                        p.ApplicationUserId!) &&
                    p.ModerationStatus ==
                        ModerationStatus.Approved &&
                    p.ApplicationUser != null &&
                    !p.ApplicationUser.IsBanned)
                .Include(p => p.AppType)
                .Include(p => p.Status)
                .Include(p => p.ProjectTechnologies!)
                .ThenInclude(pt => pt.Technology)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

            return View(projects);
        }
    }

}
