using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AIMS.WebPortal.Services;

public class BackendApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _http;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public BackendApiClient(
        IHttpClientFactory factory,
        IHttpContextAccessor http)
    {
        _factory = factory;
        _http = http;
    }

    // ── Lấy token từ Claims ────────────────────────────────
    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient("AimsApi");
        var token = _http.HttpContext?.User
            .FindFirst("AccessToken")?.Value;

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    // ── GET ────────────────────────────────────────────────
    public async Task<T?> GetAsync<T>(string url)
    {
        var resp = await CreateClient().GetAsync(url);
        if (!resp.IsSuccessStatusCode) return default;
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    // ── POST JSON ──────────────────────────────────────────
    public async Task<T?> PostAsync<T>(string url, object body)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8, "application/json");
        var resp = await CreateClient().PostAsync(url, content);
        if (!resp.IsSuccessStatusCode) return default;
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    // ── POST Form (upload file) ────────────────────────────
    public async Task<T?> PostFormAsync<T>(string url, MultipartFormDataContent form)
    {
        var resp = await CreateClient().PostAsync(url, form);
        if (!resp.IsSuccessStatusCode) return default;
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    // ── PUT ────────────────────────────────────────────────
    public async Task<bool> PutAsync(string url, object body)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8, "application/json");
        var resp = await CreateClient().PutAsync(url, content);
        return resp.IsSuccessStatusCode;
    }

    // ── DELETE ─────────────────────────────────────────────
    public async Task<bool> DeleteAsync(string url)
    {
        var resp = await CreateClient().DeleteAsync(url);
        return resp.IsSuccessStatusCode;
    }
    // ── DELETE với message lỗi ─────────────────────────────
    public async Task<(bool Success, string? Message)> DeleteWithMessageAsync(
        string url)
    {
        var resp = await CreateClient().DeleteAsync(url);

        if (resp.IsSuccessStatusCode)
            return (true, null);

        try
        {
            var json = await resp.Content.ReadAsStringAsync();
            var error = JsonSerializer.Deserialize<Dictionary<string, string>>(
                json, JsonOpts);
            return (false, error?.GetValueOrDefault("message"));
        }
        catch
        {
            return (false, "Không thể xóa.");
        }
    }
}
