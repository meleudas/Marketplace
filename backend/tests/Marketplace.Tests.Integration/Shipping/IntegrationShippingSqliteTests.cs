using Marketplace.Application.Shipping.Commands.CalculateShippingQuote;
using Marketplace.Application.Shipping.Commands.CreateUserAddress;
using Marketplace.Application.Shipping.Queries.GetShippingMethods;
using Marketplace.Application.Shipping.Queries.ListMyAddresses;
using Marketplace.Application.Shipping.Ports;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Entities;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Tests;

[Trait("Suite", "Shipping")]
public sealed class IntegrationShippingSqliteTests
{
    [Fact]
    public async Task AddressCrud_And_Quote_Works_With_Sqlite()
    {
        await using var db = await CreateSqliteContextAsync();
        var now = DateTime.UtcNow;
        db.ShippingMethods.Add(new ShippingMethodRecord
        {
            Id = 1,
            Name = "Nova Poshta",
            Code = 0,
            Price = 99,
            FreeShippingThreshold = null,
            EstimatedDaysMin = 1,
            EstimatedDaysMax = 3,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var create = new CreateUserAddressCommandHandler(new UserAddressRepository(db));
        var created = await create.Handle(
            new CreateUserAddressCommand(userId, "Shipping", true, "A", "B", "+380", "Street", "Kyiv", "Kyiv", "01001", "UA"),
            CancellationToken.None);
        Assert.True(created.IsSuccess);

        var list = new ListMyAddressesQueryHandler(new UserAddressRepository(db));
        var listed = await list.Handle(new ListMyAddressesQuery(userId), CancellationToken.None);
        Assert.True(listed.IsSuccess);
        Assert.Single(listed.Value!);

        var methods = new GetShippingMethodsQueryHandler(new ShippingMethodRepository(db));
        var methodsResult = await methods.Handle(new GetShippingMethodsQuery(), CancellationToken.None);
        Assert.True(methodsResult.IsSuccess);
        Assert.Single(methodsResult.Value!);

        var quote = new CalculateShippingQuoteCommandHandler(
            new ShippingMethodRepository(db),
            new ShippingQuoteRepository(db),
            new FakeNovaPoshtaPort());
        var quoteResult = await quote.Handle(
            new CalculateShippingQuoteCommand(userId, 1, "A", "B", "+380", "Street", "Kyiv", "Kyiv", "01001", "UA"),
            CancellationToken.None);
        Assert.True(quoteResult.IsSuccess);
        Assert.True(quoteResult.Value!.Amount > 0);
    }

    private static async Task<ApplicationDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private sealed class FakeNovaPoshtaPort : INovaPoshtaPort
    {
        public Task<NovaPoshtaQuoteResult> CalculateQuoteAsync(NovaPoshtaQuoteRequest request, CancellationToken ct = default)
            => Task.FromResult(new NovaPoshtaQuoteResult(true, request.BaseAmount, 1, 3));

        public Task<NovaPoshtaStatusSyncResult> SyncStatusAsync(string trackingNumber, CancellationToken ct = default)
            => Task.FromResult(new NovaPoshtaStatusSyncResult(true, "InTransit", trackingNumber, "{}"));
    }
}
