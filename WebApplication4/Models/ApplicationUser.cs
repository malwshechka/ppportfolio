using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace WebApplication4.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Основные свойства
        public string? DisplayName { get; set; }
        public string? Bio { get; set; }
        public string? Skills { get; set; }
        public string? GitHubUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? TelegramUrl { get; set; }
        public DateTime? RegisteredAt { get; set; } = DateTime.UtcNow;
        public string? ProfilePhotoUrl { get; set; }

        // Разрешение на сообщения
        public bool AllowPrivateMessages { get; set; } = true;

        // 🔹 НОВЫЕ СВОЙСТВА ДЛЯ АДМИН-БЛОКИРОВКИ
        public bool IsBanned { get; set; } = false;
        public DateTime? BannedAt { get; set; }
        public string? BanReason { get; set; }

        // Навигационные свойства
        public ICollection<Project>? Projects { get; set; }
        public ICollection<Review>? Reviews { get; set; }
        public ICollection<Favorite>? Favorites { get; set; }
        public ICollection<Message>? SentMessages { get; set; }

        // Подписки
        public ICollection<UserSubscription>? Subscribers { get; set; }
        public ICollection<UserSubscription>? Subscriptions { get; set; }
    }
}