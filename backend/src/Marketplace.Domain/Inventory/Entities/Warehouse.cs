using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Inventory.Entities;

public sealed class Warehouse : AuditableSoftDeleteAggregateRoot<WarehouseId>
{
    private Warehouse() { }

    public CompanyId CompanyId { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public Address Address { get; private set; } = Address.Empty;
    public string TimeZone { get; private set; } = "UTC";
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }

    public static Warehouse Create(
        WarehouseId id,
        CompanyId companyId,
        string name,
        string code,
        Address address,
        string timeZone,
        int priority,
        bool isActive = true)
    {
        ValidateName(name);
        ValidateCode(code);
        ValidateTimeZone(timeZone);
        ValidatePriority(priority);

        var now = DateTime.UtcNow;
        return new Warehouse
        {
            Id = id,
            CompanyId = companyId,
            Name = name.Trim(),
            Code = code.Trim(),
            Address = address,
            TimeZone = timeZone.Trim(),
            Priority = priority,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };
    }

    public static Warehouse Reconstitute(
        WarehouseId id,
        CompanyId companyId,
        string name,
        string code,
        Address address,
        string timeZone,
        int priority,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            Name = name,
            Code = code,
            Address = address,
            TimeZone = timeZone,
            Priority = priority,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };

    public void Update(string name, string code, Address address, string timeZone, int priority)
    {
        EnsureActiveEntity();
        ValidateName(name);
        ValidateCode(code);
        ValidateTimeZone(timeZone);
        ValidatePriority(priority);

        Name = name.Trim();
        Code = code.Trim();
        Address = address;
        TimeZone = timeZone.Trim();
        Priority = priority;
        Touch();
    }

    public void Deactivate()
    {
        EnsureActiveEntity();
        IsActive = false;
        Touch();
    }

    private void EnsureActiveEntity()
    {
        if (IsDeleted)
            throw new DomainException("Cannot modify deleted warehouse");
    }

    private static void ValidateName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Warehouse name cannot be empty");
    }

    private static void ValidateCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Warehouse code cannot be empty");
    }

    private static void ValidateTimeZone(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Warehouse timezone cannot be empty");
    }

    private static void ValidatePriority(int value)
    {
        if (value < 0)
            throw new DomainException("Warehouse priority cannot be negative");
    }
}
