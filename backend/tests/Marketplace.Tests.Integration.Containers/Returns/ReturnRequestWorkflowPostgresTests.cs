using Marketplace.Application.Returns.Commands.ApproveReturn;
using Marketplace.Application.Returns.Commands.MarkReturnReceived;
using Marketplace.Domain.Returns.Enums;
using Marketplace.Infrastructure.Persistence.Repositories;
using Marketplace.Tests.Common.Seed;
using Marketplace.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Marketplace.Tests.Returns;

[Collection(nameof(MarketplaceContainersCollection))]
[Trait("Suite", "Returns")]
[Trait("Layer", "IntegrationContainers")]
public sealed class ReturnRequestWorkflowPostgresTests
{
    private readonly MarketplaceContainersFixture _fixture;

    public ReturnRequestWorkflowPostgresTests(MarketplaceContainersFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Approve_And_MarkReceived_Work_On_Seeded_Return()
    {
        await _fixture.ApplySeedDataAsync();
        await using var scope = _fixture.CreateServiceProvider().CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Marketplace.Infrastructure.Persistence.ApplicationDbContext>();

        var returnRepo = new ReturnRequestRepository(db);
        var lineRepo = new ReturnLineItemRepository(db);
        var approve = new ApproveReturnCommandHandler(returnRepo, lineRepo, new CompanyMemberRepository(db));
        var received = new MarkReturnReceivedCommandHandler(returnRepo, lineRepo, new CompanyMemberRepository(db));

        var approved = await approve.Handle(
            new ApproveReturnCommand(
                SeedTestConstants.ReturnRequestId,
                SeedTestConstants.HomeComfortCompanyId,
                SeedTestConstants.AdminUserId,
                IsActorAdmin: true),
            CancellationToken.None);
        Assert.True(approved.IsSuccess);
        Assert.Equal(nameof(ReturnRequestStatus.Approved), approved.Value!.Status);

        var marked = await received.Handle(
            new MarkReturnReceivedCommand(
                SeedTestConstants.ReturnRequestId,
                SeedTestConstants.HomeComfortCompanyId,
                SeedTestConstants.AdminUserId,
                IsActorAdmin: true),
            CancellationToken.None);
        Assert.True(marked.IsSuccess);
        Assert.Equal(nameof(ReturnRequestStatus.Received), marked.Value!.Status);
    }
}
