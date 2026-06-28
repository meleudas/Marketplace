using System.Security.Cryptography;
using System.Text;

namespace Marketplace.Application.Notifications;

/// <summary>Stable GUIDs for idempotent app notifications (Hangfire retries, webhook replays).</summary>
public static class AppNotificationCorrelationIds
{
    public static Guid Deterministic(string key)
    {
        var input = Encoding.UTF8.GetBytes(key);
        var hash = SHA256.HashData(input);
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }

    public static Guid PaymentBuyerNotify(string transactionId, string paymentStatus) =>
        Deterministic($"app-payment-buyer|{transactionId}|{paymentStatus}");

    /// <summary>Daily bucket per user+product to align with in-app dedup and rate limit.</summary>
    public static Guid CartRestockNotify(Guid userId, long productId, string utcDateBucket) =>
        Deterministic($"cart-restock|{userId:N}|{productId}|{utcDateBucket}");

    public static Guid ProductPendingReviewQueue(long productId) =>
        Deterministic($"app-notify|product-pending-review|{productId}");

    public static Guid ProductApprovedForUser(long productId, Guid userId) =>
        Deterministic($"app-notify|product-approved|{productId}|{userId:N}");

    public static Guid ProductRejectedForUser(long productId, Guid userId) =>
        Deterministic($"app-notify|product-rejected|{productId}|{userId:N}");
}
