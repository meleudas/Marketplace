using System.Reflection;
using Marketplace.Domain.Common.Models;
using NetArchTest.Rules;
using Xunit;

namespace Marketplace.Tests.Architecture;

public sealed class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Marketplace.API.Extensions.ServiceCollectionExtensions).Assembly;

    [Fact]
    [Trait("Suite", "Architecture")]
    public void Domain_should_not_reference_outer_layers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Marketplace.Application",
                "Marketplace.Infrastructure",
                "Marketplace.API")
            .GetResult();

        AssertArchitectureRule(result);
    }

    [Fact]
    [Trait("Suite", "Architecture")]
    public void Application_should_not_reference_Infrastructure_or_API()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Marketplace.Infrastructure",
                "Marketplace.API")
            .GetResult();

        AssertArchitectureRule(result);
    }

    [Fact]
    [Trait("Suite", "Architecture")]
    public void API_controllers_should_not_reference_Infrastructure_layers()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("Marketplace.API.Controllers")
            .ShouldNot()
            .HaveDependencyOnAny(
                "Marketplace.Infrastructure.Persistence",
                "Marketplace.Infrastructure.Jobs",
                "Marketplace.Infrastructure.Identity",
                "Marketplace.Infrastructure.Caching",
                "Marketplace.Infrastructure.External")
            .GetResult();

        AssertArchitectureRule(result);
    }

    [Fact]
    [Trait("Suite", "Architecture")]
    public void Application_handlers_should_not_use_EF_or_Hangfire_directly()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "Hangfire")
            .GetResult();

        AssertArchitectureRule(result);
    }

    [Fact]
    [Trait("Suite", "Architecture")]
    public void Domain_should_not_use_EF_DataAnnotations_schema()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("System.ComponentModel.DataAnnotations.Schema")
            .GetResult();

        AssertArchitectureRule(result);
    }

    [Fact]
    [Trait("Suite", "Architecture")]
    public void Command_handlers_should_reside_in_Commands_namespace()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Should()
            .ResideInNamespaceContaining(".Commands.")
            .GetResult();

        AssertArchitectureRule(result);
    }

    [Fact]
    [Trait("Suite", "Architecture")]
    public void Query_handlers_should_reside_in_Queries_namespace()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .Should()
            .ResideInNamespaceContaining(".Queries.")
            .GetResult();

        AssertArchitectureRule(result);
    }

    private static void AssertArchitectureRule(TestResult result)
    {
        var failing = result.FailingTypes?.Select(t => t.FullName ?? t.Name).ToArray() ?? [];
        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, failing));
    }
}
