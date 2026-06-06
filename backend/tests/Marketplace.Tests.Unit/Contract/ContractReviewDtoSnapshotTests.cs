using System.Text.Json;
using Marketplace.Application.Reviews.DTOs;

namespace Marketplace.Tests;

public sealed class ContractReviewDtoSnapshotTests
{
    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "Reviews")]
    public void Contract_ReviewReplyDto_Snapshot_Matches()
    {
        var dto = new ReviewReplyDto(
            1,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "reply body",
            false,
            DateTime.UnixEpoch,
            DateTime.UnixEpoch);

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"Id\":1,\"CompanyId\":\"11111111-1111-1111-1111-111111111111\",\"AuthorUserId\":\"22222222-2222-2222-2222-222222222222\",\"Body\":\"reply body\",\"IsEdited\":false,\"CreatedAt\":\"1970-01-01T00:00:00Z\",\"UpdatedAt\":\"1970-01-01T00:00:00Z\"}";
        Assert.Equal(expected, json);
    }

    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "Reviews")]
    public void Contract_ReviewDto_Snapshot_Matches()
    {
        var dto = new ReviewDto(
            10,
            "product",
            99,
            null,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "buyer",
            5,
            null,
            "title",
            "comment",
            true,
            1,
            DateTime.UnixEpoch,
            DateTime.UnixEpoch,
            new ReviewReplyDto(
                1,
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "reply body",
                false,
                DateTime.UnixEpoch,
                DateTime.UnixEpoch));

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"Id\":10,\"TargetType\":\"product\",\"ProductId\":99,\"CompanyId\":null,\"UserId\":\"33333333-3333-3333-3333-333333333333\",\"UserName\":\"buyer\",\"Rating\":5,\"OverallRating\":null,\"Title\":\"title\",\"Comment\":\"comment\",\"IsVerifiedPurchase\":true,\"Status\":1,\"CreatedAt\":\"1970-01-01T00:00:00Z\",\"UpdatedAt\":\"1970-01-01T00:00:00Z\",\"Reply\":{\"Id\":1,\"CompanyId\":\"11111111-1111-1111-1111-111111111111\",\"AuthorUserId\":\"22222222-2222-2222-2222-222222222222\",\"Body\":\"reply body\",\"IsEdited\":false,\"CreatedAt\":\"1970-01-01T00:00:00Z\",\"UpdatedAt\":\"1970-01-01T00:00:00Z\"}}";
        Assert.Equal(expected, json);
    }
}
