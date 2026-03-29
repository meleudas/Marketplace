namespace Marketplace.Infrastructure.Persistence.Entities;

public class MarketplaceUserRecord
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Role { get; set; }
    public DateTime? Birthday { get; set; }
    public string? Avatar { get; set; }
    public bool IsVerified { get; set; }
    public string? VerificationDocument { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
