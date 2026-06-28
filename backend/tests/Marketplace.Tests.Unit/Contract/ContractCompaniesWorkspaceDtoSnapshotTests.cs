using System.Text.Json;
using Marketplace.Application.Companies.DTOs;

namespace Marketplace.Tests;

public sealed class ContractCompaniesWorkspaceDtoSnapshotTests
{
    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "CompaniesWorkspace")]
    public void Contract_CompanyDto_Snapshot_Matches()
    {
        var dto = new CompanyDto(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Workspace Company",
            "workspace-company",
            "Description",
            "https://cdn.test/company.png",
            "hello@workspace.test",
            "+380001112233",
            new CompanyAddressDto("Street", "City", "State", "01001", "UA"),
            true,
            DateTime.UnixEpoch,
            "admin-user",
            4.7m,
            17,
            23,
            "{\"mode\":\"b2b\"}",
            DateTime.UnixEpoch,
            DateTime.UnixEpoch,
            false,
            null);

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"Id\":\"11111111-1111-1111-1111-111111111111\",\"Name\":\"Workspace Company\",\"Slug\":\"workspace-company\",\"Description\":\"Description\",\"ImageUrl\":\"https://cdn.test/company.png\",\"ContactEmail\":\"hello@workspace.test\",\"ContactPhone\":\"\\u002B380001112233\",\"Address\":{\"Street\":\"Street\",\"City\":\"City\",\"State\":\"State\",\"PostalCode\":\"01001\",\"Country\":\"UA\"},\"IsApproved\":true,\"ApprovedAt\":\"1970-01-01T00:00:00Z\",\"ApprovedByUserId\":\"admin-user\",\"Rating\":4.7,\"ReviewCount\":17,\"FollowerCount\":23,\"MetaRaw\":\"{\\u0022mode\\u0022:\\u0022b2b\\u0022}\",\"CreatedAt\":\"1970-01-01T00:00:00Z\",\"UpdatedAt\":\"1970-01-01T00:00:00Z\",\"IsDeleted\":false,\"DeletedAt\":null}";
        Assert.Equal(expected, json);
    }

    [Fact]
    [Trait("Suite", "Contract")]
    [Trait("Suite", "CompaniesWorkspace")]
    public void Contract_CompanyMemberDto_Snapshot_Matches()
    {
        var dto = new CompanyMemberDto(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Manager",
            false,
            DateTime.UnixEpoch,
            DateTime.UnixEpoch);

        var json = JsonSerializer.Serialize(dto);
        const string expected = "{\"CompanyId\":\"11111111-1111-1111-1111-111111111111\",\"UserId\":\"22222222-2222-2222-2222-222222222222\",\"Role\":\"Manager\",\"IsOwner\":false,\"CreatedAt\":\"1970-01-01T00:00:00Z\",\"UpdatedAt\":\"1970-01-01T00:00:00Z\"}";
        Assert.Equal(expected, json);
    }
}
