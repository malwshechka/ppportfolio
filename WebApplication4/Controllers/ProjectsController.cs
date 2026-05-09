using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication4.Data;
using WebApplication4.Models;
using Microsoft.AspNetCore.Identity;

namespace WebApplication4.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string search, int? appTypeId, int? complexityId, int? statusId, string sortBy)
        {
            var currentUserId = _userManager.GetUserId(User);
            var blockedUserIds = new List<string>();
            if (currentUserId != null)
            {
                blockedUserIds = await _context.UserBlocks
                    .Where(b => b.BlockerId == currentUserId || b.BlockedUserId == currentUserId)
                    .Select(b => b.BlockerId == currentUserId ? b.BlockedUserId : b.BlockerId)
                    .ToListAsync();
            }

            var projects = _context.Projects
                .Include(p => p.ApplicationUser)
                .Include(p => p.AppType)
                .Include(p => p.Status)
                .Include(p => p.ComplexityLevel)
                .Include(p => p.ProjectTechnologies!)
                .ThenInclude(pt => pt.Technology)
                .Include(p => p.Images)
                .Where(p => !blockedUserIds.Contains(p.ApplicationUserId!) && !p.ApplicationUser.IsBanned)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                projects = projects.Where(p => p.Title.Contains(search) || p.Description.Contains(search));
            }
            if (appTypeId.HasValue)
                projects = projects.Where(p => p.AppTypeId == appTypeId.Value);
            if (complexityId.HasValue)
                projects = projects.Where(p => p.ComplexityLevelId == complexityId.Value);
            if (statusId.HasValue)
                projects = projects.Where(p => p.StatusId == statusId.Value);

            projects = sortBy switch
            {
                "rating" => projects.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0),
                "popularity" => projects.OrderByDescending(p => p.Reviews.Count),
                "date" => projects.OrderByDescending(p => p.CreatedAt),
                _ => projects.OrderByDescending(p => p.CreatedAt)
            };

            var model = await projects.Take(12).ToListAsync();
            ViewBag.AppTypes = await _context.AppTypes.ToListAsync();
            ViewBag.ComplexityLevels = await _context.ComplexityLevels.ToListAsync();
            ViewBag.Statuses = await _context.Statuses.ToListAsync();
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentSort = sortBy;
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects
                .Include(p => p.ApplicationUser)
                .Include(p => p.AppType)
                .Include(p => p.Status)
                .Include(p => p.ComplexityLevel)
                .Include(p => p.ProjectTechnologies!)
                .ThenInclude(pt => pt.Technology)
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .Include(p => p.Images)
                .Include(p => p.Favorites)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            ViewBag.CanReview = User.Identity?.IsAuthenticated == true
                && project.ApplicationUserId != _userManager.GetUserId(User)
                && !User.IsInRole("Admin");
            return View(project);
        }

        public async Task<IActionResult> UserPortfolio(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var isBlocked = false;
            if (currentUserId != null)
            {
                isBlocked = await _context.UserBlocks.AnyAsync(b =>
                    (b.BlockerId == currentUserId && b.BlockedUserId == userId) ||
                    (b.BlockerId == userId && b.BlockedUserId == currentUserId));
            }

            List<Project> projects;
            if (isBlocked || user.IsBanned)
            {
                projects = new List<Project>();
            }
            else
            {
                projects = await _context.Projects
                    .Include(p => p.AppType)
                    .Include(p => p.Status)
                    .Include(p => p.ComplexityLevel)
                    .Include(p => p.ProjectTechnologies!)
                    .ThenInclude(pt => pt.Technology)
                    .Include(p => p.Images)
                    .Where(p => p.ApplicationUserId == userId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }

            var model = new UserPortfolioViewModel
            {
                User = user,
                Projects = projects,
                TotalProjects = projects.Count,
                TotalReviews = await _context.Reviews.CountAsync(r => r.Project.ApplicationUserId == userId),
                AverageRating = projects.Any()
                    ? await _context.Reviews.Where(r => r.Project.ApplicationUserId == userId).AverageAsync(r => (double?)r.Rating) ?? 0
                    : 0
            };

            ViewBag.CurrentUserId = currentUserId;
            ViewBag.IsUserBlocked = isBlocked;
            ViewBag.IsAdminBanned = user.IsBanned;
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddReview(int projectId, ReviewViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Details", new { id = projectId });
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Администраторы не могут оставлять отзывы";
                return RedirectToAction("Details", new { id = projectId });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var project = await _context.Projects
                .Include(p => p.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return NotFound();
            if (project.ApplicationUserId == userId)
            {
                TempData["Error"] = "Вы не можете оставить отзыв на свой собственный проект";
                return RedirectToAction("Details", new { id = projectId });
            }
            if (string.IsNullOrWhiteSpace(model.Text) || model.Rating < 1 || model.Rating > 5)
            {
                TempData["Error"] = "Заполните текст отзыва и выберите оценку от 1 до 5";
                return RedirectToAction("Details", new { id = projectId });
            }

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProjectId == projectId && r.UserId == userId);

            if (existingReview != null)
            {
                existingReview.Text = model.Text.Trim();
                existingReview.Rating = model.Rating;
                existingReview.Date = DateTime.Now;
                _context.Reviews.Update(existingReview);
            }
            else
            {
                var review = new Review
                {
                    Text = model.Text.Trim(),
                    Rating = model.Rating,
                    Date = DateTime.Now,
                    UserId = userId,
                    ProjectId = projectId
                };
                _context.Reviews.Add(review);
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Отзыв успешно сохранён!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ошибка при сохранении: " + ex.Message;
            }
            return RedirectToAction("Details", new { id = projectId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int reviewId, int projectId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Details", new { id = projectId });

            var userId = _userManager.GetUserId(User);
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review == null)
                return NotFound();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = projectId });
        }
    }
}