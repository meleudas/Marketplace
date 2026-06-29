using Marketplace.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Marketplace.Tests.Security;

public sealed class ProductionConfigurationValidatorTests
{
  private static readonly Dictionary<string, string?> ValidProductionConfig = new(StringComparer.OrdinalIgnoreCase)
  {
    ["Jwt:SecretKey"] = "ProductionSecretKey_32CharsMinimumLength!!",
    ["LiqPay:PublicKey"] = "prod_public_key_value_12345",
    ["LiqPay:PrivateKey"] = "prod_private_key_value_12345",
    ["GoogleAuth:ClientId"] = "prod-client-id.apps.googleusercontent.com",
    ["GoogleAuth:ClientSecret"] = "prod-client-secret-value",
    ["AppNotifications:EmailEnabled"] = "false",
    ["WebPush:Enabled"] = "false",
    ["Storage:Enabled"] = "false",
    ["Shipping:Enabled"] = "false",
  };

  [Fact]
  [Trait("Suite", "Security")]
  public void Validate_Skips_When_Not_Production()
  {
    var config = BuildConfig(new Dictionary<string, string?> { ["Jwt:SecretKey"] = "short" });
    var env = new FakeHostEnvironment(Environments.Development);

    ProductionConfigurationValidator.Validate(config, env);
  }

  [Fact]
  [Trait("Suite", "Security")]
  public void Validate_Passes_With_Valid_Production_Config()
  {
    var config = BuildConfig(ValidProductionConfig);
    var env = new FakeHostEnvironment(Environments.Production);

    ProductionConfigurationValidator.Validate(config, env);
    Assert.Empty(ProductionConfigurationValidator.CollectErrors(config));
  }

  [Fact]
  [Trait("Suite", "Security")]
  public void CollectErrors_Fails_When_Dev_Jwt_In_Production()
  {
    var values = new Dictionary<string, string?>(ValidProductionConfig, StringComparer.OrdinalIgnoreCase)
    {
      ["Jwt:SecretKey"] = "ChangeThis_DevSecretKey_AtLeast32CharsLong!!"
    };
    var config = BuildConfig(values);

    var errors = ProductionConfigurationValidator.CollectErrors(config);

    Assert.Contains(errors, e => e.Contains("Jwt:SecretKey", StringComparison.Ordinal));
  }

  [Fact]
  [Trait("Suite", "Security")]
  public void CollectErrors_Fails_When_LiqPay_Sandbox_In_Production()
  {
    var values = new Dictionary<string, string?>(ValidProductionConfig, StringComparer.OrdinalIgnoreCase)
    {
      ["LiqPay:PublicKey"] = "sandbox_test",
      ["LiqPay:PrivateKey"] = "sandbox_test"
    };
    var config = BuildConfig(values);

    var errors = ProductionConfigurationValidator.CollectErrors(config);

    Assert.Contains(errors, e => e.Contains("LiqPay", StringComparison.Ordinal));
  }

  [Fact]
  [Trait("Suite", "Security")]
  public void CollectErrors_Fails_When_Storage_Uses_Minioadmin()
  {
    var values = new Dictionary<string, string?>(ValidProductionConfig, StringComparer.OrdinalIgnoreCase)
    {
      ["Storage:Enabled"] = "true",
      ["Storage:AccessKey"] = "minioadmin",
      ["Storage:SecretKey"] = "minioadmin"
    };
    var config = BuildConfig(values);

    var errors = ProductionConfigurationValidator.CollectErrors(config);

    Assert.Contains(errors, e => e.Contains("Storage", StringComparison.Ordinal));
  }

  [Fact]
  [Trait("Suite", "Security")]
  public void CollectErrors_Fails_When_Shipping_Enabled_Without_NovaPoshta_Key()
  {
    var values = new Dictionary<string, string?>(ValidProductionConfig, StringComparer.OrdinalIgnoreCase)
    {
      ["Shipping:Enabled"] = "true",
      ["Shipping:NovaPoshtaEnabled"] = "true",
      ["NovaPoshta:ApiKey"] = ""
    };
    var config = BuildConfig(values);

    var errors = ProductionConfigurationValidator.CollectErrors(config);

    Assert.Contains(errors, e => e.Contains("NovaPoshta:ApiKey", StringComparison.Ordinal));
  }

  [Fact]
  [Trait("Suite", "Security")]
  public void CollectErrors_Fails_When_Email_Enabled_Without_SendGrid()
  {
    var values = new Dictionary<string, string?>(ValidProductionConfig, StringComparer.OrdinalIgnoreCase)
    {
      ["AppNotifications:EmailEnabled"] = "true",
      ["SendGrid:ApiKey"] = ""
    };
    var config = BuildConfig(values);

    var errors = ProductionConfigurationValidator.CollectErrors(config);

    Assert.Contains(errors, e => e.Contains("SendGrid:ApiKey", StringComparison.Ordinal));
  }

  private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
    new ConfigurationBuilder()
      .AddInMemoryCollection(values)
      .Build();

  private sealed class FakeHostEnvironment : IHostEnvironment
  {
    public FakeHostEnvironment(string environmentName) => EnvironmentName = environmentName;

    public string EnvironmentName { get; set; }
    public string ApplicationName { get; set; } = "Marketplace.Tests";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
  }
}
