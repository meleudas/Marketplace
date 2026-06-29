using System.Text;
using Marketplace.Application.Common.Options;
using Marketplace.Application.Notifications;
using Marketplace.Application.Shipping.Options;
using Marketplace.Infrastructure.External.Email;
using Marketplace.Infrastructure.External.Payments;
using Marketplace.Infrastructure.External.Shipping;
using Marketplace.Infrastructure.Identity;
using Marketplace.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Marketplace.Infrastructure.Configuration;

/// <summary>
/// Fail-fast validation for production secrets and feature-flag dependencies.
/// Skipped in Development and Testing environments.
/// </summary>
public static class ProductionConfigurationValidator
{
    private static readonly string[] ForbiddenJwtSubstrings =
        ["ChangeThis", "DevSecret", "AtLeast32", "YourSecret", "ReplaceMe"];

    private static readonly HashSet<string> ForbiddenLiqPayValues =
        new(StringComparer.OrdinalIgnoreCase) { "sandbox_test", "sandbox", "test" };

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
            return;

        var errors = CollectErrors(configuration);
        if (errors.Count == 0)
            return;

        throw new InvalidOperationException(
            "Production configuration validation failed:" + Environment.NewLine +
            string.Join(Environment.NewLine, errors.Select(e => $"  - {e}")));
    }

    public static IReadOnlyList<string> CollectErrors(IConfiguration configuration)
    {
        var errors = new List<string>();

        ValidateJwt(configuration, errors);
        ValidateLiqPay(configuration, errors);
        ValidateGoogleAuth(configuration, errors);
        ValidateSendGrid(configuration, errors);
        ValidateWebPush(configuration, errors);
        ValidateStorage(configuration, errors);
        ValidateShipping(configuration, errors);

        return errors;
    }

    private static void ValidateJwt(IConfiguration configuration, List<string> errors)
    {
        var secret = configuration[$"{JwtOptions.SectionName}:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            errors.Add($"{JwtOptions.SectionName}:SecretKey is required in Production.");
            return;
        }

        if (Encoding.UTF8.GetByteCount(secret) < 32)
            errors.Add($"{JwtOptions.SectionName}:SecretKey must be at least 32 bytes for HS256.");

        if (ContainsForbiddenSubstring(secret, ForbiddenJwtSubstrings))
            errors.Add($"{JwtOptions.SectionName}:SecretKey must not use development placeholder values.");
    }

    private static void ValidateLiqPay(IConfiguration configuration, List<string> errors)
    {
        var publicKey = configuration[$"{LiqPayOptions.SectionName}:PublicKey"];
        var privateKey = configuration[$"{LiqPayOptions.SectionName}:PrivateKey"];

        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
        {
            errors.Add($"{LiqPayOptions.SectionName}:PublicKey and PrivateKey are required in Production.");
            return;
        }

        if (ForbiddenLiqPayValues.Contains(publicKey.Trim()) || ForbiddenLiqPayValues.Contains(privateKey.Trim()))
            errors.Add($"{LiqPayOptions.SectionName} keys must not use sandbox/test placeholders in Production.");
    }

    private static void ValidateGoogleAuth(IConfiguration configuration, List<string> errors)
    {
        const string section = "GoogleAuth";
        var clientId = configuration[$"{section}:ClientId"];
        var clientSecret = configuration[$"{section}:ClientSecret"];

        var idSet = !string.IsNullOrWhiteSpace(clientId);
        var secretSet = !string.IsNullOrWhiteSpace(clientSecret);

        if (idSet != secretSet)
            errors.Add($"{section}:ClientId and ClientSecret must both be set or both be empty in Production.");

        if (idSet && ContainsForbiddenSubstring(clientId!, ForbiddenJwtSubstrings))
            errors.Add($"{section}:ClientId must not use development placeholder values.");
    }

    private static void ValidateSendGrid(IConfiguration configuration, List<string> errors)
    {
        var emailEnabled = configuration.GetValue<bool?>($"{AppNotificationOptions.SectionName}:EmailEnabled") ?? true;
        if (!emailEnabled)
            return;

        var apiKey = configuration[$"{SendGridOptions.SectionName}:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            errors.Add($"{SendGridOptions.SectionName}:ApiKey is required when {AppNotificationOptions.SectionName}:EmailEnabled=true.");
        else if (!apiKey.StartsWith("SG.", StringComparison.Ordinal))
            errors.Add($"{SendGridOptions.SectionName}:ApiKey must be a valid SendGrid key (starts with SG.).");
    }

    private static void ValidateWebPush(IConfiguration configuration, List<string> errors)
    {
        var enabled = configuration.GetValue<bool?>($"{WebPushOptions.SectionName}:Enabled") ?? false;
        if (!enabled)
            return;

        var publicKey = configuration[$"{WebPushOptions.SectionName}:PublicKey"];
        var privateKey = configuration[$"{WebPushOptions.SectionName}:PrivateKey"];

        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
            errors.Add($"{WebPushOptions.SectionName}:PublicKey and PrivateKey are required when Enabled=true.");
    }

    private static void ValidateStorage(IConfiguration configuration, List<string> errors)
    {
        var enabled = configuration.GetValue<bool?>($"{StorageOptions.SectionName}:Enabled") ?? false;
        if (!enabled)
            return;

        var accessKey = configuration[$"{StorageOptions.SectionName}:AccessKey"];
        var secretKey = configuration[$"{StorageOptions.SectionName}:SecretKey"];

        if (string.Equals(accessKey, "minioadmin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(secretKey, "minioadmin", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"{StorageOptions.SectionName} must not use default minioadmin credentials in Production.");
        }
    }

    private static void ValidateShipping(IConfiguration configuration, List<string> errors)
    {
        var shippingEnabled = configuration.GetValue<bool?>($"{ShippingOptions.SectionName}:Enabled") ?? false;
        if (!shippingEnabled)
            return;

        var novaPoshtaEnabled = configuration.GetValue<bool?>($"{ShippingOptions.SectionName}:NovaPoshtaEnabled") ?? false;
        if (!novaPoshtaEnabled)
        {
            errors.Add($"{ShippingOptions.SectionName}:Enabled=true requires at least one carrier (set NovaPoshtaEnabled=true).");
            return;
        }

        var apiKey = configuration[$"{NovaPoshtaOptions.SectionName}:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            errors.Add($"{NovaPoshtaOptions.SectionName}:ApiKey is required when {ShippingOptions.SectionName}:NovaPoshtaEnabled=true.");
    }

    private static bool ContainsForbiddenSubstring(string value, IEnumerable<string> forbidden) =>
        forbidden.Any(f => value.Contains(f, StringComparison.OrdinalIgnoreCase));
}
