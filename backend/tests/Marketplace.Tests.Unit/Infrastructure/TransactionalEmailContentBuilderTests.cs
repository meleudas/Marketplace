using Marketplace.Infrastructure.External.Email;

namespace Marketplace.Tests;

public sealed class TransactionalEmailContentBuilderTests
{
    [Fact]
    public void BuildConfirmation_Includes_Frontend_Link()
    {
        var message = TransactionalEmailContentBuilder.BuildConfirmation(
            "http://localhost:3000",
            "user@example.com",
            "token-abc");

        Assert.Equal("Підтвердження email", message.Subject);
        Assert.Contains("http://localhost:3000/confirm-email", message.Body, StringComparison.Ordinal);
        Assert.Contains("user%40example.com", message.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPasswordReset_Includes_Token()
    {
        var message = TransactionalEmailContentBuilder.BuildPasswordReset("reset-token");

        Assert.Equal("Скидання пароля", message.Subject);
        Assert.Contains("reset-token", message.Body, StringComparison.Ordinal);
    }
}
