using Marketplace.Domain.Categories.Repositories;
using Marketplace.Domain.Companies.Repositories;
using Marketplace.Infrastructure;
using Marketplace.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Tests;

public class InfrastructureAdminCatalogRegistrationTests
{
    [Fact]
    public void AddInfrastructure_Registers_Company_And_Category_Repositories()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructure(BuildConfiguration());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var companyRepository = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();
        var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        Assert.IsType<CompanyRepository>(companyRepository);
        Assert.IsType<CategoryRepository>(categoryRepository);
    }

    private static IConfiguration BuildConfiguration()
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = "Host=localhost;Port=5432;Database=marketplace;Username=postgres;Password=postgres",
            ["Jwt:SecretKey"] = "ChangeThis_DevSecretKey_AtLeast32CharsLong!!",
            ["Jwt:Issuer"] = "Marketplace",
            ["Jwt:Audience"] = "Marketplace",
            ["Jwt:AccessTokenMinutes"] = "10",
            ["Jwt:RefreshTokenDays"] = "30"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
