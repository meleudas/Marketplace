using System.Text.Json;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Seeders;

record BookDef(string Title, string Author, long CatId, decimal Price);

public class ProductSeeder : IDbSeeder
{
    public async Task SeedAsync(ApplicationDbContext context, IServiceProvider sp, CancellationToken ct = default)
    {
        if (await context.Products.AnyAsync(ct))
            return;

        var companies = await context.Companies.ToListAsync(ct);
        var now = DateTime.UtcNow;
        var rng = Random.Shared;

        var bookDefs = new List<BookDef>
        {
            new("Солодка Даруся", "Марія Матіос", 11, 320),
            new("Записки українського самашедшого", "Ліна Костенко", 11, 280),
            new("Місто", "Валер'ян Підмогильний", 11, 250),
            new("Танго смерті", "Юрій Винничук", 11, 350),
            new("Музей покинутих секретів", "Оксана Забужко", 11, 380),
            new("1984", "Джордж Орвелл", 11, 250),
            new("Чорний ворон", "Василь Шкляр", 12, 350),
            new("Століття Якова", "Володимир Лис", 12, 320),
            new("Мазепа", "Богдан Лепкий", 12, 400),
            new("Тигролови", "Іван Багряний", 12, 290),
            new("Ніч на полонині", "Марко Черемшина", 13, 220),
            new("The Notebook", "Ніколас Спаркс", 13, 280),
            new("Гордість і упередження", "Джейн Остін", 13, 260),
            new("Кобзар (кишеньковий)", "Тарас Шевченко", 14, 180),
            new("На крилах пісень", "Леся Українка", 14, 200),
            new("Поезії", "Ліна Костенко", 14, 280),
            new("Котигорошко", "Народна казка", 21, 150),
            new("Червона шапочка", "Шарль Перро", 21, 140),
            new("Абетка", "Збірка", 22, 180),
            new("Сорока-ворона", "Народні потішки", 22, 120),
            new("Гаррі Поттер і філософський камінь", "Дж. Ролінґ", 23, 380),
            new("Голодні ігри", "Сюзанна Коллінз", 23, 280),
            new("7 звичок надзвичайно ефективних людей", "Стівен Кові", 31, 450),
            new("Від хорошого до великого", "Джим Коллінз", 31, 420),
            new("Start with Why", "Саймон Сінек", 31, 340),
            new("Сила звички", "Чарльз Дахіґґ", 32, 380),
            new("Атомні звички", "Джеймс Клір", 32, 350),
            new("Багатий тато, бідний тато", "Роберт Кійосакі", 33, 320),
            new("Розумний інвестор", "Бенджамін Грем", 33, 520),
            new("Думай і багатій", "Наполеон Гілл", 33, 280),
            new("Clean Code", "Роберт Мартін", 41, 550),
            new("Pragmatic Programmer", "Ендрю Гант", 41, 480),
            new("Design Patterns", "Банда чотирьох", 41, 520),
            new("C# та .NET", "Марк Прайс", 41, 490),
            new("The DevOps Handbook", "Джин Кім", 42, 450),
            new("Docker: Up & Running", "Шон Кейн", 42, 420),
            new("Kubernetes: Up and Running", "Брендан Бернс", 42, 480),
            new("Python для аналізу даних", "Вес МакКінні", 43, 480),
            new("Hands-On Machine Learning", "Аурельєн Жерон", 43, 550),
            new("Deep Learning", "Ян Ґудфеллоу", 43, 620),
            new("The Hacker Playbook 3", "Пітер Кім", 44, 480),
            new("Web Application Security", "Ендрю Гоффман", 44, 420),
            new("English Grammar in Use", "Реймонд Мерфі", 51, 350),
            new("Deutsch für Anfänger", "Збірник", 51, 320),
            new("Польська мова для українців", "Збірник", 51, 260),
            new("Sapiens", "Ювал Ной Харарі", 52, 420),
            new("Космос", "Карл Саган", 52, 360),
            new("Homo Deus", "Ювал Ной Харарі", 53, 420),
            new("Критика чистого розуму", "Іммануїл Кант", 53, 380),
            new("Кобзар (повне зібрання)", "Тарас Шевченко", 61, 450),
            new("Лісова пісня", "Леся Українка", 61, 220),
            new("Тіні забутих предків", "Михайло Коцюбинський", 61, 200),
            new("Кайдашева сім'я", "Іван Нечуй-Левицький", 61, 250),
            new("Хіба ревуть воли, як ясла повні?", "Панас Мирний", 61, 280),
            new("Енеїда", "Іван Котляревський", 61, 250),
            new("Війна і мир", "Лев Толстой", 62, 550),
            new("Злочин і кара", "Федір Достоєвський", 62, 420),
            new("Майстер і Маргарита", "Михайло Булгаков", 62, 380),
            new("Анна Кареніна", "Лев Толстой", 62, 480),
            new("Великий Гетсбі", "Френсіс Фіцджеральд", 62, 280),
            new("Сто років самотності", "Ґабрієль Ґарсія Маркес", 62, 380),
            new("Іліада", "Гомер", 63, 320),
            new("Одіссея", "Гомер", 63, 300),
            new("Медитації", "Марк Аврелій", 63, 250),
            new("Володар перснів: Братство персня", "Дж. Толкін", 71, 450),
            new("Володар перснів: Дві вежі", "Дж. Толкін", 71, 420),
            new("Володар перснів: Повернення короля", "Дж. Толкін", 71, 450),
            new("Гра престолів", "Джордж Мартін", 71, 480),
            new("Відьмак: Останнє бажання", "Анджей Сапковський", 71, 350),
            new("Дюна", "Френк Герберт", 72, 450),
            new("Фундація", "Айзек Азімов", 72, 350),
            new("Нейромант", "Вільям Ґібсон", 72, 340),
            new("Марсіянин", "Енді Вейр", 72, 360),
            new("Соляріс", "Станіслав Лем", 72, 300),
            new("Американські боги", "Ніл Ґейман", 73, 380),
            new("Небудьде", "Ніл Ґейман", 73, 320),
            new("Вбивство у «Східному експресі»", "Агата Крісті", 81, 280),
            new("Собака Баскервілів", "Артур Конан Дойл", 81, 260),
            new("Пригоди Шерлока Холмса", "Артур Конан Дойл", 81, 340),
            new("Дівчина з тату дракона", " Стіґ Ларссон", 82, 350),
            new("Зникла", "Ґіліан Флінн", 82, 320),
            new("Мовчазний пацієнт", "Алекс Міхаелідес", 82, 310),
            new("Хрещений батько", "Маріо П'юзо", 83, 380),
            new("Мовчання ягнят", "Томас Гарріс", 83, 320),
            new("Код да Вінчі", "Ден Браун", 83, 340),
        };

        var index = 0;
        var products = new List<ProductRecord>();
        var details = new List<ProductDetailRecord>();
        var images = new List<ProductImageRecord>();

        foreach (var book in bookDefs)
        {
            var numEditions = rng.Next(3, Math.Min(8, companies.Count + 1));
            var assignedCompanies = companies.OrderBy(_ => rng.Next()).Take(numEditions).ToList();

            foreach (var company in assignedCompanies)
            {
                index++;
                var slug = $"{book.Title.ToLowerInvariant().Replace(' ', '-').Replace("«", "").Replace("»", "").Replace(":", "").Replace("'", "").Replace(",", "")}-{index}";
                var stock = rng.Next(5, 200);
                var price = book.Price + (rng.Next(-50, 100) / 10m) * 10;

                products.Add(new ProductRecord
                {
                    CompanyId = company.Id,
                    Name = book.Title,
                    Slug = slug,
                    Description = $"Книга «{book.Title}» — {book.Author}. Українською мовою.",
                    Price = Math.Max(50, price),
                    OldPrice = rng.Next(3) == 0 ? Math.Max(50, price + rng.Next(50, 200)) : null,
                    Stock = stock,
                    MinStock = 5,
                    CategoryId = book.CatId,
                    Status = 1,
                    SubmittedByUserId = null,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                });

                details.Add(new ProductDetailRecord
                {
                    Slug = slug,
                    Tags = new[] { book.CatId switch { >= 61 and <= 63 => "класика", >= 71 and <= 73 => "фентезі", >= 81 and <= 83 => "детектив", >= 41 and <= 44 => "it", >= 51 and <= 53 => "освіта", >= 31 and <= 33 => "бізнес", >= 21 and <= 23 => "дитяча", _ => "художня" }, "українська", "популярне" },
                    Brands = new[] { book.Author },
                    SpecificationsRaw = JsonSerializer.Serialize(new { isbn = $"978-{rng.Next(100, 999)}-{rng.Next(1000, 9999)}-{rng.Next(10, 99)}-{rng.Next(1, 10)}", pages = rng.Next(100, 800), year = 2020 + rng.Next(0, 6), language = "Українська", format = "Тверда обкладинка" }),
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }

        context.Products.AddRange(products);
        await context.SaveChangesAsync(ct);

        for (int i = 0; i < details.Count; i++)
            details[i].ProductId = products[i].Id;

        context.ProductDetails.AddRange(details);

        images.AddRange(products.Select(p => new ProductImageRecord
        {
            ProductId = p.Id,
            ImageUrl = $"https://via.placeholder.com/600x900?text={Uri.EscapeDataString(p.Name)}",
            ThumbnailUrl = $"https://via.placeholder.com/150x225?text={Uri.EscapeDataString(p.Name)}",
            OriginalObjectKey = $"products/{p.Id}/original.jpg",
            ImageObjectKey = $"products/{p.Id}/image.jpg",
            ThumbnailObjectKey = $"products/{p.Id}/thumb.jpg",
            AltText = p.Name,
            SortOrder = 0,
            IsMain = true,
            CreatedAt = now,
            UpdatedAt = now,
        }));

        context.ProductImages.AddRange(images);
        await context.SaveChangesAsync(ct);
    }
}
