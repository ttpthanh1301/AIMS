using System.Net;
using FluentAssertions;
using Xunit;
namespace AIMS.BackendServer.UnitTests.Integration;

public class PermissionMiddlewareIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PermissionMiddlewareIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ─────────────────────────────────────────────────────────
    // TEST: Admin gọi GET /api/roles → 200 (bypass middleware)
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task Admin_GetRoles_Returns200()
    {
        var client = _factory.CreateClient();
        var token = await AuthHelper.GetTokenAsync(
            client, "admin@test.vn", "Admin@2025!");
        client.SetBearerToken(token);

        var response = await client.GetAsync("/api/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─────────────────────────────────────────────────────────
    // TEST: HR gọi DELETE /api/roles → 403 (không có quyền)
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task HR_DeleteRole_Returns403()
    {
        var client = _factory.CreateClient();
        var token = await AuthHelper.GetTokenAsync(
            client, "hr@test.vn", "Hr@2025!");
        client.SetBearerToken(token);

        var response = await client.DeleteAsync("/api/roles/some-role-id");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─────────────────────────────────────────────────────────
    // TEST: Không có token → 401
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task NoToken_GetRoles_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────────────
    // TEST: HR gọi GET /api/roles → 403 (HR không có quyền xem roles)
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task HR_GetRoles_Returns403()
    {
        var client = _factory.CreateClient();
        var token = await AuthHelper.GetTokenAsync(
            client, "hr@test.vn", "Hr@2025!");
        client.SetBearerToken(token);

        var response = await client.GetAsync("/api/roles");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}