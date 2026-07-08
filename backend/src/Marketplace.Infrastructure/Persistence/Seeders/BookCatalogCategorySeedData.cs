namespace Marketplace.Infrastructure.Persistence.Seeders;

/// <summary>
/// Canonical bookstore category tree. Keep in sync with
/// <c>backend/scripts/seed-test-data.sql</c> section 5 (categories INSERT).
/// </summary>
internal static class BookCatalogCategorySeedData
{
    internal sealed record CategoryDef(
        long Id,
        string Name,
        string Slug,
        long? ParentId,
        string Description,
        string MetaRaw,
        int SortOrder);

    internal static IReadOnlyList<CategoryDef> All { get; } =
    [
        new(1, "Художня література", "fiction", null, "Романи, детективи, фантастика та класика", """{"seed":true,"segment":"books","level":"root"}""", 1),
        new(11, "Романи", "fiction-novels", 1, "Художні романи", """{"seed":true,"segment":"books","parent":"fiction"}""", 1),
        new(12, "Детективи", "fiction-detectives", 1, "Детективні романи", """{"seed":true,"segment":"books","parent":"fiction"}""", 2),
        new(13, "Фентезі", "fiction-fantasy", 1, "Фентезійні романи", """{"seed":true,"segment":"books","parent":"fiction"}""", 3),
        new(14, "Класика", "fiction-classics", 1, "Класична художня література", """{"seed":true,"segment":"books","parent":"fiction"}""", 4),
        new(15, "Трилери", "fiction-thrillers", 1, "Психологічні та саспенс трилери", """{"seed":true,"segment":"books","parent":"fiction"}""", 5),
        new(16, "Проза", "fiction-prose", 1, "Сучасна та класична проза", """{"seed":true,"segment":"books","parent":"fiction"}""", 6),
        new(17, "Фантастика", "fiction-sci-fi", 1, "Наукова фантастика", """{"seed":true,"segment":"books","parent":"fiction"}""", 7),
        new(18, "Містика і жахи", "fiction-mystery-horror", 1, "Містика, horror та dark fiction", """{"seed":true,"segment":"books","parent":"fiction"}""", 8),
        new(19, "Пригоди", "fiction-adventure", 1, "Пригодницькі романи", """{"seed":true,"segment":"books","parent":"fiction"}""", 9),
        new(20, "Колекційні і лімітовані видання", "fiction-collectible", 1, "Колекційні та лімітовані видання", """{"seed":true,"segment":"books","parent":"fiction"}""", 10),

        new(2, "Документальна література", "documentary", null, "Біографії, історія, наука та нон-фікшн", """{"seed":true,"segment":"books","level":"root"}""", 2),
        new(21, "Біографії та мемуари", "documentary-biographies", 2, "Біографії, автобіографії та мемуари", """{"seed":true,"segment":"books","parent":"documentary"}""", 1),
        new(22, "Історія", "documentary-history", 2, "Історична документальна література", """{"seed":true,"segment":"books","parent":"documentary"}""", 2),
        new(23, "Наука та популяризація", "documentary-science", 2, "Науково-популярні видання", """{"seed":true,"segment":"books","parent":"documentary"}""", 3),
        new(24, "Психологія та саморозвиток", "documentary-psychology", 2, "Психологія, мотивація, soft skills", """{"seed":true,"segment":"books","parent":"documentary"}""", 4),
        new(25, "Бізнес та економіка", "documentary-business", 2, "Бізнес, менеджмент, фінанси", """{"seed":true,"segment":"books","parent":"documentary"}""", 5),
        new(26, "Політика та суспільство", "documentary-politics", 2, "Політика, соціологія, суспільні процеси", """{"seed":true,"segment":"books","parent":"documentary"}""", 6),
        new(27, "Подорожі", "documentary-travel", 2, "Подорожі, країнознавство, репортажі", """{"seed":true,"segment":"books","parent":"documentary"}""", 7),
        new(28, "True crime", "documentary-true-crime", 2, "Реальні злочини та розслідування", """{"seed":true,"segment":"books","parent":"documentary"}""", 8),

        new(3, "Дитяча література", "children", null, "Книги для дітей та підлітків", """{"seed":true,"segment":"books","level":"root"}""", 3),
        new(31, "Казки та малюки", "children-fairy-tales", 3, "Казки, малюки та bedtime stories", """{"seed":true,"segment":"books","parent":"children"}""", 1),
        new(32, "Для дошкільнят", "children-preschool", 3, "Книги для дошкільного віку", """{"seed":true,"segment":"books","parent":"children"}""", 2),
        new(33, "Для молодших школярів", "children-elementary", 3, "Книги для молодших школярів", """{"seed":true,"segment":"books","parent":"children"}""", 3),
        new(34, "Підліткова література", "children-ya", 3, "Young adult та підліткова проза", """{"seed":true,"segment":"books","parent":"children"}""", 4),
        new(35, "Розмальовки та активності", "children-activity", 3, "Розмальовки, наліпки, активності", """{"seed":true,"segment":"books","parent":"children"}""", 5),
        new(36, "Навчальні книги для дітей", "children-educational", 3, "Розвиваючі та навчальні книги", """{"seed":true,"segment":"books","parent":"children"}""", 6),
        new(37, "Комікси для дітей", "children-comics", 3, "Дитячі комікси та graphic novels", """{"seed":true,"segment":"books","parent":"children"}""", 7),
        new(38, "Книги-м'якотики", "children-soft-books", 3, "М'які книжки для найменших", """{"seed":true,"segment":"books","parent":"children"}""", 8),

        new(4, "Освітня література", "education", null, "Підручники, довідники та навчальні матеріали", """{"seed":true,"segment":"books","level":"root"}""", 4),
        new(41, "Шкільні підручники", "education-textbooks", 4, "Підручники для школи", """{"seed":true,"segment":"books","parent":"education"}""", 1),
        new(42, "Іноземні мови", "education-languages", 4, "Підручники та посібники з іноземних мов", """{"seed":true,"segment":"books","parent":"education"}""", 2),
        new(43, "Математика та точні науки", "education-stem", 4, "Математика, фізика, хімія, біологія", """{"seed":true,"segment":"books","parent":"education"}""", 3),
        new(44, "Гуманітарні дисципліни", "education-humanities", 4, "Історія, література, філософія для навчання", """{"seed":true,"segment":"books","parent":"education"}""", 4),
        new(45, "IT та програмування", "education-it", 4, "Підручники з IT та програмування", """{"seed":true,"segment":"books","parent":"education"}""", 5),
        new(46, "ЗНО/НМТ", "education-exam-prep", 4, "Підготовка до ЗНО та НМТ", """{"seed":true,"segment":"books","parent":"education"}""", 6),
        new(47, "Методична література", "education-methodology", 4, "Методичні посібники для педагогів", """{"seed":true,"segment":"books","parent":"education"}""", 7),
        new(48, "Словники та довідники", "education-dictionaries", 4, "Словники, енциклопедії та довідники", """{"seed":true,"segment":"books","parent":"education"}""", 8),
    ];
}
