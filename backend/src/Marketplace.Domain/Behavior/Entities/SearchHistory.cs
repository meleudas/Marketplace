using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Common.ValueObjects;

namespace Marketplace.Domain.Behavior.Entities;

public sealed class SearchHistory : AuditableSoftDeleteAggregateRoot<SearchHistoryId>
{
    private SearchHistory() { }

    public Guid? UserId { get; private set; }
    public string SessionId { get; private set; } = string.Empty;
    public string Query { get; private set; } = string.Empty;
    public JsonBlob Filters { get; private set; } = JsonBlob.Empty;
    public int ResultsCount { get; private set; }
    public JsonBlob ClickedProductIds { get; private set; } = JsonBlob.Empty;
    public DateTime SearchedAt { get; private set; }
    public string? RawPayload { get; private set; }

    public static SearchHistory Reconstitute(
        SearchHistoryId id,
        Guid? userId,
        string sessionId,
        string query,
        JsonBlob filters,
        int resultsCount,
        JsonBlob clickedProductIds,
        DateTime searchedAt,
        string? rawPayload,
        DateTime createdAt,
        DateTime updatedAt,
        bool isDeleted,
        DateTime? deletedAt) =>
        new()
        {
            Id = id,
            UserId = userId,
            SessionId = sessionId,
            Query = query,
            Filters = filters,
            ResultsCount = resultsCount,
            ClickedProductIds = clickedProductIds,
            SearchedAt = searchedAt,
            RawPayload = rawPayload,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt
        };
}
