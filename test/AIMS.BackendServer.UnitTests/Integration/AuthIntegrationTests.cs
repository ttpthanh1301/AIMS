using System.Net;
using System.Net.Http.Json;
using AIMS.ViewModels.Auth;
using FluentAssertions;
using Xunit;

namespace AIMS.BackendServer.UnitTests.Integration;

public class AuthIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─────────────────────────────────────────────────────────
    // TEST: Login thành công → 200 + token
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest
            {
                Email = "admin@test.vn",
                Password = "Admin@2025!",
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await response.Content
            .ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrEmpty();
        auth.Roles.Should().Contain("Admin");
    }

    // ─────────────────────────────────────────────────────────
    // TEST: Login sai password → 401
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest
            {
                Email = "admin@test.vn",
                Password = "WrongPassword!",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────────────
    // TEST: Gọi /api/auth/me không có token → 401
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─────────────────────────────────────────────────────────
    // TEST: Gọi /api/auth/me có token → 200 + user info
    // ─────────────────────────────────────────────────────────
    [Fact]
    public async Task GetMe_WithValidToken_Returns200()
    {
        // Login lấy token thật
        var token = await AuthHelper.GetTokenAsync(
            _client, "admin@test.vn", "Admin@2025!");

        _client.SetBearerToken(token);

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}