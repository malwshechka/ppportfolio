using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebApplication4.Models
{
    public class ProfileViewModel
    {
        [Display(Name = "Отображаемое имя")]
        public string? DisplayName { get; set; }

        [Display(Name = "О себе")]
        public string? Bio { get; set; }

        [Display(Name = "Навыки")]
        public string? Skills { get; set; }

        [Display(Name = "GitHub")]
        public string? GitHubUrl { get; set; }

        [Display(Name = "LinkedIn")]
        public string? LinkedInUrl { get; set; }

        [Display(Name = "Telegram")]
        public string? TelegramUrl { get; set; }

        public string? Email { get; set; }
        public DateTime? RegisteredAt { get; set; }
        public int ProjectsCount { get; set; }
        public int ReviewsCount { get; set; }
        public double AverageRating { get; set; }

        [Display(Name = "Фото профиля")]
        public IFormFile? ProfilePhoto { get; set; }
        public string? ProfilePhotoUrl { get; set; }

        // 🔹 ДОБАВЛЕНО: Разрешение личных сообщений
        [Display(Name = "Разрешить личные сообщения")]
        public bool AllowPrivateMessages { get; set; }
        public int SubscribersCount { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Введите текущий пароль")]
        [DataType(DataType.Password)]
        [Display(Name = "Текущий пароль")]
        public string? OldPassword { get; set; }

        [Required(ErrorMessage = "Введите новый пароль")]
        [StringLength(100, ErrorMessage = "Пароль должен содержать от 6 до 100 символов", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Новый пароль")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Подтвердите новый пароль")]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        public string? ConfirmPassword { get; set; }
    }


public class ProjectViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название проекта")]
        [Display(Name = "Название")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Введите описание")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "Тип приложения")]
        public int AppTypeId { get; set; }

        [Display(Name = "Статус")]
        public int StatusId { get; set; }

        [Display(Name = "Уровень сложности")]
        public int ComplexityLevelId { get; set; }

        [Display(Name = "Ссылка на репозиторий")]
        public string? RepositoryUrl { get; set; }

        [Display(Name = "Ссылка на демо")]
        public string? DemoUrl { get; set; }

        [Display(Name = "Технологии")]
        public List<int>? SelectedTechnologyIds { get; set; }

        [Display(Name = "Фотографии проекта")]
        public List<IFormFile>? Images { get; set; }
        public List<Image>? ExistingImages { get; set; }
        [Display(Name = "Разрешить личные сообщения")]
        public bool AllowPrivateMessages { get; set; } = true;
    }

    public class UserPortfolioViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public List<Project> Projects { get; set; } = new();
        public int TotalProjects { get; set; }
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
    }

    public class ReviewViewModel
    {
        [Required(ErrorMessage = "Введите текст отзыва")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Отзыв должен содержать от 10 до 500 символов")]
        [Display(Name = "Ваш отзыв")]
        public string Text { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Оценка должна быть от 1 до 5")]
        [Display(Name = "Оценка")]
        public int Rating { get; set; }
    }
}