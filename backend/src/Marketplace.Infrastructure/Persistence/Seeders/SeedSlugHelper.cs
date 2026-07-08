using System.Text;

namespace Marketplace.Infrastructure.Persistence.Seeders;

internal static class SeedSlugHelper
{
    public static string ToSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var transliterated = TransliterateToLatin(value).ToLowerInvariant();
        var builder = new StringBuilder(transliterated.Length);

        foreach (var ch in transliterated)
        {
            if (char.IsAsciiLetterOrDigit(ch))
                builder.Append(ch);
            else if (ch is ' ' or '-' or '_' or '.' or '/' or '&')
                builder.Append('-');
        }

        return CollapseDashes(builder.ToString()).Trim('-');
    }

    public static string TransliterateToLatin(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length * 2);

        foreach (var ch in value)
            builder.Append(TransliterateChar(ch));

        return builder.ToString();
    }

    private static string TransliterateChar(char ch) => ch switch
    {
        'А' or 'а' => "a",
        'Б' or 'б' => "b",
        'В' or 'в' => "v",
        'Г' or 'г' => "h",
        'Ґ' or 'ґ' => "g",
        'Д' or 'д' => "d",
        'Е' or 'е' => "e",
        'Є' or 'є' => "ie",
        'Ж' or 'ж' => "zh",
        'З' or 'з' => "z",
        'И' or 'и' => "y",
        'І' or 'і' => "i",
        'Ї' or 'ї' => "i",
        'Й' or 'й' => "i",
        'К' or 'к' => "k",
        'Л' or 'л' => "l",
        'М' or 'м' => "m",
        'Н' or 'н' => "n",
        'О' or 'о' => "o",
        'П' or 'п' => "p",
        'Р' or 'р' => "r",
        'С' or 'с' => "s",
        'Т' or 'т' => "t",
        'У' or 'у' => "u",
        'Ф' or 'ф' => "f",
        'Х' or 'х' => "kh",
        'Ц' or 'ц' => "ts",
        'Ч' or 'ч' => "ch",
        'Ш' or 'ш' => "sh",
        'Щ' or 'щ' => "shch",
        'Ь' or 'ь' => string.Empty,
        'Ю' or 'ю' => "iu",
        'Я' or 'я' => "ia",
        'Ъ' or 'ъ' => string.Empty,
        'Ы' or 'ы' => "y",
        'Э' or 'э' => "e",
        'Ё' or 'ё' => "e",
        _ => ch.ToString(),
    };

    private static string CollapseDashes(string value)
    {
        while (value.Contains("--", StringComparison.Ordinal))
            value = value.Replace("--", "-", StringComparison.Ordinal);

        return value;
    }
}
