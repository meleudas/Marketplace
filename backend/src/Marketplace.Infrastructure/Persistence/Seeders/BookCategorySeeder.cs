using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Seeders;

public class BookCategorySeeder : IDbSeeder
{
    public async Task SeedAsync(ApplicationDbContext context, IServiceProvider sp, CancellationToken ct = default)
    {
        if (await context.Categories.AnyAsync(ct))
            return;

        var now = DateTime.UtcNow;
        var categories = new List<CategoryRecord>
        {
            new() { Id = 1, Name = "Художня література", Slug = "khudozhnia-literatura", Description = "Романи, повісті, оповідання, поезія", SortOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 11, Name = "Сучасна проза", Slug = "suchasna-proza", ParentId = 1, SortOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 12, Name = "Історичні романи", Slug = "istorychni-romany", ParentId = 1, SortOrder = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 13, Name = "Любовні романи", Slug = "liubovni-romany", ParentId = 1, SortOrder = 3, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 14, Name = "Поезія", Slug = "poeziia", ParentId = 1, SortOrder = 4, IsActive = true, CreatedAt = now, UpdatedAt = now },

            new() { Id = 2, Name = "Дитяча література", Slug = "dytiacha-literatura", Description = "Казки, розвивальні книги, підліткова література", SortOrder = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 21, Name = "Казки", Slug = "kazky", ParentId = 2, SortOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 22, Name = "Для найменших", Slug = "dlia-naimenshykh", ParentId = 2, SortOrder = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 23, Name = "Підліткова література (YA)", Slug = "pidlitkova-literatura", ParentId = 2, SortOrder = 3, IsActive = true, CreatedAt = now, UpdatedAt = now },

            new() { Id = 3, Name = "Бізнес та саморозвиток", Slug = "biznes-ta-samorozvytok", Description = "Менеджмент, лідерство, фінанси, особистісний ріст", SortOrder = 3, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 31, Name = "Менеджмент та лідерство", Slug = "menedzhment-ta-liderstvo", ParentId = 3, SortOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 32, Name = "Саморозвиток та мотивація", Slug = "samorozvytok-ta-motyvatsiia", ParentId = 3, SortOrder = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 33, Name = "Фінанси та інвестиції", Slug = "finansy-ta-investytsii", ParentId = 3, SortOrder = 3, IsActive = true, CreatedAt = now, UpdatedAt = now },

            new() { Id = 4, Name = "Технічні та IT книги", Slug = "tekhnichni-ta-it-knyhy", Description = "Програмування, DevOps, Data Science, кібербезпека", SortOrder = 4, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 41, Name = "Програмування", Slug = "prohramuvannia", ParentId = 4, SortOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 42, Name = "DevOps та інфраструктура", Slug = "devops-ta-infrastruktura", ParentId = 4, SortOrder = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 43, Name = "Data Science та AI", Slug = "data-science-ta-ai", ParentId = 4, SortOrder = 3, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 44, Name = "Кібербезпека", Slug = "kiberbezpeka", ParentId = 4, SortOrder = 4, IsActive = true, CreatedAt = now, UpdatedAt = now },

            new() { Id = 5, Name = "Освітні та підручники", Slug = "osvitni-ta-pidruchnyky", Description = "Іноземні мови, природничі науки, гуманітарні науки", SortOrder = 5, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 51, Name = "Іноземні мови", Slug = "inozemni-movy", ParentId = 5, SortOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 52, Name = "Природничі науки", Slug = "pryrodnychi-nauky", ParentId = 5, SortOrder = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 53, Name = "Гуманітарні науки", Slug = "humanitarni-nauky", ParentId = 5, SortOrder = 3, IsActive = true, CreatedAt = now, UpdatedAt = now },

            new() { Id = 6, Name = "Класична література", Slug = "klasychna-literatura", Description = "Українська та світова класика, антична література", SortOrder = 6, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 61, Name = "Українська класика", Slug = "ukrainska-klasyka", ParentId = 6, SortOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 62, Name = "Зарубіжна класика", Slug = "zarubizhna-klasyka", ParentId = 6, SortOrder = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 63, Name = "Антична література", Slug = "antychna-literatura", ParentId = 6, SortOrder = 3, IsActive = true, CreatedAt = now, UpdatedAt = now },

            new() { Id = 7, Name = "Фентезі та Sci-Fi", Slug = "fentezi-ta-scifi", Description = "Епічне фентезі, наукова фантастика, міське фентезі", SortOrder = 7, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 71, Name = "Епічне фентезі", Slug = "epichne-fentezi", ParentId = 7, SortOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 72, Name = "Наукова фантастика", Slug = "naukova-fantastyka", ParentId = 7, SortOrder = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 73, Name = "Міське фентезі", Slug = "miske-fentezi", ParentId = 7, SortOrder = 3, IsActive = true, CreatedAt = now, UpdatedAt = now },

            new() { Id = 8, Name = "Детективи та трилери", Slug = "detektyvy-ta-trylery", Description = "Класичні детективи, психологічні трилери, кримінальні романи", SortOrder = 8, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 81, Name = "Класичні детективи", Slug = "klasychni-detektyvy", ParentId = 8, SortOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 82, Name = "Психологічні трилери", Slug = "psykholohichni-trylery", ParentId = 8, SortOrder = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = 83, Name = "Кримінальні романи", Slug = "kryminalni-romany", ParentId = 8, SortOrder = 3, IsActive = true, CreatedAt = now, UpdatedAt = now },
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync(ct);
    }
}
