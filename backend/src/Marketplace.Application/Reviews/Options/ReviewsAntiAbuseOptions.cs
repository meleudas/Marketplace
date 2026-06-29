namespace Marketplace.Application.Reviews.Options;

public sealed class ReviewsAntiAbuseOptions
{
    public const string SectionName = "ReviewsAntiAbuse";

    public int CreatePerUserProductPerDay { get; set; } = 3;
    public int CreateWindowMinutes { get; set; } = 1440;
}
