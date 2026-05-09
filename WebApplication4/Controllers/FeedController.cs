using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication4.Data;
using WebApplication4.Models;

namespace WebApplication4.Controllers
{
    [Authorize]
    public class FeedController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FeedController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var blockedUserIds = await _context.UserBlocks
                .Where(b => b.BlockerId == userId || b.BlockedUserId == userId)
                .Select(b => b.BlockerId == userId ? b.BlockedUserId : b.BlockerId)
                .ToListAsync();

            var followedIds = await _context.UserSubscriptions
                .Where(s => s.SubscriberId == userId && !blockedUserIds.Contains(s.FollowedUserId))
                .Select(s => s.FollowedUserId)
                .ToListAsync();

            if (!followedIds.Any())
            {
                ViewData["EmptyMessage"] = "Вы пока ни на кого не подписаны. Найдите интересных разработчиков!";
                return View(new List<Project>());
            }

            var projects = await _context.Projects
                .Where(p => followedIds.Contains(p.ApplicationUserId!) &&
                            !blockedUserIds.Contains(p.ApplicationUserId!) &&
                            p.ModerationStatus == ModerationStatus.Approved &&
                            !p.ApplicationUser.IsBanned)
                .Include(p => p.ApplicationUser)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

            return View(projects);
        }
    }
}