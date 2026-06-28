using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Repositories;
using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Repositories;

public sealed class ShippingQuoteRepository : IShippingQuoteRepository
{
    private readonly ApplicationDbContext _context;

    public ShippingQuoteRepository(ApplicationDbContext context) => _context = context;

    public async Task<ShippingQuote?> GetByIdAsync(ShippingQuoteId id, CancellationToken ct = default)
    {
        var row = await _context.ShippingQuotes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return row is null ? null : ToDomain(row);
    }

    public async Task<ShippingQuote> AddAsync(ShippingQuote entity, CancellationToken ct = default)
    {
        var row = ToRecord(entity);
        await _context.ShippingQuotes.AddAsync(row, ct);
        await _context.SaveChangesAsync(ct);
        return ToDomain(row);
    }

    private static ShippingQuote ToDomain(ShippingQuoteRecord row) =>
        ShippingQuote.Reconstitute(
            ShippingQuoteId.From(row.Id),
            row.UserId,
            ShippingMethodId.From(row.ShippingMethodId),
            new Money(row.Amount),
            ContactPerson.Create(row.FirstName, row.LastName, row.Phone),
            Address.Create(row.Street, row.City, row.State, row.PostalCode, row.Country),
            row.ExpiresAtUtc,
            row.CreatedAt,
            row.UpdatedAt,
            row.IsDeleted,
            row.DeletedAt);

    private static ShippingQuoteRecord ToRecord(ShippingQuote entity) =>
        new()
        {
            Id = entity.Id.Value,
            UserId = entity.UserId,
            ShippingMethodId = entity.ShippingMethodId.Value,
            Amount = entity.Amount.Amount,
            FirstName = entity.Contact.FirstName,
            LastName = entity.Contact.LastName,
            Phone = entity.Contact.Phone,
            Street = entity.Address.Street,
            City = entity.Address.City,
            State = entity.Address.State,
            PostalCode = entity.Address.PostalCode,
            Country = entity.Address.Country,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
}
