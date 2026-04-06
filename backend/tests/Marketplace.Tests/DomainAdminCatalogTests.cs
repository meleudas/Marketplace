using Marketplace.Domain.Categories.Entities;
using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.ValueObjects;
using Marketplace.Domain.Companies.Entities;

namespace Marketplace.Tests;

public class DomainAdminCatalogTests
{
    [Fact]
    public void Company_Approve_Sets_Approval_State()
    {
        var company = BuildCompany();

        company.Approve("admin-1");

        Assert.True(company.IsApproved);
        Assert.Equal("admin-1", company.ApprovedByUserId);
        Assert.NotNull(company.ApprovedAt);
    }

    [Fact]
    public void Company_UpdateProfile_Throws_When_Deleted()
    {
        var company = BuildCompany();
        company.SoftDelete();

        var ex = Assert.Throws<DomainException>(() => company.UpdateProfile(
            "New Name",
            "new-name",
            "New Description",
            null,
            "new@company.com",
            "+380000000000",
            Address.Empty,
            null));

        Assert.Contains("deleted company", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Category_Activate_And_Deactivate_Changes_State()
    {
        var category = BuildCategory(isActive: false);
        category.Activate();
        Assert.True(category.IsActive);

        category.Deactivate();
        Assert.False(category.IsActive);
    }

    [Fact]
    public void Category_SetProductCount_Rejects_Negative_Value()
    {
        var category = BuildCategory();

        var ex = Assert.Throws<DomainException>(() => category.SetProductCount(-1));

        Assert.Contains("cannot be negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Company BuildCompany()
    {
        return Company.Create(
            CompanyId.From(Guid.NewGuid()),
            "Company",
            "company",
            "Description",
            null,
            "mail@company.com",
            "+380000000000",
            Address.Create("Street", "City", "State", "00000", "UA"),
            JsonBlob.Empty);
    }

    private static Category BuildCategory(bool isActive = true)
    {
        return Category.Create(
            CategoryId.From(501),
            "Category",
            "category",
            null,
            null,
            "Description",
            JsonBlob.Empty,
            0,
            isActive);
    }
}
