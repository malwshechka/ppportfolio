using WebApplication4.Data;
using WebApplication4.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;

namespace WebApplication4;

public static class DbInitializer
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Заполнение справочников (AppTypes, Statuses, ComplexityLevels, Technologies)
        if (!context.AppTypes.Any())
        {
            context.AppTypes.AddRange(
                new AppType { Name = "Веб-приложение" },
                new AppType { Name = "Мобильное приложение" },
                new AppType { Name = "Десктопное приложение" },
                new AppType { Name = "Игра" },
                new AppType { Name = "Библиотека/API" },
                new AppType { Name = "Скрипт/Автоматизация" }
            );
        }

        if (!context.Statuses.Any())
        {
            context.Statuses.AddRange(
                new Status { Name = "В разработке" },
                new Status { Name = "Завершён" },
                new Status { Name = "В архиве" },
                new Status { Name = "В продакшене" }
            );
        }

        if (!context.ComplexityLevels.Any())
        {
            context.ComplexityLevels.AddRange(
                new ComplexityLevel { Name = "Pet-проект" },
                new ComplexityLevel { Name = "Средний" },
                new ComplexityLevel { Name = "Сложный" },
                new ComplexityLevel { Name = "Коммерческий" }
            );
        }

        if (!context.Technologies.Any())
        {
            context.Technologies.AddRange(
                new Technology { Name = "C#" },
                new Technology { Name = "ASP.NET Core" },
                new Technology { Name = "JavaScript" },
                new Technology { Name = "React" },
                new Technology { Name = "Vue.js" },
                new Technology { Name = "Angular" },
                new Technology { Name = "Python" },
                new Technology { Name = "Django" },
                new Technology { Name = "SQL" },
                new Technology { Name = "Entity Framework" },
                new Technology { Name = "Docker" },
                new Technology { Name = "Git" }
            );
        }

        await context.SaveChangesAsync();

        // 2. Создание роли Admin, если её нет
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // 3. Создание пользователя-администратора, если его нет
        var adminEmail = "admin@portfolio.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(adminUser, "Admin123!");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}