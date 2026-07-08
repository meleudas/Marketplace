using System.Text.Json;
using Marketplace.Application.Products.Catalog;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Seeders;

record BookDef(
    string Title,
    string Author,
    long CatId,
    string ParentSlug,
    string SubcategorySlug,
    string SubcategoryName,
    decimal Price,
    string? Genre = null);

public class ProductSeeder : IDbSeeder
{
    private const string FormatPaper = ProductCatalogFormats.Paper;
    private const string FormatElectronic = ProductCatalogFormats.Electronic;
    private const int BooksPerSubcategory = 7;

    private static readonly string[] BookPool =
    [
        "Кобзар", "Лісова пісня", "Тіні забутих предків", "Intermezzo", "Земля", "Майстер і Маргарита", "1984",
        "Старий і море", "Великий Гетсбі", "Гордість і упередження", "Джейн Ейр", "Війна і мир", "Анна Кареніна",
        "Злочин і кара", "Ідіот", "Брати Карамазови", "Маленький принц", "Хобіт", "Аліса в Країні чудес",
        "Пригоди Тома Сойєра", "Sapiens", "Atomic Habits", "Clean Code", "Design Patterns",
        "Коротка історія майже всього", "Thinking, Fast and Slow", "Deep Work", "Zero to One",
    ];

    private static readonly string[] AuthorPool =
    [
        "Тарас Шевченко", "Леся Українка", "Михайло Коцюбинський", "Ольга Кобилянська", "Михайло Булгаков", "Джордж Орвелл",
        "Ернест Гемінгуей", "Ф. Скотт Фіцджеральд", "Джейн Остін", "Лев Толстой", "Федір Достоєвський", "J.K. Rowling",
        "J.R.R. Tolkien", "Lewis Carroll", "Mark Twain", "Arthur Conan Doyle", "Agatha Christie", "Yuval Noah Harari",
        "James Clear", "Robert Martin", "Bill Bryson", "Daniel Kahneman", "Cal Newport", "Peter Thiel",
    ];

    public async Task SeedAsync(ApplicationDbContext context, IServiceProvider sp, CancellationToken ct = default)
    {
        if (await context.Products.AnyAsync(ct))
            return;

        var companies = await context.Companies.ToListAsync(ct);
        var now = DateTime.UtcNow;
        var rng = Random.Shared;
        var bookDefs = BuildBookDefs();

        var usedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var products = new List<ProductRecord>();
        var details = new List<ProductDetailRecord>();
        var images = new List<ProductImageRecord>();

        for (var bookIndex = 0; bookIndex < bookDefs.Count; bookIndex++)
        {
            var book = bookDefs[bookIndex];
            var formats = ResolveFormats(bookIndex, rng);
            var numEditions = rng.Next(3, Math.Min(8, companies.Count + 1));
            var assignedCompanies = companies.OrderBy(_ => rng.Next()).Take(numEditions).ToList();
            var genre = book.Genre ?? ResolveGenre(book.CatId);
            var titleSlug = SeedSlugHelper.ToSlug(book.Title);

            foreach (var company in assignedCompanies)
            {
                foreach (var format in formats)
                {
                    var formatSlug = FormatSlug(format);
                    var companySlug = ResolveCompanySlug(company);
                    var slug = ResolveUniqueSlug(titleSlug, formatSlug, companySlug, usedSlugs);
                    var stock = format == FormatElectronic
                        ? rng.Next(50, 500)
                        : rng.Next(5, 200);
                    var price = ResolveFormatPrice(book.Price, format, rng);

                    products.Add(new ProductRecord
                    {
                        CompanyId = company.Id,
                        Name = $"{book.Title} ({FormatLabel(format)})",
                        Slug = slug,
                        Description = $"{book.Author}. Книга з розділу «{book.SubcategoryName}».",
                        Price = price,
                        OldPrice = rng.Next(3) == 0 ? Math.Max(50, price + rng.Next(50, 200)) : null,
                        Stock = stock,
                        MinStock = 5,
                        CategoryId = book.CatId,
                        Status = 1,
                        HasVariants = false,
                        SubmittedByUserId = null,
                        IsDeleted = false,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });

                    details.Add(new ProductDetailRecord
                    {
                        Slug = slug,
                        AttributesRaw = JsonSerializer.Serialize(new
                        {
                            seed = true,
                            author = book.Author,
                            genre,
                            format,
                            subcategoryId = book.CatId,
                            subcategory = book.SubcategoryName,
                            parent = book.ParentSlug,
                        }),
                        Tags = ["seed", "book", book.SubcategorySlug, genre, $"format:{format}", "українська", "популярне"],
                        Brands = [book.Author],
                        SpecificationsRaw = JsonSerializer.Serialize(new
                        {
                            isbn = $"978-{rng.Next(100, 999)}-{rng.Next(1000, 9999)}-{rng.Next(10, 99)}-{rng.Next(1, 10)}",
                            pages = format == FormatElectronic ? (int?)null : rng.Next(100, 800),
                            year = 2020 + rng.Next(0, 6),
                            language = "Українська",
                            format = FormatLabel(format),
                        }),
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }
            }
        }

        context.Products.AddRange(products);
        await context.SaveChangesAsync(ct);

        for (var i = 0; i < details.Count; i++)
            details[i].ProductId = products[i].Id;

        context.ProductDetails.AddRange(details);

        images.AddRange(products.Select(p => new ProductImageRecord
        {
            ProductId = p.Id,
            ImageUrl = $"https://picsum.photos/seed/marketplace-book-{p.Id}/400/600",
            ThumbnailUrl = $"https://picsum.photos/seed/marketplace-book-{p.Id}/200/300",
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

    private static List<BookDef> BuildBookDefs()
    {
        var rootsById = BookCatalogCategorySeedData.All
            .Where(category => category.ParentId is null)
            .ToDictionary(category => category.Id);

        var subcategories = BookCatalogCategorySeedData.All
            .Where(category => category.ParentId is not null)
            .OrderBy(category => category.ParentId)
            .ThenBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .ToList();

        var bookDefs = new List<BookDef>(subcategories.Count * BooksPerSubcategory);
        var productIndex = 0;

        for (var subcategoryIndex = 0; subcategoryIndex < subcategories.Count; subcategoryIndex++)
        {
            var subcategory = subcategories[subcategoryIndex];
            var parent = rootsById[subcategory.ParentId!.Value];

            for (var editionIndex = 0; editionIndex < BooksPerSubcategory; editionIndex++)
            {
                productIndex++;
                var poolIndex = (subcategoryIndex + editionIndex) % BookPool.Length;
                var authorIndex = (productIndex + subcategoryIndex + editionIndex) % AuthorPool.Length;
                var price = 219m + (productIndex * 13 + subcategoryIndex * 7 + editionIndex * 3) % 480;

                bookDefs.Add(new BookDef(
                    $"{subcategory.Name}: {BookPool[poolIndex]}",
                    AuthorPool[authorIndex],
                    subcategory.Id,
                    parent.Slug,
                    subcategory.Slug,
                    subcategory.Name,
                    price));
            }
        }

        return bookDefs;
    }

    private static IReadOnlyList<string> ResolveFormats(int bookIndex, Random rng)
    {
        return (bookIndex % 3, rng.Next(4)) switch
        {
            (0, _) => [FormatPaper, FormatElectronic],
            (1, _) => [FormatPaper],
            (2, 0) => [FormatPaper, FormatElectronic],
            (2, _) => [FormatElectronic],
            _ => [FormatPaper],
        };
    }

    private static decimal ResolveFormatPrice(decimal basePrice, string format, Random rng)
    {
        var adjusted = format == FormatElectronic
            ? basePrice * 0.65m
            : basePrice;

        adjusted += (rng.Next(-50, 100) / 10m) * 10;
        return Math.Max(format == FormatElectronic ? 29 : 50, adjusted);
    }

    private static string FormatLabel(string format) => ProductCatalogFormats.GetLabel(format);

    private static string FormatSlug(string format) =>
        format switch
        {
            FormatElectronic => "electronic",
            FormatPaper => "paper",
            _ => SeedSlugHelper.ToSlug(format),
        };

    private static string ResolveCompanySlug(CompanyRecord company) =>
        SeedSlugHelper.ToSlug(string.IsNullOrWhiteSpace(company.Slug) ? company.Name : company.Slug);

    private static string ResolveUniqueSlug(
        string titleSlug,
        string formatSlug,
        string companySlug,
        HashSet<string> usedSlugs)
    {
        var baseSlug = $"{titleSlug}-{formatSlug}";
        if (usedSlugs.Add(baseSlug))
            return baseSlug;

        var withCompany = $"{baseSlug}-{companySlug}";
        if (usedSlugs.Add(withCompany))
            return withCompany;

        for (var suffix = 2; suffix < 1000; suffix++)
        {
            var slug = $"{withCompany}-{suffix}";
            if (usedSlugs.Add(slug))
                return slug;
        }

        throw new InvalidOperationException($"Could not generate unique slug for '{baseSlug}'.");
    }

    private static string ResolveGenre(long categoryId) => categoryId switch
    {
        11 or 16 or 19 or 20 => "fiction",
        12 => "detective",
        13 => "fantasy",
        14 => "fiction",
        15 => "thriller",
        17 => "sci-fi",
        18 => "fiction",
        21 => "memoir",
        22 or 23 or 26 or 27 or 28 => "non-fiction",
        24 => "self-help",
        25 => "business",
        31 or 32 or 33 or 35 or 36 or 37 or 38 => "children",
        34 => "ya",
        41 or 42 or 43 or 44 or 46 or 47 or 48 => "education",
        45 => "it",
        _ => "fiction",
    };
}
