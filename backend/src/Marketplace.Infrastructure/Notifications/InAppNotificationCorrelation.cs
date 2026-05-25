using System.Security.Cryptography;
using System.Text;

namespace Marketplace.Infrastructure.Notifications;

internal static class InAppNotificationCorrelation
{
    /// <summary>Stable per-user correlation for idempotent inserts when one job fans out to many users (admins).</summary>
    public static Guid PerUser(Guid jobCorrelationId, Guid userId)
    {
        var input = Encoding.UTF8.GetBytes($"{jobCorrelationId:N}:{userId:N}");
        var hash = SHA256.HashData(input);
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50); // version 5-ish marker for UUID
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }
}
