using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebApplication4.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Существующие поля
        public int StatusId { get; set; }
        public int AppTypeId { get; set; }
        public int ComplexityLevelId { get; set; }
        public string? ApplicationUserId { get; set; }
        public string? RepositoryUrl { get; set; }
        public string? DemoUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int ViewCount { get; set; } = 0;

        // 🔹 НОВОЕ: Статус модерации
        public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;

        // 🔹 НОВОЕ: Причина отклонения
        public string? RejectionReason { get; set; }

        // 🔹 НОВОЕ: Кто и когда модерировал
        public string? ModeratedByUserId { get; set; }
        public DateTime? ModeratedAt { get; set; }

        // Навигационные свойства
        public Status? Status { get; set; }
        public AppType? AppType { get; set; }
        public ComplexityLevel? ComplexityLevel { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }
        public List<ProjectTechnology>? ProjectTechnologies { get; set; }
        
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Favorite>? Favorites { get; set; }
        public List<Image>? Images { get; set; }
    }
}