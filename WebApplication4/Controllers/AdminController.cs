using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication4.Data;
using WebApplication4.Models;

namespace WebApplication4.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.IsInRole("Admin"))
                return RedirectToAction("Index", "Home");

            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalProjects = await _context.Projects.CountAsync(),
                PendingProjects = await _context.Projects.CountAsync(p => p.ModerationStatus == ModerationStatus.Pending),
                TotalReviews = await _context.Reviews.CountAsync(),
                RecentProjects = await _context.Projects
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.Images)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                PendingProjectsList = await _context.Projects
                    .Include(p => p.ApplicationUser)
                    .Include(p => p.AppType)
                    .Include(p => p.Status)
                    .Include(p => p.ProjectTechnologies!)
                    .ThenInclude(pt => pt!.Technology)
                    .Include(p => p.Images)
                    .Where(p => p.ModerationStatus == ModerationStatus.Pending)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };
            return View(model);
        }

        public async Task<IActionResult> Users(string search, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.Email!.Contains(search) ||
                    u.UserName!.Contains(search) ||
                    (u.DisplayName != null && u.DisplayName.Contains(search)));
            }

            var totalUsers = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.RegisteredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var projectsCount = await _context.Projects
                .Where(p => p.ApplicationUserId != null)
                .GroupBy(p => p.ApplicationUserId!)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            ViewBag.ProjectsCount = projectsCount;
            ViewBag.CurrentSearch = search;
            ViewBag.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
            ViewBag.CurrentPage = page;

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBan(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            if (await _userManager.IsInRoleAsync(user, "Admin")) return Forbid();

            user.IsBanned = !user.IsBanned;
            user.BannedAt = user.IsBanned ? DateTime.UtcNow : null;
            user.BanReason = user.IsBanned ? "Блокировка администратором" : null;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = user.IsBanned
                ? "Пользователь заблокирован. Все функции отключены, проекты скрыты."
                : "Пользователь разблокирован. Доступ и проекты восстановлены.";

            return RedirectToAction("Users");
        }

        public async Task<IActionResult> PendingProjects()
        {
            var projects = await _context.Projects
                .Include(p => p.ApplicationUser)
                .Include(p => p.AppType)
                .Include(p => p.Status)
                .Include(p => p.ComplexityLevel)
                .Include(p => p.ProjectTechnologies!)
                .ThenInclude(pt => pt!.Technology)
                .Include(p => p.Images)
                .Where(p => p.ModerationStatus == ModerationStatus.Pending)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(projects);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                project.ModerationStatus = ModerationStatus.Approved;
                project.ModeratedAt = DateTime.UtcNow;
                project.ModeratedByUserId = _userManager.GetUserId(User);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Проект \"{project.Title}\" опубликован!";
            }
            return RedirectToAction(nameof(PendingProjects));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProject(int id, string reason)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
            {
                project.ModerationStatus = ModerationStatus.Rejected;
                project.RejectionReason = reason;
                project.ModeratedAt = DateTime.UtcNow;
                project.ModeratedByUserId = _userManager.GetUserId(User);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Проект \"{project.Title}\" отклонён";
            }
            return RedirectToAction(nameof(PendingProjects));
        }

        public async Task<IActionResult> Reviews()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Project)
                .OrderByDescending(r => r.Date)
                .ToListAsync();
            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Отзыв удалён";
            }
            return RedirectToAction(nameof(Reviews));
        }

        public async Task<IActionResult> Complaints()
        {
            var reports = await _context.UserReports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .OrderByDescending(r => r.ReportedAt)
                .ToListAsync();
            return View(reports);
        }

        [HttpPost]
        public async Task<IActionResult> ResolveReport(int reportId, string? response)
        {
            var report = await _context.UserReports.FindAsync(reportId);
            if (report == null) return NotFound();

            report.IsResolved = true;
            report.AdminResponse = response;
            report.ResolvedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Жалоба рассмотрена";
            return RedirectToAction(nameof(Complaints));
        }

        [HttpPost]
        public async Task<IActionResult> SendWarning(string userId, string message)
        {
            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser == null || await _userManager.IsInRoleAsync(targetUser, "Admin")) return NotFound();

            var adminId = _userManager.GetUserId(User);
            var conv = await _context.Conversations.FirstOrDefaultAsync(c =>
                (c.User1Id == adminId && c.User2Id == userId) ||
                (c.User1Id == userId && c.User2Id == adminId));

            if (conv == null)
            {
                conv = new Conversation { User1Id = adminId, User2Id = userId, LastMessageAt = DateTime.UtcNow };
                _context.Conversations.Add(conv);
                await _context.SaveChangesAsync();
            }

            _context.Messages.Add(new Message
            {
                ConversationId = conv.Id,
                SenderId = adminId,
                Text = $"[ПРЕДУПРЕЖДЕНИЕ ОТ АДМИНИСТРАЦИИ] {message}",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Предупреждение отправлено";
            return RedirectToAction(nameof(Complaints));
        }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProjects { get; set; }
        public int PendingProjects { get; set; }
        public int TotalReviews { get; set; }
        public List<Project> RecentProjects { get; set; } = new();
        public List<Project> PendingProjectsList { get; set; } = new();
    }
}