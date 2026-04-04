namespace Marketplace.Infrastructure;

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>Базовий URL SPA без завершального слеша (наприклад https://app.example.com).</summary>
    public string BaseUrl { get; set; } = "";
}
