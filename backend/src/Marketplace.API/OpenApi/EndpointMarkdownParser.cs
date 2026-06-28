using System.Text.RegularExpressions;

namespace Marketplace.API.OpenApi;

public static class EndpointMarkdownParser
{
    private static readonly Regex EndpointHeaderRegex = new(
        @"^\s*#{2,4}\s+`(?<method>[A-Z]+)\s+(?<path>/[^`]+)`\s*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex BulletRegex = new(
        @"^-\s+\*\*(?<key>[^*]+):\*\*\s*(?<value>.*)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static IReadOnlyList<EndpointDocEntry> ParseFile(string markdown, string sourceFileName)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return [];

        var matches = EndpointHeaderRegex.Matches(markdown);
        if (matches.Count == 0)
            return [];

        var entries = new List<EndpointDocEntry>(matches.Count);
        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var sectionStart = match.Index + match.Length;
            var sectionEnd = i + 1 < matches.Count ? matches[i + 1].Index : markdown.Length;
            var sectionBody = markdown[sectionStart..sectionEnd];

            var method = EndpointPathNormalizer.NormalizeMethod(match.Groups["method"].Value);
            var rawPath = match.Groups["path"].Value;
            var path = EndpointPathNormalizer.NormalizePath(rawPath);
            var fields = ParseFields(sectionBody);

            entries.Add(BuildEntry(method, path, fields, sourceFileName));
        }

        return entries;
    }

    private static Dictionary<string, string> ParseFields(string sectionBody)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = sectionBody.Replace("\r\n", "\n").Split('\n');
        string? currentKey = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            var bulletMatch = BulletRegex.Match(line);
            if (bulletMatch.Success)
            {
                currentKey = NormalizeFieldKey(bulletMatch.Groups["key"].Value);
                fields[currentKey] = bulletMatch.Groups["value"].Value.Trim();
                continue;
            }

            if (currentKey is null)
                continue;

            var continuation = line.Trim();
            if (continuation.Length == 0)
                continue;

            fields[currentKey] = fields[currentKey].Length == 0
                ? continuation
                : fields[currentKey] + "\n" + continuation;
        }

        return fields;
    }

    private static EndpointDocEntry BuildEntry(
        string method,
        string path,
        Dictionary<string, string> fields,
        string sourceFileName)
    {
        var summary = FirstNonEmpty(fields, "summary", "purpose");

        var purpose = Get(fields, "purpose");
        var whoCanCall = Get(fields, "who-can-call");
        var authorization = Get(fields, "authorization");
        var businessLogic = Get(fields, "business-logic");
        var sideEffects = Get(fields, "side-effects");
        var asyncEffects = Get(fields, "async-effects");
        var frontend = Get(fields, "frontend");
        var request = Get(fields, "request");
        var response = Get(fields, "response");
        var errors = Get(fields, "errors");
        var notes = MergeNotes(fields, sourceFileName);

        var globalRoles = ExtractRoles(whoCanCall, authorization, "глобальні ролі");
        var companyRoles = ExtractRoles(whoCanCall, authorization, "компанійні ролі");
        var frontendStatus = ExtractFrontendStatus(frontend);
        var frontendModule = ExtractFrontendModule(frontend);
        var notificationTemplates = ExtractNotificationTemplates(asyncEffects);
        var idempotencyRequired = DetectIdempotencyRequired(fields, notes);

        return new EndpointDocEntry
        {
            HttpMethod = method,
            Path = path,
            Summary = summary,
            Purpose = purpose,
            WhoCanCall = whoCanCall,
            BusinessLogic = businessLogic,
            SideEffects = sideEffects,
            AsyncEffects = asyncEffects,
            Frontend = frontend,
            Request = request,
            Response = response,
            Errors = errors,
            Notes = notes,
            Authorization = authorization,
            GlobalRoles = globalRoles,
            CompanyRoles = companyRoles,
            FrontendStatus = frontendStatus,
            FrontendModule = frontendModule,
            NotificationTemplates = notificationTemplates,
            IdempotencyRequired = idempotencyRequired
        };
    }

    private static string NormalizeFieldKey(string key)
    {
        var trimmed = key.Trim().TrimEnd(':');
        if (trimmed.StartsWith("Summary", StringComparison.OrdinalIgnoreCase))
            return "summary";

        return trimmed switch
        {
            _ when trimmed.Equals("Призначення", StringComparison.OrdinalIgnoreCase) => "purpose",
            _ when trimmed.Equals("Хто може викликати", StringComparison.OrdinalIgnoreCase) => "who-can-call",
            _ when trimmed.Equals("Бізнес-логіка", StringComparison.OrdinalIgnoreCase) => "business-logic",
            _ when trimmed.Equals("Side effects (синхронно)", StringComparison.OrdinalIgnoreCase) => "side-effects",
            _ when trimmed.Equals("Side effects", StringComparison.OrdinalIgnoreCase) => "side-effects",
            _ when trimmed.StartsWith("Async", StringComparison.OrdinalIgnoreCase) => "async-effects",
            _ when trimmed.Equals("Де на фронті", StringComparison.OrdinalIgnoreCase) => "frontend",
            _ when trimmed.StartsWith("Приймає", StringComparison.OrdinalIgnoreCase) => "request",
            _ when trimmed.Equals("Повертає", StringComparison.OrdinalIgnoreCase) => "response",
            _ when trimmed.Equals("Помилки", StringComparison.OrdinalIgnoreCase) => "errors",
            _ when trimmed.Equals("Авторизація", StringComparison.OrdinalIgnoreCase) => "authorization",
            _ when trimmed.Equals("Idempotency", StringComparison.OrdinalIgnoreCase) => "idempotency",
            _ when trimmed.Equals("Кеш", StringComparison.OrdinalIgnoreCase) => "cache",
            _ when trimmed.Equals("Metrics", StringComparison.OrdinalIgnoreCase) => "metrics",
            _ when trimmed.Equals("Спостережуваність", StringComparison.OrdinalIgnoreCase) => "observability",
            _ when trimmed.Equals("Event consistency", StringComparison.OrdinalIgnoreCase) => "event-consistency",
            _ when trimmed.Equals("Response timeline", StringComparison.OrdinalIgnoreCase) => "response-timeline",
            _ when trimmed.Equals("Фільтри", StringComparison.OrdinalIgnoreCase) => "filters",
            _ when trimmed.Equals("Інваріант", StringComparison.OrdinalIgnoreCase) => "invariant",
            _ when trimmed.Equals("Примітки", StringComparison.OrdinalIgnoreCase) => "notes",
            _ when trimmed.Equals("Body", StringComparison.OrdinalIgnoreCase) => "request",
            _ => trimmed.ToLowerInvariant()
        };
    }

    private static string? Get(IReadOnlyDictionary<string, string> fields, string key) =>
        fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;

    private static string? FirstNonEmpty(IReadOnlyDictionary<string, string> fields, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = Get(fields, key);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static string? MergeNotes(IReadOnlyDictionary<string, string> fields, string sourceFileName)
    {
        var noteKeys = new[]
        {
            "notes",
            "idempotency",
            "cache",
            "metrics",
            "observability",
            "event-consistency",
            "response-timeline",
            "filters",
            "invariant"
        };

        var chunks = new List<string>();
        foreach (var key in noteKeys)
        {
            var value = Get(fields, key);
            if (!string.IsNullOrWhiteSpace(value))
                chunks.Add($"**{key}:** {value}");
        }

        if (chunks.Count == 0)
            return null;

        chunks.Add($"**Джерело:** `{sourceFileName}`");
        return string.Join("\n\n", chunks);
    }

    private static IReadOnlyList<string> ExtractRoles(string? whoCanCall, string? authorization, string roleSectionKeyword)
    {
        var source = whoCanCall ?? authorization;
        if (string.IsNullOrWhiteSpace(source))
            return [];

        var lines = source.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            if (!line.Contains(roleSectionKeyword, StringComparison.OrdinalIgnoreCase))
                continue;

            var value = line.Split(':', 2, StringSplitOptions.TrimEntries).ElementAtOrDefault(1);
            if (string.IsNullOrWhiteSpace(value) || value is "—" or "-")
                return [];

            return SplitRoleTokens(value);
        }

        if (whoCanCall is null && authorization is not null)
        {
            if (authorization.Contains("AllowAnonymous", StringComparison.OrdinalIgnoreCase))
                return ["Anonymous"];

            var roleMatch = Regex.Match(authorization, @"Roles\s*=\s*""([^""]+)""", RegexOptions.CultureInvariant);
            if (roleMatch.Success)
                return SplitRoleTokens(roleMatch.Groups[1].Value);

            if (authorization.Contains("[Authorize]", StringComparison.OrdinalIgnoreCase))
                return ["Authenticated"];
        }

        return [];
    }

    private static IReadOnlyList<string> SplitRoleTokens(string value) =>
        value.Split([',', '/', '|', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static token => token is not "—" and not "-")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string? ExtractFrontendStatus(string? frontend)
    {
        if (string.IsNullOrWhiteSpace(frontend))
            return null;

        var match = Regex.Match(frontend, @"Статус:\s*`(?<status>[a-z_]+)`", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["status"].Value : null;
    }

    private static string? ExtractFrontendModule(string? frontend)
    {
        if (string.IsNullOrWhiteSpace(frontend))
            return null;

        var match = Regex.Match(frontend, @"API-модуль:\s*`(?<module>[^`]+)`", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["module"].Value : null;
    }

    private static IReadOnlyList<string> ExtractNotificationTemplates(string? asyncEffects)
    {
        if (string.IsNullOrWhiteSpace(asyncEffects))
            return [];

        return Regex.Matches(asyncEffects, @"`([A-Za-z][A-Za-z0-9_]*)`", RegexOptions.CultureInvariant)
            .Select(static match => match.Groups[1].Value)
            .Where(static name => name.Contains("Notify", StringComparison.OrdinalIgnoreCase)
                                  || name.StartsWith("User", StringComparison.OrdinalIgnoreCase)
                                  || name.StartsWith("Admin", StringComparison.OrdinalIgnoreCase)
                                  || name.StartsWith("Company", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool DetectIdempotencyRequired(IReadOnlyDictionary<string, string> fields, string? notes)
    {
        if (fields.ContainsKey("idempotency"))
            return true;

        var haystack = notes ?? string.Empty;
        return haystack.Contains("Idempotency-Key", StringComparison.OrdinalIgnoreCase);
    }
}
