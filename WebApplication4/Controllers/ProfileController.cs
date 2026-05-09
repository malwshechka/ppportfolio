using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication4.Data;
using WebApplication4.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication4.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        private async Task LoadProjectViewBags()
        {
            ViewBag.AppTypes = await _context.AppTypes.ToListAsync() ?? new List<AppType>();
            ViewBag.Statuses = await _context.Statuses.ToListAsync() ?? new List<Status>();
            ViewBag.ComplexityLevels = await _context.ComplexityLevels.ToListAsync() ?? new List<ComplexityLevel>();
            ViewBag.Technologies = await _context.Technologies.ToListAsync() ?? new List<Technology>();
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                DisplayName = user.DisplayName,
                Bio = user.Bio,
                Skills = user.Skills,
                GitHubUrl = user.GitHubUrl,
                LinkedInUrl = user.LinkedInUrl,
                TelegramUrl = user.TelegramUrl,
                Email = user.Email,
                RegisteredAt = user.RegisteredAt,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                AllowPrivateMessages = user.AllowPrivateMessages
            };

            model.ProjectsCount = await _context.Projects.CountAsync(p => p.ApplicationUserId == user.Id && !p.ApplicationUser.IsBanned);
            model.ReviewsCount = await _context.Reviews.CountAsync(r => r.Project.ApplicationUserId == user.Id);
            var projectIds = await _context.Projects
                .Where(p => p.ApplicationUserId == user.Id)
                .Select(p => p.Id)
                .ToListAsync();
            model.AverageRating = projectIds.Any()
                ? await _context.Reviews
                    .Where(r => projectIds.Contains(r.ProjectId))
                    .AverageAsync(r => (double?)r.Rating) ?? 0
                : 0;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                user.DisplayName = model.DisplayName;
                user.Bio = model.Bio;
                user.Skills = model.Skills;
                user.GitHubUrl = model.GitHubUrl;
                user.LinkedInUrl = model.LinkedInUrl;
                user.TelegramUrl = model.TelegramUrl;
                user.AllowPrivateMessages = model.AllowPrivateMessages;

                if (model.ProfilePhoto != null && model.ProfilePhoto.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = $"{Guid.NewGuid()}_{model.ProfilePhoto.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await model.ProfilePhoto.CopyToAsync(stream);
                    user.ProfilePhotoUrl = $"/images/profiles/{uniqueFileName}";
                }

                await _userManager.UpdateAsync(user);
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Профиль обновлен!";
                return RedirectToAction(nameof(Index));
            }
            return View(nameof(Index), model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = "Администраторы не могут удалить свой профиль.";
                return RedirectToAction("Index");
            }

            var userId = user.Id;

            // Используем транзакцию, чтобы если упадет удаление в Identity, данные в БД не побились
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. УДАЛЕНИЕ ФАЙЛОВ
                var projectImageUrls = await _context.Images
                    .Where(i => i.Project.ApplicationUserId == userId)
                    .Select(i => i.Url)
                    .ToListAsync();

                foreach (var url in projectImageUrls)
                {
                    if (string.IsNullOrEmpty(url)) continue;
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", url.TrimStart('/'));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }

                if (!string.IsNullOrEmpty(user.ProfilePhotoUrl))
                {
                    var avatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePhotoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(avatarPath)) System.IO.File.Delete(avatarPath);
                }

                // 2. РУЧНОЕ УДАЛЕНИЕ ВСЕХ СВЯЗЕЙ (из-за Restrict)

                // Избранное и отзывы
                _context.Favorites.RemoveRange(_context.Favorites.Where(f => f.UserId == userId || f.Project.ApplicationUserId == userId));
                _context.Reviews.RemoveRange(_context.Reviews.Where(r => r.UserId == userId || r.Project.ApplicationUserId == userId));

                // Проекты и их составляющие
                _context.ProjectTechnologies.RemoveRange(_context.ProjectTechnologies.Where(pt => pt.Project.ApplicationUserId == userId));
                _context.SelectedImages.RemoveRange(_context.SelectedImages.Where(si => si.Project.ApplicationUserId == userId));
                _context.Images.RemoveRange(_context.Images.Where(i => i.Project.ApplicationUserId == userId));
                _context.Projects.RemoveRange(_context.Projects.Where(p => p.ApplicationUserId == userId));

                // Чаты, сообщения и участники
                _context.Messages.RemoveRange(_context.Messages.Where(m => m.SenderId == userId));
                _context.ConversationParticipants.RemoveRange(_context.ConversationParticipants.Where(cp => cp.UserId == userId));
                _context.Conversations.RemoveRange(_context.Conversations.Where(c => c.User1Id == userId || c.User2Id == userId));

                // Групповые чаты
                _context.GroupMessages.RemoveRange(_context.GroupMessages.Where(gm => gm.SenderId == userId));
                _context.GroupMembers.RemoveRange(_context.GroupMembers.Where(gm => gm.UserId == userId));
                // Если пользователь создатель группы, можно либо удалить группу, либо передать владение. Тут удаляем:
                _context.GroupChats.RemoveRange(_context.GroupChats.Where(gc => gc.CreatedById == userId));

                // Подписки, блокировки и жалобы
                _context.UserSubscriptions.RemoveRange(_context.UserSubscriptions.Where(s => s.SubscriberId == userId || s.FollowedUserId == userId));
                _context.UserBlocks.RemoveRange(_context.UserBlocks.Where(b => b.BlockerId == userId || b.BlockedUserId == userId));
                _context.UserReports.RemoveRange(_context.UserReports.Where(r => r.ReporterId == userId || r.ReportedUserId == userId));

                // Сохраняем изменения в БД перед удалением самого юзера
                await _context.SaveChangesAsync();

                // 3. УДАЛЕНИЕ ДАННЫХ IDENTITY (Роли, Логины, Токены)
                // Это критично, если юзер не удаляется
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Any()) await _userManager.RemoveFromRolesAsync(user, roles);

                var logins = await _userManager.GetLoginsAsync(user);
                foreach (var login in logins) await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);

                // 4. ФИНАЛЬНОЕ УДАЛЕНИЕ ЮЗЕРА
                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    // Собираем ошибки, чтобы понять ПОЧЕМУ не удалилось
                    var errorMsg = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["Error"] = $"Ошибка Identity: {errorMsg}";
                    return RedirectToAction("Index");
                }

                await transaction.CommitAsync();

                // 5. ВЫХОД И РЕДИРЕКТ
                await _signInManager.SignOutAsync();
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Выводим внутреннюю ошибку БД в TempData для отладки
                TempData["Error"] = $"Критическая ошибка: {ex.InnerException?.Message ?? ex.Message}";
                return RedirectToAction("Index");
            }
        }
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            if (!ModelState.IsValid) return View(model);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword!, model.NewPassword!);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Пароль успешно изменен!";
                return RedirectToAction("Index");
            }
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateProject()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            await LoadProjectViewBags();
            return View(new ProjectViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(ProjectViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            if (!ModelState.IsValid) { await LoadProjectViewBags(); return View(model); }

            var userId = _userManager.GetUserId(User);
            var project = new Project
            {
                Title = model.Title,
                Description = model.Description,
                AppTypeId = model.AppTypeId,
                StatusId = model.StatusId,
                ComplexityLevelId = model.ComplexityLevelId,
                RepositoryUrl = model.RepositoryUrl,
                DemoUrl = model.DemoUrl,
                ApplicationUserId = userId,
                CreatedAt = DateTime.Now,
                ModerationStatus = ModerationStatus.Pending
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            if (model.SelectedTechnologyIds != null)
                foreach (var techId in model.SelectedTechnologyIds)
                    _context.ProjectTechnologies.Add(new ProjectTechnology { ProjectId = project.Id, TechnologyId = techId });

            if (model.Images != null && model.Images.Any())
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "projects");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                foreach (var file in model.Images)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create)) await file.CopyToAsync(stream);
                    _context.Images.Add(new Image { Name = file.FileName, Url = $"/images/projects/{uniqueFileName}", ProjectId = project.Id });
                }
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = "Проект успешно создан!";
            return RedirectToAction(nameof(MyProjects));
        }

        [HttpGet]
        public async Task<IActionResult> EditProject(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            var userId = _userManager.GetUserId(User);
            var project = await _context.Projects
                .Include(p => p.ProjectTechnologies).Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && p.ApplicationUserId == userId);
            if (project == null) return NotFound();

            await LoadProjectViewBags();
            var model = new ProjectViewModel
            {
                Id = project.Id,
                Title = project.Title,
                Description = project.Description,
                AppTypeId = project.AppTypeId,
                StatusId = project.StatusId,
                ComplexityLevelId = project.ComplexityLevelId,
                RepositoryUrl = project.RepositoryUrl,
                DemoUrl = project.DemoUrl,
                SelectedTechnologyIds = project.ProjectTechnologies?.Select(pt => pt.TechnologyId).ToList(),
                ExistingImages = project.Images?.ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProject(ProjectViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            var userId = _userManager.GetUserId(User);
            var project = await _context.Projects
                .Include(p => p.ProjectTechnologies)
                .FirstOrDefaultAsync(p => p.Id == model.Id && p.ApplicationUserId == userId);
            if (project == null) return NotFound();
            if (!ModelState.IsValid) { await LoadProjectViewBags(); return View(model); }

            project.Title = model.Title;
            project.Description = model.Description;
            project.AppTypeId = model.AppTypeId;
            project.StatusId = model.StatusId;
            project.ComplexityLevelId = model.ComplexityLevelId;
            project.RepositoryUrl = model.RepositoryUrl;
            project.DemoUrl = model.DemoUrl;
            project.UpdatedAt = DateTime.Now;

            if (project.ModerationStatus is ModerationStatus.Rejected or ModerationStatus.Approved)
            {
                project.ModerationStatus = ModerationStatus.Pending;
                project.RejectionReason = null;
                project.ModeratedAt = null;
                project.ModeratedByUserId = null;
            }

            _context.ProjectTechnologies.RemoveRange(project.ProjectTechnologies!);
            if (model.SelectedTechnologyIds != null)
                foreach (var techId in model.SelectedTechnologyIds)
                    _context.ProjectTechnologies.Add(new ProjectTechnology { ProjectId = project.Id, TechnologyId = techId });

            if (model.Images != null && model.Images.Any())
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "projects");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                foreach (var file in model.Images)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create)) await file.CopyToAsync(stream);
                    _context.Images.Add(new Image { Name = file.FileName, Url = $"/images/projects/{uniqueFileName}", ProjectId = project.Id });
                }
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = "Проект обновлен!";
            return RedirectToAction(nameof(MyProjects));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            var image = await _context.Images.Include(i => i.Project).FirstOrDefaultAsync(i => i.Id == imageId);
            if (image == null) return NotFound();
            if (image.Project.ApplicationUserId != _userManager.GetUserId(User)) return Forbid();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.Url.TrimStart('/'));
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            _context.Images.Remove(image);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        public async Task<IActionResult> MyProjects()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            var userId = _userManager.GetUserId(User);
            var projects = await _context.Projects
                .Include(p => p.AppType).Include(p => p.Status).Include(p => p.ComplexityLevel)
                .Include(p => p.ProjectTechnologies!).ThenInclude(pt => pt!.Technology)
                .Include(p => p.Images)
                .Where(p => p.ApplicationUserId == userId)
                .OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(projects);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            var userId = _userManager.GetUserId(User);
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.ApplicationUserId == userId);
            if (project != null) { _context.Projects.Remove(project); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(MyProjects));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSubscription(string userId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == userId) return NotFound();

            var targetUser = await _userManager.FindByIdAsync(userId);
            if (targetUser == null || await _userManager.IsInRoleAsync(targetUser, "Admin")) return Forbid();

            var existing = await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.SubscriberId == currentUserId && s.FollowedUserId == userId);

            if (existing != null)
            {
                _context.UserSubscriptions.Remove(existing);
                TempData["Success"] = "Вы отписались от пользователя";
            }
            else
            {
                _context.UserSubscriptions.Add(new UserSubscription
                {
                    SubscriberId = currentUserId,
                    FollowedUserId = userId,
                    SubscribedAt = DateTime.Now
                });
                TempData["Success"] = "Вы подписались на пользователя";
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Subscriptions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var blockedUserIds = await _context.UserBlocks
                .Where(b => b.BlockerId == userId || b.BlockedUserId == userId)
                .Select(b => b.BlockerId == userId ? b.BlockedUserId : b.BlockerId)
                .ToListAsync();

            var subscriptions = await _context.UserSubscriptions
                .Where(s => s.SubscriberId == userId && !blockedUserIds.Contains(s.FollowedUserId))
                .Include(s => s.FollowedUser)
                .OrderByDescending(s => s.SubscribedAt)
                .Select(s => new SubscriptionViewModel
                {
                    UserId = s.FollowedUser.Id,
                    DisplayName = s.FollowedUser.DisplayName ?? s.FollowedUser.UserName,
                    ProfilePhotoUrl = s.FollowedUser.ProfilePhotoUrl,
                    SubscribedAt = s.SubscribedAt
                }).ToListAsync();

            return View(subscriptions);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePrivacySettings(bool allowMessages)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            if (user == null) return NotFound();
            user.AllowPrivateMessages = allowMessages;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) return Ok();
            return BadRequest();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(string userId, string type, string comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            var reporterId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(reporterId)) return Challenge();

            var reportedUser = await _userManager.FindByIdAsync(userId);
            if (reportedUser == null) return NotFound();

            var reportType = type switch
            {
                "Spam" => ReportType.Spam,
                "Harassment" => ReportType.Harassment,
                "InappropriateContent" => ReportType.InappropriateContent,
                "FakeProfile" => ReportType.FakeProfile,
                _ => ReportType.Other
            };

            _context.UserReports.Add(new UserReport
            {
                ReporterId = reporterId,
                ReportedUserId = userId,
                Type = reportType,
                Reason = type,
                AdditionalComment = comment,
                ReportedAt = DateTime.Now,
                IsResolved = false
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Жалоба отправлена. Администрация рассмотрит её в ближайшее время.";
            return RedirectToAction(nameof(Index), new { userId });
        }

        public async Task<IActionResult> BlockedUsers()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var blocked = await _context.UserBlocks
                .Where(b => b.BlockerId == userId)
                .Include(b => b.BlockedUserRef)
                .OrderByDescending(b => b.BlockedAt)
                .ToListAsync();
            return View(blocked);
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
    }

    public class SubscriptionViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? ProfilePhotoUrl { get; set; }
        public DateTime SubscribedAt { get; set; }
    }
}