namespace Marketplace.Infrastructure.Persistence.Entities;

public class CompanyMemberRecord
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public bool IsOwner { get; set; }
    public short Role { get; set; }
    public string? PermissionsRaw { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
