using Marketplace.Infrastructure.Identity.Entities;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Infrastructure.Persistence.Seeders;

public class UserSeeder : IDbSeeder
{
    public async Task SeedAsync(ApplicationDbContext context, IServiceProvider sp, CancellationToken ct = default)
    {
        if (await context.MarketplaceUsers.AnyAsync(ct))
            return;

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var now = DateTime.UtcNow;
        var password = "BookMarket1!";

        var firstNames = new[] { "Олександр", "Марія", "Іван", "Анна", "Дмитро", "Олена", "Андрій", "Катерина",
            "Михайло", "Наталія", "Тарас", "Юлія", "Володимир", "Оксана", "Сергій", "Ірина",
            "Василь", "Тетяна", "Петро", "Вікторія", "Максим", "Світлана", "Роман", "Людмила",
            "Євген", "Надія", "Віталій", "Ганна", "Олег", "Ольга", "Богдан", "Любов",
            "Артем", "Зоя", "Данило", "Віра", "Павло", "Поліна", "Ярослав", "Аліна",
            "Валентин", "Олеся", "Владислав", "Марина", "Станіслав", "Валентина", "Юрій", "Ніна",
            "Ігор", "Лілія" };

        var lastNames = new[] { "Шевченко", "Коваленко", "Бондаренко", "Ткаченко", "Кравчук",
            "Мельник", "Олійник", "Савченко", "Бойко", "Кузьменко",
            "Марченко", "Лисенко", "Пономаренко", "Гаврилюк", "Левченко",
            "Данилюк", "Руденко", "Грищенко", "Козак", "Костюк",
            "Захарченко", "Федорук", "Гончарук", "Тимошенко", "Майборода",
            "Семенюк", "Романюк", "Остапчук", "Гордієнко", "Пилипенко",
            "Дорошенко", "Приходько", "Петренко", "Матвієнко", "Соколюк",
            "Дем'янчук", "Волошин", "Кривенко", "Павлюк", "Чорновіл",
            "Івасюк", "Герасименко", "Довженко", "Терещенко", "Кобилянський",
            "Харченко", "Драч", "Стус", "Порошенко", "Мазепа" };

        var users = new List<(ApplicationUser AppUser, MarketplaceUserRecord Record)>();
        var rng = Random.Shared;

        var admins = new List<(string Email, string First, string Last)>
        {
            ("admin@bookmarket.ua", "Адмін", "Головний"),
            ("moderator@bookmarket.ua", "Модератор", "Старший"),
        };

        foreach (var (email, first, last) in admins)
        {
            var appUser = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            users.Add((appUser, new MarketplaceUserRecord
            {
                FirstName = first, LastName = last, Role = 4,
                IsVerified = true, CreatedAt = now, UpdatedAt = now
            }));
        }

        for (int i = 0; i < 198; i++)
        {
            var fn = firstNames[i % firstNames.Length];
            var ln = lastNames[i % lastNames.Length];
            var email = $"user{i + 1}@bookmarket.ua";
            var isSeller = i < 48;

            var appUser = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            users.Add((appUser, new MarketplaceUserRecord
            {
                FirstName = fn, LastName = ln, Role = isSeller ? 2 : 1,
                IsVerified = true,
                Birthday = new DateTime(1970 + rng.Next(18, 50), rng.Next(1, 13), rng.Next(1, 29)),
                LastLoginAt = now.AddDays(-rng.Next(0, 30)),
                CreatedAt = now, UpdatedAt = now
            }));
        }

        foreach (var (appUser, record) in users)
        {
            var result = await userManager.CreateAsync(appUser, password);
            if (!result.Succeeded)
                continue;

            var role = record.Role switch { 4 => "Admin", 2 => "Seller", _ => "Buyer" };
            await userManager.AddToRoleAsync(appUser, role);
            record.Id = appUser.Id;
            context.MarketplaceUsers.Add(record);
        }

        await context.SaveChangesAsync(ct);
    }
}
