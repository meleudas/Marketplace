using Marketplace.Application.Categories.Commands.CreateCategory;
using Marketplace.Application.Categories.Commands.UpdateCategory;
using Marketplace.Application.Companies.Commands.ApproveCompany;
using Marketplace.Application.Companies.Commands.CreateCompany;
using Marketplace.Application.Companies.DTOs;

namespace Marketplace.Tests;

public class ApplicationAdminCatalogValidatorsTests
{
    [Fact]
    public void CreateCompanyValidator_Rejects_Empty_Required_Fields()
    {
        var validator = new CreateCompanyCommandValidator();
        var command = new CreateCompanyCommand(
            "",
            "",
            "",
            null,
            "",
            "",
            new CompanyAddressDto("", "", "", "", ""),
            new CompanyLegalProfileDto("", "llc", null, null, null, false, null),
            null);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Slug");
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }

    [Fact]
    public void ApproveCompanyValidator_Requires_Admin_UserId()
    {
        var validator = new ApproveCompanyCommandValidator();
        var command = new ApproveCompanyCommand(Guid.NewGuid(), Guid.Empty);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "AdminUserId");
    }

    [Fact]
    public void CreateCategoryValidator_Rejects_Negative_SortOrder()
    {
        var validator = new CreateCategoryCommandValidator();
        var command = new CreateCategoryCommand("Cat", "cat", null, null, null, null, -1, true);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SortOrder");
    }

    [Fact]
    public void UpdateCategoryValidator_Allows_Valid_Request()
    {
        var validator = new UpdateCategoryCommandValidator();
        var command = new UpdateCategoryCommand(10, "Cat", "cat", null, null, null, null, 1);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
