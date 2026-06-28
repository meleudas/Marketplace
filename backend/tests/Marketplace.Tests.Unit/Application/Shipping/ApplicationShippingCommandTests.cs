using Marketplace.Application.Shipping.Commands.CalculateShippingQuote;
using Marketplace.Application.Shipping.Commands.CreateUserAddress;
using Marketplace.Application.Shipping.Ports;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Shipping.Entities;
using Marketplace.Domain.Shipping.Enums;
using Marketplace.Domain.Shipping.Repositories;

namespace Marketplace.Tests;

[Trait("Suite", "Shipping")]
public sealed class ApplicationShippingCommandTests
{
    [Fact]
    public async Task CreateUserAddress_When_IsDefault_Clears_Previous_Default()
    {
        var userId = Guid.NewGuid();
        var repo = new InMemoryUserAddressRepository();
        await repo.AddAsync(
            UserAddress.Reconstitute(
                UserAddressId.From(0),
                userId,
                UserAddressType.Shipping,
                true,
                ContactPerson.Create("Old", "Default", "+380"),
                Address.Create("Street", "Kyiv", "Kyiv", "01001", "UA"),
                DateTime.UtcNow,
                DateTime.UtcNow,
                false,
                null));
        var handler = new CreateUserAddressCommandHandler(repo);

        var result = await handler.Handle(
            new CreateUserAddressCommand(
                userId,
                "Shipping",
                true,
                "New",
                "Default",
                "+380123",
                "Street 2",
                "Kyiv",
                "Kyiv",
                "01001",
                "UA"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var all = await repo.ListByUserAsync(userId);
        var defaultAddress = Assert.Single(all, x => x.IsDefault);
        Assert.Equal("New", defaultAddress.FirstName);
    }

    [Fact]
    public async Task CalculateShippingQuote_Returns_Quote_Dto()
    {
        var handler = new CalculateShippingQuoteCommandHandler(
            new FakeShippingMethodRepository(),
            new InMemoryShippingQuoteRepository(),
            new FakeNovaPoshtaPort());

        var result = await handler.Handle(
            new CalculateShippingQuoteCommand(
                Guid.NewGuid(),
                1,
                "A",
                "B",
                "+380",
                "Street",
                "Kyiv",
                "Kyiv",
                "01001",
                "UA"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(99m, result.Value!.Amount);
        Assert.Equal(1, result.Value.ShippingMethodId);
    }

    private sealed class InMemoryUserAddressRepository : IUserAddressRepository
    {
        private readonly Dictionary<long, UserAddress> _items = new();
        private long _nextId = 1;

        public Task<UserAddress?> GetByIdAsync(UserAddressId id, CancellationToken ct = default)
            => Task.FromResult(_items.GetValueOrDefault(id.Value));

        public Task<IReadOnlyList<UserAddress>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<UserAddress>>(_items.Values.Where(x => x.UserId == userId && !x.IsDeleted).ToList());

        public Task<UserAddress> AddAsync(UserAddress entity, CancellationToken ct = default)
        {
            var id = entity.Id.Value <= 0 ? _nextId++ : entity.Id.Value;
            var saved = UserAddress.Reconstitute(
                UserAddressId.From(id),
                entity.UserId,
                entity.Type,
                entity.IsDefault,
                entity.Contact,
                entity.Address,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.IsDeleted,
                entity.DeletedAt);
            _items[id] = saved;
            return Task.FromResult(saved);
        }

        public Task UpdateAsync(UserAddress entity, CancellationToken ct = default)
        {
            _items[entity.Id.Value] = entity;
            return Task.CompletedTask;
        }

        public Task SoftDeleteAsync(UserAddressId id, DateTime deletedAtUtc, CancellationToken ct = default)
        {
            if (_items.TryGetValue(id.Value, out var existing))
            {
                _items[id.Value] = UserAddress.Reconstitute(
                    existing.Id,
                    existing.UserId,
                    existing.Type,
                    existing.IsDefault,
                    existing.Contact,
                    existing.Address,
                    existing.CreatedAt,
                    deletedAtUtc,
                    true,
                    deletedAtUtc);
            }

            return Task.CompletedTask;
        }

        public Task ClearDefaultAsync(Guid userId, CancellationToken ct = default)
        {
            foreach (var existing in _items.Values.Where(x => x.UserId == userId && x.IsDefault).ToList())
            {
                _items[existing.Id.Value] = UserAddress.Reconstitute(
                    existing.Id,
                    existing.UserId,
                    existing.Type,
                    false,
                    existing.Contact,
                    existing.Address,
                    existing.CreatedAt,
                    DateTime.UtcNow,
                    existing.IsDeleted,
                    existing.DeletedAt);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeShippingMethodRepository : IShippingMethodRepository
    {
        public Task<ShippingMethod?> GetByIdAsync(ShippingMethodId id, CancellationToken ct = default)
            => Task.FromResult<ShippingMethod?>(
                ShippingMethod.Reconstitute(
                    id,
                    "Nova Poshta",
                    ShippingCarrierCode.NovaPoshta,
                    new Money(99),
                    null,
                    1,
                    3,
                    true,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    false,
                    null));

        public Task<IReadOnlyList<ShippingMethod>> ListActiveAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ShippingMethod>>([]);
    }

    private sealed class InMemoryShippingQuoteRepository : IShippingQuoteRepository
    {
        private long _nextId = 1;

        public Task<ShippingQuote?> GetByIdAsync(ShippingQuoteId id, CancellationToken ct = default)
            => Task.FromResult<ShippingQuote?>(null);

        public Task<ShippingQuote> AddAsync(ShippingQuote entity, CancellationToken ct = default)
        {
            var saved = ShippingQuote.Reconstitute(
                ShippingQuoteId.From(_nextId++),
                entity.UserId,
                entity.ShippingMethodId,
                entity.Amount,
                entity.Contact,
                entity.Address,
                entity.ExpiresAtUtc,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.IsDeleted,
                entity.DeletedAt);
            return Task.FromResult(saved);
        }
    }

    private sealed class FakeNovaPoshtaPort : INovaPoshtaPort
    {
        public Task<NovaPoshtaQuoteResult> CalculateQuoteAsync(NovaPoshtaQuoteRequest request, CancellationToken ct = default)
            => Task.FromResult(new NovaPoshtaQuoteResult(true, request.BaseAmount, 1, 3));

        public Task<NovaPoshtaStatusSyncResult> SyncStatusAsync(string trackingNumber, CancellationToken ct = default)
            => Task.FromResult(new NovaPoshtaStatusSyncResult(true, "InTransit", trackingNumber, "{}"));
    }
}
