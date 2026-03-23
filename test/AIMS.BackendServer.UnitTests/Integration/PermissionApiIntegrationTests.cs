using System.Net;
using System.Net.Http.Json;
using AIMS.ViewModels.Systems;
using FluentAssertions;
using Xunit;

namespace AIMS.BackendServer.UnitTests.Integration;

public class PermissionApiIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PermissionApiIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ─────────────────────────────────────────────────────────
    // TEST: GET /api/permissions/{roleId} → 200 + list
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task GetByRole_Admin_ReturnsPermissions()
    {
        var client = _factory.CreateClient();
        var token = await AuthHelper.GetTokenAsync(
            client, "admin@test.vn", "Admin@2025!");
        client.SetBearerToken(token);

        var response = await client.GetAsync("/api/permissions/admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var perms = await response.Content
            .ReadFromJsonAsync<List<PermissionVm>>();
        perms.Should().NotBeNull();
        perms!.Count.Should().BeGreaterThan(0);
    }

    // ─────────────────────────────────────────────────────────
    // TEST: PUT /api/permissions → 200 + cache bị invalidate
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdatePermissions_Admin_Returns200()
    {
        var client = _factory.CreateClient();
        var token = await AuthHelper.GetTokenAsync(
            client, "admin@test.vn", "Admin@2025!");
        client.SetBearerToken(token);

        var hrRoleId = "hr"; // Id của HR role trong seed data

        var request = new UpdatePermissionRequest
        {
            RoleId = hrRoleId,
            Permissions = new List<PermissionVm>
            {
                new PermissionVm
                {
                    FunctionId = "RECRUITMENT_JD",
                    RoleId     = hrRoleId,
                    CommandId  = "VIEW",
                },
                new PermissionVm
                {
                    FunctionId = "RECRUITMENT_JD",
                    RoleId     = hrRoleId,
                    CommandId  = "CREATE",
                },
            }
        };

        var response = await client.PutAsJsonAsync(
            "/api/permissions", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify: GET lại permissions phải có 2 items
        var getResponse = await client.GetAsync(
            $"/api/permissions/{hrRoleId}");
        var perms = await getResponse.Content
            .ReadFromJsonAsync<List<PermissionVm>>();

        perms.Should().NotBeNull();
        perms!.Count.Should().Be(2);
    }

    // ─────────────────────────────────────────────────────────
    // TEST: GET /api/permissions/user/{userId}
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task GetByUser_ReturnsUserPermissions()
    {
        var client = _factory.CreateClient();
        var token = await AuthHelper.GetTokenAsync(
            client, "admin@test.vn", "Admin@2025!");
        client.SetBearerToken(token);

        var response = await client.GetAsync(
            "/api/permissions/user/admin-test-001");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var perms = await response.Content
            .ReadFromJsonAsync<List<PermissionVm>>();
        perms.Should().NotBeNull();
        perms!.Should().AllSatisfy(p =>
        {
            p.FunctionId.Should().NotBeNullOrEmpty();
            p.CommandId.Should().NotBeNullOrEmpty();
        });
    }
}