using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Seeders;

public class CompanySeeder : IDbSeeder
{
    public async Task SeedAsync(ApplicationDbContext context, IServiceProvider sp, CancellationToken ct = default)
    {
        if (await context.Companies.AnyAsync(ct))
            return;

        var now = DateTime.UtcNow;
        var publishers = new (string Name, string City, string Email)[]
        {
            ("Видавництво Старого Лева", "Львів", "info@starlev.com.ua"),
            ("Наш Формат", "Київ", "info@nashformat.ua"),
            ("Vivat", "Харків", "info@vivat.com.ua"),
            ("Ранок", "Харків", "info@ranok.com.ua"),
            ("Фоліо", "Харків", "info@folio.com.ua"),
            ("Клуб Сімейного Дозвілля", "Харків", "info@ksd.com.ua"),
            ("А-БА-БА-ГА-ЛА-МА-ГА", "Київ", "info@ababahalamaha.com.ua"),
            ("Дух і Літера", "Київ", "info@duh-i-litera.com.ua"),
            ("Смолоскип", "Київ", "info@smoloskyp.com.ua"),
            ("Кальварія", "Львів", "info@kalvaria.com.ua"),
            ("Піраміда", "Львів", "info@piramida.com.ua"),
            ("Ярославів Вал", "Київ", "info@val.com.ua"),
            ("Махаон-Україна", "Київ", "info@mahaon.com.ua"),
            ("Богдан", "Тернопіль", "info@bogdan.com.ua"),
            ("Навчальна книга — Богдан", "Тернопіль", "info@nk-bogdan.com.ua"),
            ("Літера ЛТД", "Київ", "info@litera.com.ua"),
            ("Генеза", "Київ", "info@geneza.com.ua"),
            ("Освіта", "Київ", "info@osvita.com.ua"),
            ("Магістр", "Харків", "info@magistr.com.ua"),
            ("Києво-Могилянська академія", "Київ", "info@kmacademia.com.ua"),
            ("Темпора", "Київ", "info@tempora.com.ua"),
            ("Критика", "Київ", "info@krytyka.com.ua"),
            ("Комубук", "Київ", "info@komubook.com.ua"),
            ("ArtHuss", "Київ", "info@arthuss.com.ua"),
            ("IST Publishing", "Київ", "info@istpublishing.com.ua"),
            ("BookChef", "Київ", "info@bookchef.ua"),
            ("Портал", "Львів", "info@portal.com.ua"),
            ("Чорні вівці", "Чернівці", "info@chornivivtsi.com.ua"),
            ("Видавництво 21", "Львів", "info@vydavnytstvo21.com.ua"),
            ("Лабораторія", "Київ", "info@laboratory.ua"),
            ("Віхола", "Київ", "info@vihola.com.ua"),
            ("Nebo Booklab Publishing", "Київ", "info@nebo.com.ua"),
            ("Фабула", "Харків", "info@fabula.com.ua"),
            ("Жорж", "Львів", "info@georges.com.ua"),
            ("Анетти Антоненко", "Львів", "info@anetta.com.ua"),
            ("Зелений Пес", "Київ", "info@zelenyipes.com.ua"),
            ("Рідна мова", "Київ", "info@ridnamova.com.ua"),
            ("Лілея-НВ", "Івано-Франківськ", "info@lileanv.com.ua"),
            ("Школа", "Запоріжжя", "info@shkola.com.ua"),
            ("Аверс", "Київ", "info@avers.com.ua"),
            ("Педагогічна думка", "Київ", "info@peddumka.com.ua"),
            ("Підручники і посібники", "Тернопіль", "info@pidruchnyky.com.ua"),
            ("Центр навчальної літератури", "Київ", "info@cnl.com.ua"),
            ("Веселка", "Київ", "info@veselka.com.ua"),
            ("Дніпро", "Київ", "info@dnipro.com.ua"),
            ("Український письменник", "Київ", "info@ukrpysmennyk.com.ua"),
            ("MIM Book", "Київ", "info@mimbook.com.ua"),
            ("Грані-Т", "Київ", "info@grani-t.com.ua"),
            ("Пегас", "Харків", "info@pegas.com.ua"),
            ("ACCА", "Київ", "info@acca.com.ua"),
        };

        var companies = publishers.Select((p, i) => new CompanyRecord
        {
            Id = Guid.NewGuid(),
            Name = p.Name,
            Slug = p.Name.ToLowerInvariant().Replace(' ', '-').Replace("—", "-").Replace("«", "").Replace("»", ""),
            Description = $"Видавництво «{p.Name}» — одне з провідних українських видавництв, засноване в м. {p.City}.",
            ContactEmail = p.Email,
            ContactPhone = $"+38044{2000000 + i:D7}",
            AddressStreet = $"вул. Книжкова, {i + 1}",
            AddressCity = p.City,
            AddressState = p.City,
            AddressPostalCode = $"0100{i % 10:D2}",
            AddressCountry = "Україна",
            IsApproved = true,
            ApprovedAt = now,
            Rating = Math.Round(4 + (decimal)Random.Shared.NextDouble(), 1),
            ReviewCount = Random.Shared.Next(10, 500),
            FollowerCount = Random.Shared.Next(50, 5000),
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();

        context.Companies.AddRange(companies);
        await context.SaveChangesAsync(ct);
    }
}
