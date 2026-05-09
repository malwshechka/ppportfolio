using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication4.Data;
using WebApplication4.Models;

namespace WebApplication4.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoritesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            if (await _userManager.IsInRoleAsync(user, "Admin")) return RedirectToAction("Index", "Home");

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var blockedUserIds = await _context.UserBlocks
                .Where(b => b.BlockerId == userId || b.BlockedUserId == userId)
                .Select(b => b.BlockerId == userId ? b.BlockedUserId : b.BlockerId)
                .ToListAsync();

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Project).ThenInclude(p => p.ApplicationUser)
                .Include(f => f.Project).ThenInclude(p => p.Images)
                .Include(f => f.Project).ThenInclude(p => p.Reviews)
                .Where(f => !blockedUserIds.Contains(f.Project.ApplicationUserId!) && !f.Project.ApplicationUser.IsBanned)
                .OrderByDescending(f => f.AddedAt)
                .Select(f => f.Project)
                .ToListAsync();

            return View(favorites);
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int projectId, string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Администраторы не могут использовать избранное";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            var userId = _userManager.GetUserId(User);
            var exists = await _context.Favorites.AnyAsync(f => f.UserId == userId && f.ProjectId == projectId);

            if (exists)
            {
                var fav = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ProjectId == projectId);
                if (fav != null) _context.Favorites.Remove(fav);
            }
            else
            {
                _context.Favorites.Add(new Favorite { UserId = userId, ProjectId = projectId });
            }
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index");
        }
    }
}