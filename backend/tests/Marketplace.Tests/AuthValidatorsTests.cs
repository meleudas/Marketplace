using Marketplace.Application.Auth.Commands.Login;
using Marketplace.Application.Auth.Commands.RefreshToken;
using Marketplace.Application.Auth.Commands.TwoFactor.EnableEmailTwoFactor;
using Marketplace.Application.Auth.Commands.TwoFactor.EnableTelegramTwoFactor;
using Marketplace.Application.Auth.Commands.TwoFactor.LinkTelegramAccount;
using Marketplace.Application.Users.Commands.RequestPasswordReset;
using Marketplace.Application.Users.Commands.ResetPassword;
using Marketplace.Application.Users.Commands.VerifyEmail;

namespace Marketplace.Tests;

public class AuthValidatorsTests
{
    [Fact]
    public void LoginValidator_Allows_Login_Without_2Fa_Code()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("user@example.com", "StrongPass1!", false, null);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("12")]
    [InlineData("abcdef")]
    [InlineData("12345a")]
    [InlineData("1234567")]
    public void LoginValidator_Rejects_Invalid_2Fa_Code(string code)
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("user@example.com", "StrongPass1!", false, code);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TwoFactorCode");
    }

    [Fact]
    public void EnableEmail2FaValidator_Requires_6_Digit_Code()
    {
        var validator = new EnableEmailTwoFactorCommandValidator();
        var command = new EnableEmailTwoFactorCommand(Guid.NewGuid(), "12345");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Code");
    }

    [Fact]
    public void EnableTelegram2FaValidator_Requires_6_Digit_Code()
    {
        var validator = new EnableTelegramTwoFactorCommandValidator();
        var command = new EnableTelegramTwoFactorCommand(Guid.NewGuid(), "abc123");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Code");
    }

    [Fact]
    public void LinkTelegramAccountValidator_Requires_LinkCode_And_ChatId()
    {
        var validator = new LinkTelegramAccountCommandValidator();
        var command = new LinkTelegramAccountCommand("", "");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LinkCode");
        Assert.Contains(result.Errors, e => e.PropertyName == "ChatId");
    }

    [Fact]
    public void RefreshTokenValidator_Allows_Null_RefreshToken()
    {
        var validator = new RefreshTokenCommandValidator();
        var command = new RefreshTokenCommand(null);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ResetPasswordValidator_Requires_Token_And_Email()
    {
        var validator = new ResetPasswordCommandValidator();
        var command = new ResetPasswordCommand("", "", "NewStrongPass1!");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
        Assert.Contains(result.Errors, e => e.PropertyName == "Token");
    }

    [Fact]
    public void VerifyEmailValidator_Requires_Email_And_Token()
    {
        var validator = new VerifyEmailCommandValidator();
        var command = new VerifyEmailCommand("", "");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
        Assert.Contains(result.Errors, e => e.PropertyName == "Token");
    }

    [Fact]
    public void RequestPasswordResetValidator_Requires_Valid_Email()
    {
        var validator = new RequestPasswordResetCommandValidator();
        var command = new RequestPasswordResetCommand("not-an-email");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }
}

