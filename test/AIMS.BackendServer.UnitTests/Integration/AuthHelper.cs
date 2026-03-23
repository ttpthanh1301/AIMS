using System.Net.Http.Json;
using AIMS.ViewModels.Auth;

namespace AIMS.BackendServer.UnitTests.Integration;

public static class AuthHelper
{
    // Lấy JWT token bằng cách gọi /api/auth/login thật
    public static async Task<string> GetTokenAsync(
        HttpClient client,
        string email,
        string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = password });

        response.EnsureSuccessStatusCode();

        var auth = await response.Content
            .ReadFromJsonAsync<AuthResponse>();

        return auth?.AccessToken
            ?? throw new Exception("Không lấy được token");
    }

    // Helper thêm Bearer token vào HttpClient
    public static void SetBearerToken(
        this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", token);
    }
}