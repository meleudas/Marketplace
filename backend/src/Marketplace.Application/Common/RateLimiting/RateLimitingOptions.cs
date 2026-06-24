namespace Marketplace.Application.Common.RateLimiting;

public enum RateLimitPartitionKind
{
    Ip,
    UserId,
    EmailHash
}

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
    public RateLimitPartitionKind PartitionBy { get; set; } = RateLimitPartitionKind.Ip;
}

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;
    public RateLimitPolicyOptions Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
    public RateLimitPolicyOptions AuthEmail { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60, PartitionBy = RateLimitPartitionKind.EmailHash };
    public RateLimitPolicyOptions Checkout { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60, PartitionBy = RateLimitPartitionKind.UserId };
    public RateLimitPolicyOptions Review { get; set; } = new() { PermitLimit = 10, WindowSeconds = 3600, PartitionBy = RateLimitPartitionKind.UserId };
    public RateLimitPolicyOptions PaymentWebhook { get; set; } = new() { PermitLimit = 120, WindowSeconds = 60 };
    public RateLimitPolicyOptions PaymentAdmin { get; set; } = new() { PermitLimit = 30, WindowSeconds = 60, PartitionBy = RateLimitPartitionKind.UserId };
}
