using System.Security.Cryptography;
using System.Text;

namespace Marketplace.Infrastructure.Identity.Security;

internal static class TokenHasher
{
    public static string Sha256Hex(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
