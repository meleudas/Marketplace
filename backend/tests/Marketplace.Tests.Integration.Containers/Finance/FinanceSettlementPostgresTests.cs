using Marketplace.Application.Finance.Commands.ApproveSettlementPayout;
using Marketplace.Application.Finance.Queries.GetSellerEarningsSummary;
using Marketplace.Application.Finance.Authorization;
using Marketplace.Infrastructure.External.Finance;
using Marketplace.Infrastructure.Persistence;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Common.Seed;
using Marketplace.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Marketplace.Tests.Finance;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Finance")]
[Trait("Layer", "IntegrationContainers")]
public sealed class FinanceSettlementPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public FinanceSettlementPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Seed_Ledger_Summary_And_Approve_Payout_Work()
    {
        await _fixture.ApplySeedDataAsync();
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var access = new FinanceAccessService(new CompanyMemberRepository(db));
        var summaryHandler = new GetSellerEarningsSummaryQueryHandler(
            access,
            new SellerLedgerRepository(db));

        var summary = await summaryHandler.Handle(
            new GetSellerEarningsSummaryQuery(
                SeedTestConstants.TechStoreCompanyId,
                SeedTestConstants.SellerUserId,
                false,
                null,
                null),
            CancellationToken.None);

        Assert.True(summary.IsSuccess);
        Assert.True(summary.Value!.AvailableAmount > 0);

        var approveHandler = new ApproveSettlementPayoutCommandHandler(
            new SettlementBatchRepository(db),
            new SellerPayoutRepository(db),
            new SellerLedgerRepository(db),
            new CompanyLegalProfileRepository(db),
            new ManualSellerPayoutAdapter(NullLogger<ManualSellerPayoutAdapter>.Instance));

        var approve = await approveHandler.Handle(
            new ApproveSettlementPayoutCommand(SeedTestConstants.SettlementBatchReadyId),
            CancellationToken.None);

        Assert.True(approve.IsSuccess);
        var payout = await db.SellerPayouts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SettlementBatchId == SeedTestConstants.SettlementBatchReadyId);
        Assert.NotNull(payout);
    }
}
