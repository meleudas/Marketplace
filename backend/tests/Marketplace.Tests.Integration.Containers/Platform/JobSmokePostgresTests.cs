using Marketplace.Infrastructure.Jobs;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Common.Fakes;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Platform;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Platform")]
[Trait("Layer", "IntegrationContainers")]
public sealed class JobSmokePostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public JobSmokePostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Inventory_And_Payment_Jobs_Run_Without_Error_On_Empty_Database()
    {
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var inventoryJobs = sp.GetRequiredService<InventoryJobs>();
        await inventoryJobs.ExpireReservationsAsync(CancellationToken.None);

        var paymentJobs = sp.GetRequiredService<PaymentJobs>();
        await paymentJobs.SyncPendingPaymentsAsync(CancellationToken.None);
    }
}
