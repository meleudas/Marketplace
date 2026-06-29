using Marketplace.Application.Common.Ports;
using Marketplace.Application.Finance.Options;
using Marketplace.Application.Finance.Services;
using Marketplace.Application.Orders.Cache;
using Marketplace.Application.Orders.Services;
using Marketplace.Application.Payments.Services;
using Marketplace.Application.Returns.Commands.ApproveReturn;
using Marketplace.Application.Returns.Commands.MarkReturnReceived;
using Marketplace.Application.Returns.Commands.ProcessReturnRefund;
using Marketplace.Domain.Finance.Enums;
using Marketplace.Domain.Returns.Enums;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Common.Fakes;
using Marketplace.Tests.Common.Seed;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Returns;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Returns")]
[Trait("Layer", "IntegrationContainers")]
public sealed class ReturnRefundLedgerPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public ReturnRefundLedgerPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ProcessReturnRefund_Posts_Seller_Ledger_Reversal()
    {
        await _fixture.ApplySeedDataAsync();
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var returnRepo = new ReturnRequestRepository(db);
        var lineRepo = new ReturnLineItemRepository(db);
        var paymentRepo = new PaymentRepository(db);
        var refundRepo = new RefundRepository(db);
        var orderRepo = new OrderRepository(db);
        var ledgerRepo = new SellerLedgerRepository(db);

        var approve = new ApproveReturnCommandHandler(returnRepo, lineRepo, new CompanyMemberRepository(db));
        var received = new MarkReturnReceivedCommandHandler(returnRepo, lineRepo, new CompanyMemberRepository(db));
        var orderFinancialsRepo = new OrderFinancialsRepository(db);
        var commissionRepo = new CompanyCommissionRateRepository(db);
        var financialsWriter = new OrderFinancialsWriter(
            paymentRepo,
            orderRepo,
            commissionRepo,
            orderFinancialsRepo,
            new SellerLedgerService(ledgerRepo),
            Microsoft.Extensions.Options.Options.Create(new SettlementOptions()));

        var refund = new ProcessReturnRefundCommandHandler(
            returnRepo,
            lineRepo,
            paymentRepo,
            new PaymentRefundExecutor(
                paymentRepo,
                refundRepo,
                orderRepo,
                new FakeLiqPayPort(),
                new OrderStatusHistoryWriter(new OrderStatusHistoryRepository(db)),
                new OrderPaymentStateApplier(),
                OrderTestDoubles.CreateCoordinator(new OrderCacheInvalidationService(new NoopCachePort()), new OutboxRepository(db)),
                financialsWriter));

        var approved = await approve.Handle(
            new ApproveReturnCommand(
                SeedTestConstants.ReturnRequestId,
                SeedTestConstants.HomeComfortCompanyId,
                SeedTestConstants.AdminUserId,
                IsActorAdmin: true),
            CancellationToken.None);
        Assert.True(approved.IsSuccess);

        var marked = await received.Handle(
            new MarkReturnReceivedCommand(
                SeedTestConstants.ReturnRequestId,
                SeedTestConstants.HomeComfortCompanyId,
                SeedTestConstants.AdminUserId,
                IsActorAdmin: true),
            CancellationToken.None);
        Assert.True(marked.IsSuccess);
        Assert.Equal(nameof(ReturnRequestStatus.Received), marked.Value!.Status);

        var beforeCount = (await ledgerRepo.ListByCompanyIdAsync(
            Marketplace.Domain.Common.ValueObjects.CompanyId.From(SeedTestConstants.HomeComfortCompanyId),
            CancellationToken.None)).Count;

        var refunded = await refund.Handle(
            new ProcessReturnRefundCommand(SeedTestConstants.ReturnRequestId, SeedTestConstants.AdminUserId, 899m),
            CancellationToken.None);
        Assert.True(refunded.IsSuccess);
        Assert.Equal(nameof(ReturnRequestStatus.Refunded), refunded.Value!.Status);

        var entries = await ledgerRepo.ListByCompanyIdAsync(
            Marketplace.Domain.Common.ValueObjects.CompanyId.From(SeedTestConstants.HomeComfortCompanyId),
            CancellationToken.None);
        Assert.True(entries.Count > beforeCount);
        Assert.Contains(entries, x => x.EntryType == SellerLedgerEntryType.Refund);
    }
}
