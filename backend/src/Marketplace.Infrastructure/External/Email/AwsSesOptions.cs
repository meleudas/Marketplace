namespace Marketplace.Infrastructure.External.Email;

public sealed class AwsSesOptions
{
    public const string SectionName = "AwsSes";

    public bool Enabled { get; set; }
    public string Region { get; set; } = "eu-central-1";
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Marketplace";
    public string? ConfigurationSetName { get; set; }

    public bool IsConfigured() =>
        Enabled &&
        !string.IsNullOrWhiteSpace(Region) &&
        !string.IsNullOrWhiteSpace(FromEmail);
}
