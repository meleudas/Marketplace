namespace Marketplace.API.OpenApi;

public sealed class EndpointDocEntry
{
    public required string HttpMethod { get; init; }
    public required string Path { get; init; }
    public string? Summary { get; init; }
    public string? Purpose { get; init; }
    public string? WhoCanCall { get; init; }
    public string? BusinessLogic { get; init; }
    public string? SideEffects { get; init; }
    public string? AsyncEffects { get; init; }
    public string? Frontend { get; init; }
    public string? Request { get; init; }
    public string? Response { get; init; }
    public string? Errors { get; init; }
    public string? Notes { get; init; }
    public string? Authorization { get; init; }
    public IReadOnlyList<string> GlobalRoles { get; init; } = [];
    public IReadOnlyList<string> CompanyRoles { get; init; } = [];
    public string? FrontendStatus { get; init; }
    public string? FrontendModule { get; init; }
    public IReadOnlyList<string> NotificationTemplates { get; init; } = [];
    public bool IdempotencyRequired { get; init; }

    public string BuildDescriptionMarkdown()
    {
        var sections = new List<(string Title, string? Body)>
        {
            ("Призначення", Purpose),
            ("Хто може викликати", WhoCanCall ?? FormatAuthorizationFallback()),
            ("Бізнес-логіка", BusinessLogic),
            ("Side effects (синхронно)", SideEffects),
            ("Async / «магія»", AsyncEffects),
            ("Де на фронті", Frontend),
            ("Приймає", Request),
            ("Повертає", Response),
            ("Помилки", Errors),
            ("Примітки", Notes)
        };

        var lines = new List<string>();
        foreach (var (title, body) in sections)
        {
            if (string.IsNullOrWhiteSpace(body))
                continue;

            lines.Add($"### {title}");
            lines.Add(body.Trim());
            lines.Add(string.Empty);
        }

        return lines.Count == 0
            ? Purpose ?? "Документацію для цього endpoint-а ще не додано."
            : string.Join('\n', lines).TrimEnd();
    }

    private string? FormatAuthorizationFallback()
    {
        if (string.IsNullOrWhiteSpace(Authorization))
            return null;

        return Authorization.Trim();
    }
}
