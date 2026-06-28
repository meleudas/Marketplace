using System.Security.Cryptography;
using System.Text;

namespace Marketplace.Application.Common;

public static class DomainEventIds
{
    private static readonly Guid Namespace = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

    public static Guid ForPaymentStatus(long paymentId, string status, string source)
        => Create($"{paymentId}|{status.Trim().ToLowerInvariant()}|{source.Trim().ToLowerInvariant()}");

    public static Guid ForOrderEvent(long orderId, string eventType, string correlationKey)
        => Create($"order|{orderId}|{eventType.Trim()}|{correlationKey.Trim()}");

    public static Guid ForInventoryEvent(long reservationId, string eventType)
        => Create($"inventory|{reservationId}|{eventType.Trim()}");

    public static Guid ForBehaviorEvent(long eventId)
        => Create($"behavior|{eventId}");

    private static Guid Create(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(bytes, hash);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash[..16]);
    }
}
