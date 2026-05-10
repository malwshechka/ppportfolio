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
    public class MessagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public MessagesController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        private async Task<bool> IsBlockedAsync(string userA, string userB)
        {
            return await _context.UserBlocks.AnyAsync(b =>
                (b.BlockerId == userA && b.BlockedUserId == userB) ||
                (b.BlockerId == userB && b.BlockedUserId == userA));
        }

        public async Task<IActionResult> Index(string search)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var blockedIds = await _context.UserBlocks
                .Where(b => b.BlockerId == userId || b.BlockedUserId == userId)
                .Select(b => b.BlockerId == userId ? b.BlockedUserId : b.BlockerId)
                .ToListAsync();

            var convs = await _context.Conversations
                .Include(c => c.User1).Include(c => c.User2)
                .Include(c => c.Messages)
                .ToListAsync();

            var privateThreads = convs
                .Where(c => (c.User1Id == userId || c.User2Id == userId))
                .Where(c => {
                    var otherId = c.User1Id == userId ? c.User2Id : c.User1Id;
                    return !blockedIds.Contains(otherId) &&
                           !(c.User1Id == userId && c.IsDeletedByUser1) &&
                           !(c.User2Id == userId && c.IsDeletedByUser2);
                })
                .Select(c => new MessageThreadViewModel
                {
                    Id = c.Id,
                    Type = "private",
                    OtherUser = c.User1Id == userId ? c.User2 : c.User1,
                    LastMessageText = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Text).FirstOrDefault(),
                    LastMessageAt = c.LastMessageAt,
                    UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderId != userId),
                    IsPinned = c.User1Id == userId ? c.IsPinnedByUser1 : c.IsPinnedByUser2
                });

            var groupChats = await _context.GroupChats
                .Include(g => g.Members)
                .Include(g => g.Messages)
                .Where(g => g.Members.Any(m => m.UserId == userId && !m.Left))
                .Where(g => !blockedIds.Contains(g.CreatedById))
                .ToListAsync();

            var groupThreads = groupChats.Select(g => new MessageThreadViewModel
            {
                Id = g.Id,
                Type = "group",
                GroupName = g.Name,
                LastMessageText = g.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Text).FirstOrDefault(),
                LastMessageAt = g.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.CreatedAt).FirstOrDefault(),
                UnreadCount = g.Messages.Count(m => !m.IsRead && m.SenderId != userId),
                IsPinned = false
            });

            var allThreads = privateThreads.Concat(groupThreads)
                .OrderByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.LastMessageAt)
                .ToList();

            if (!string.IsNullOrEmpty(search))
            {
                allThreads = allThreads.Where(t =>
                    (t.Type == "private" && t.OtherUser != null &&
                     (t.OtherUser.DisplayName.Contains(search) || t.OtherUser.UserName.Contains(search))) ||
                    (t.Type == "group" && t.GroupName != null && t.GroupName.Contains(search))
                ).ToList();
            }

            ViewBag.SearchQuery = search;
            return View(allThreads);
        }

        [HttpPost]
        public async Task<IActionResult> TogglePin(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });

            var userId = _userManager.GetUserId(User);
            var conv = await _context.Conversations.FindAsync(id);
            if (conv == null || (conv.User1Id != userId && conv.User2Id != userId)) return NotFound();
            if (conv.User1Id == userId) conv.IsPinnedByUser1 = !conv.IsPinnedByUser1;
            else conv.IsPinnedByUser2 = !conv.IsPinnedByUser2;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ClearChat(int id, bool isGroup = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });

            var userId = _userManager.GetUserId(User);
            if (isGroup)
            {
                var group = await _context.GroupChats
                    .Include(g => g.Messages)
                    .FirstOrDefaultAsync(g => g.Id == id && g.Members.Any(m => m.UserId == userId));
                if (group == null) return NotFound();
                _context.GroupMessages.RemoveRange(group.Messages);
            }
            else
            {
                var conv = await _context.Conversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == id && (c.User1Id == userId || c.User2Id == userId));
                if (conv == null) return NotFound();
                _context.Messages.RemoveRange(conv.Messages);
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Чат очищен";
            return RedirectToAction("Chat", new { id, isGroup });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteChat(int id, bool isGroup = false, bool deleteForBoth = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });

            var userId = _userManager.GetUserId(User);
            if (isGroup)
            {
                var group = await _context.GroupChats
                    .Include(g => g.Members)
                    .Include(g => g.Messages)
                    .FirstOrDefaultAsync(g => g.Id == id && g.Members.Any(m => m.UserId == userId));
                if (group == null) return NotFound();
                _context.GroupMessages.RemoveRange(group.Messages);
                var member = group.Members.FirstOrDefault(m => m.UserId == userId);
                if (member != null) member.Left = true;
            }
            else
            {
                var conv = await _context.Conversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == id && (c.User1Id == userId || c.User2Id == userId));
                if (conv == null) return NotFound();
                _context.Messages.RemoveRange(conv.Messages);
                if (deleteForBoth) { conv.IsDeletedByUser1 = true; conv.IsDeletedByUser2 = true; }
                else if (conv.User1Id == userId) conv.IsDeletedByUser1 = true;
                else conv.IsDeletedByUser2 = true;
                if (conv.IsDeletedByUser1 && conv.IsDeletedByUser2) _context.Conversations.Remove(conv);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int messageId, bool isGroup = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });

            var userId = _userManager.GetUserId(User);
            if (isGroup)
            {
                var msg = await _context.GroupMessages.FindAsync(messageId);
                if (msg == null || msg.SenderId != userId) return Unauthorized();
                _context.GroupMessages.Remove(msg);
            }
            else
            {
                var msg = await _context.Messages.FindAsync(messageId);
                if (msg == null || msg.SenderId != userId) return Unauthorized();
                _context.Messages.Remove(msg);
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> EditMessage(int messageId, string text, bool isGroup = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });

            if (string.IsNullOrWhiteSpace(text)) return BadRequest();
            var userId = _userManager.GetUserId(User);
            if (isGroup)
            {
                var msg = await _context.GroupMessages.FindAsync(messageId);
                if (msg == null || msg.SenderId != userId) return Unauthorized();
                msg.Text = text.Trim(); msg.IsEdited = true;
            }
            else
            {
                var msg = await _context.Messages.FindAsync(messageId);
                if (msg == null || msg.SenderId != userId) return Unauthorized();
                msg.Text = text.Trim(); msg.IsEdited = true;
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> BlockUser(string userId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });

            var blockerId = _userManager.GetUserId(User);
            if (blockerId == null) return Challenge();
            if (blockerId == userId) return BadRequest("Нельзя заблокировать себя");
            var exists = await _context.UserBlocks.AnyAsync(b => b.BlockerId == blockerId && b.BlockedUserId == userId);
            if (!exists)
            {
                _context.UserBlocks.Add(new UserBlock { BlockerId = blockerId, BlockedUserId = userId, BlockedAt = DateTime.UtcNow });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });

            var blockerId = _userManager.GetUserId(User);
            var block = await _context.UserBlocks.FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedUserId == userId);
            if (block != null) { _context.UserBlocks.Remove(block); await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CreateGroup(string name, string memberUsernames)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Введите название группы");
                return View();
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var usernamesArray = memberUsernames?
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(u => u.Trim())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .ToArray() ?? Array.Empty<string>();

            System.Diagnostics.Debug.WriteLine($"=== CREATE GROUP ===");
            System.Diagnostics.Debug.WriteLine($"Group Name: {name}");
            System.Diagnostics.Debug.WriteLine($"Usernames count: {usernamesArray.Length}");
            foreach (var u in usernamesArray)
            {
                System.Diagnostics.Debug.WriteLine($"  - Username: '{u}'");
            }

            var group = new GroupChat
            {
                Name = name.Trim(),
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.GroupChats.Add(group);
            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"Group created with ID: {group.Id}");

            _context.GroupMembers.Add(new GroupMember
            {
                GroupChatId = group.Id,
                UserId = userId,
                IsAdmin = true,
                JoinedAt = DateTime.UtcNow,
                Left = false
            });

            int addedCount = 0;
            foreach (var username in usernamesArray)
            {
                var targetUser = await _userManager.FindByNameAsync(username)
                        ?? await _userManager.FindByEmailAsync(username)
                        ?? _context.Users.FirstOrDefault(u => u.DisplayName == username);

                if (targetUser != null)
                {
                    if (targetUser.Id != userId)
                    {
                        _context.GroupMembers.Add(new GroupMember
                        {
                            GroupChatId = group.Id,
                            UserId = targetUser.Id,
                            IsAdmin = false,
                            JoinedAt = DateTime.UtcNow,
                            Left = false
                        });
                        addedCount++;
                        System.Diagnostics.Debug.WriteLine($"Added user: {targetUser.UserName} ({targetUser.Id})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipping creator: {targetUser.UserName}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"User NOT found: '{username}'");
                }
            }

            await _context.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"Total members added: {addedCount}");
            System.Diagnostics.Debug.WriteLine($"=== END CREATE GROUP ===");

            TempData["Success"] = $"Группа создана! Добавлено участников: {addedCount}";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Chat(int id, bool isGroup = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });

            var userId = _userManager.GetUserId(User);
            if (isGroup)
            {
                var group = await _context.GroupChats
                    .Include(g => g.Members).ThenInclude(m => m.User)
                    .Include(g => g.Messages)
                    .FirstOrDefaultAsync(g => g.Id == id && g.Members.Any(m => m.UserId == userId && !m.Left));
                if (group == null) return NotFound();

                ViewBag.IsGroup = true;
                ViewBag.GroupName = group.Name;
                ViewBag.ConversationId = id;
                ViewBag.Members = group.Members.Where(m => !m.Left).ToList();
                ViewBag.MemberCount = group.Members.Count(m => !m.Left);

                var blockedIds = await _context.UserBlocks.Where(b => b.BlockerId == userId || b.BlockedUserId == userId)
                    .Select(b => b.BlockerId == userId ? b.BlockedUserId : b.BlockerId).ToListAsync();

                var allConvs = await _context.Conversations.Include(c => c.User1).Include(c => c.User2).Include(c => c.Messages).ToListAsync();
                var threads = allConvs
                    .Where(c => (c.User1Id == userId || c.User2Id == userId) && !blockedIds.Contains(c.User1Id == userId ? c.User2Id : c.User1Id))
                    .Where(c => !(c.User1Id == userId && c.IsDeletedByUser1) && !(c.User2Id == userId && c.IsDeletedByUser2))
                    .Select(c => new MessageThreadViewModel
                    {
                        Id = c.Id,
                        Type = "private",
                        OtherUser = c.User1Id == userId ? c.User2 : c.User1,
                        LastMessageText = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Text).FirstOrDefault(),
                        LastMessageAt = c.LastMessageAt,
                        UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderId != userId),
                        IsPinned = c.User1Id == userId ? c.IsPinnedByUser1 : c.IsPinnedByUser2
                    });
                var groupChats = await _context.GroupChats.Include(g => g.Members).Where(g => g.Members.Any(m => m.UserId == userId && !m.Left)).ToListAsync();
                var groupThreads = groupChats.Select(g => new MessageThreadViewModel
                {
                    Id = g.Id,
                    Type = "group",
                    GroupName = g.Name,
                    LastMessageText = g.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Text).FirstOrDefault(),
                    LastMessageAt = g.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.CreatedAt).FirstOrDefault(),
                    UnreadCount = g.Messages.Count(m => !m.IsRead && m.SenderId != userId),
                    IsPinned = false
                });
                ViewBag.Threads = threads.Concat(groupThreads).OrderByDescending(t => t.IsPinned).ThenByDescending(t => t.LastMessageAt).ToList();

                var unread = group.Messages.Where(m => !m.IsRead && m.SenderId != userId).ToList();
                foreach (var msg in unread) msg.IsRead = true;
                await _context.SaveChangesAsync();
                return View("GroupChat", group.Messages.Where(m => !m.IsDeleted).OrderBy(m => m.CreatedAt).ToList());
            }
            else
            {
                var conversation = await _context.Conversations
                    .Include(c => c.User1).Include(c => c.User2).Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == id && (c.User1Id == userId || c.User2Id == userId));
                if (conversation == null) return NotFound();
                var otherUser = conversation.User1Id == userId ? conversation.User2 : conversation.User1;
                if (otherUser != null && !otherUser.AllowPrivateMessages) ViewBag.MessagesDisabled = true;
                var blockedIds = await _context.UserBlocks.Where(b => b.BlockerId == userId || b.BlockedUserId == userId)
                    .Select(b => b.BlockerId == userId ? b.BlockedUserId : b.BlockerId).ToListAsync();
                var allConvs = await _context.Conversations.Include(c => c.User1).Include(c => c.User2).Include(c => c.Messages).ToListAsync();
                var threads = allConvs
                    .Where(c => (c.User1Id == userId || c.User2Id == userId) && !blockedIds.Contains(c.User1Id == userId ? c.User2Id : c.User1Id))
                    .Where(c => !(c.User1Id == userId && c.IsDeletedByUser1) && !(c.User2Id == userId && c.IsDeletedByUser2))
                    .Select(c => new MessageThreadViewModel
                    {
                        Id = c.Id,
                        Type = "private",
                        OtherUser = c.User1Id == userId ? c.User2 : c.User1,
                        LastMessageText = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Text).FirstOrDefault(),
                        LastMessageAt = c.LastMessageAt,
                        UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderId != userId),
                        IsPinned = c.User1Id == userId ? c.IsPinnedByUser1 : c.IsPinnedByUser2
                    });
                var groupChats = await _context.GroupChats
                    .Include(g => g.Members)
                    .Include(g => g.Messages)
                    .Where(g => g.Members.Any(m => m.UserId == userId && !m.Left))
                    .ToListAsync();
                var groupThreads = groupChats.Select(g => new MessageThreadViewModel
                {
                    Id = g.Id,
                    Type = "group",
                    GroupName = g.Name,
                    LastMessageText = g.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Text).FirstOrDefault(),
                    LastMessageAt = g.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.CreatedAt).FirstOrDefault(),
                    UnreadCount = g.Messages.Count(m => !m.IsRead && m.SenderId != userId),
                    IsPinned = false
                });
                ViewBag.Threads = threads.Concat(groupThreads)
                    .OrderByDescending(t => t.IsPinned)
                    .ThenByDescending(t => t.LastMessageAt)
                    .ToList();
                ViewBag.IsPinned = conversation.User1Id == userId ? conversation.IsPinnedByUser1 : conversation.IsPinnedByUser2;
                ViewBag.OtherUser = otherUser;
                ViewBag.ConversationId = id;
                var unread = conversation.Messages.Where(m => !m.IsRead && m.SenderId != userId).ToList();
                foreach (var msg in unread) msg.IsRead = true;
                await _context.SaveChangesAsync();
                return View("Chat", conversation.Messages.Where(m => !m.IsDeleted).OrderBy(m => m.CreatedAt).ToList());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Send(int conversationId, string? text, IFormFile? image, bool isGroup = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });

            if (string.IsNullOrWhiteSpace(text) && image == null) return RedirectToAction("Chat", new { id = conversationId, isGroup });
            var userId = _userManager.GetUserId(User);
            if (isGroup)
            {
                var group = await _context.GroupChats.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == conversationId && g.Members.Any(m => m.UserId == userId && !m.Left));
                if (group == null) return NotFound();
                var message = new GroupMessage { GroupChatId = conversationId, SenderId = userId!, Text = text?.Trim() ?? "", CreatedAt = DateTime.UtcNow, IsRead = false };
                if (image != null && image.Length > 0)
                {
                    var dir = Path.Combine(_environment.WebRootPath, "uploads", "chat");
                    Directory.CreateDirectory(dir);
                    var fn = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                    await using var fs = new FileStream(Path.Combine(dir, fn), FileMode.Create);
                    await image.CopyToAsync(fs);
                    message.ImageUrl = $"/uploads/chat/{fn}";
                }
                _context.GroupMessages.Add(message);
                await _context.SaveChangesAsync();
                return RedirectToAction("Chat", new { id = conversationId, isGroup = true });
            }
            else
            {
                var conversation = await _context.Conversations.FindAsync(conversationId);
                if (conversation == null || (conversation.User1Id != userId && conversation.User2Id != userId)) return NotFound();
                var otherUserId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;
                var isBlocked = await IsBlockedAsync(userId, otherUserId);
                if (isBlocked) { TempData["Error"] = "Вы не можете писать этому пользователю (заблокирован)"; return RedirectToAction("Chat", new { id = conversationId }); }
                var otherUser = await _userManager.FindByIdAsync(otherUserId);
                if (otherUser != null && !otherUser.AllowPrivateMessages) { TempData["Error"] = "Этот пользователь запретил получать личные сообщения"; return RedirectToAction("Chat", new { id = conversationId }); }
                var message = new Message { ConversationId = conversationId, SenderId = userId!, Text = text?.Trim() ?? "", CreatedAt = DateTime.UtcNow, IsRead = false };
                if (image != null && image.Length > 0)
                {
                    var dir = Path.Combine(_environment.WebRootPath, "uploads", "chat");
                    Directory.CreateDirectory(dir);
                    var fn = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                    await using var fs = new FileStream(Path.Combine(dir, fn), FileMode.Create);
                    await image.CopyToAsync(fs);
                    message.ImageUrl = $"/uploads/chat/{fn}";
                }
                _context.Messages.Add(message);
                conversation.LastMessageAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return RedirectToAction("Chat", new { id = conversationId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Start(string userId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.IsBanned == true) return RedirectToAction("Index", "Home", new { banned = 1 });

            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null) return Challenge();
            if (currentUserId == userId) return RedirectToAction(nameof(Index));
            var targetUser = await _userManager.FindByIdAsync(userId);
            var isBlocked = await IsBlockedAsync(currentUserId, userId);
            if (isBlocked) { TempData["Error"] = "Вы не можете писать этому пользователю (заблокирован)"; return RedirectToAction(nameof(Index)); }
            if (targetUser != null && !targetUser.AllowPrivateMessages) { TempData["Error"] = "Этот пользователь запретил получать личные сообщения"; return RedirectToAction(nameof(Index)); }
            var existing = await _context.Conversations.FirstOrDefaultAsync(c => (c.User1Id == currentUserId && c.User2Id == userId) || (c.User1Id == userId && c.User2Id == currentUserId));
            if (existing != null) return RedirectToAction("Chat", new { id = existing.Id });
            var conv = new Conversation { User1Id = currentUserId, User2Id = userId, LastMessageAt = DateTime.UtcNow };
            _context.Conversations.Add(conv);
            await _context.SaveChangesAsync();
            return RedirectToAction("Chat", new { id = conv.Id });
        }
    }

    public class MessageThreadViewModel
    {
        public int Id { get; set; }
        public string Type { get; set; } = "private";
        public ApplicationUser? OtherUser { get; set; }
        public string? GroupName { get; set; }
        public string? LastMessageText { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public bool IsPinned { get; set; }
    }
}