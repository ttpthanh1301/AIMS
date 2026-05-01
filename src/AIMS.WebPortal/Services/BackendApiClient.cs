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
        try
        {
            var client = CreateClient();
            // Không gửi token cho API jobpositions (public endpoint)
            if (url.Contains("/jobpositions"))
            {
                client = _factory.CreateClient("AimsApi"); // Client không có token
            }

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return default;
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }
        catch
        {
            return default;
        }
    }

    public async Task<(T? Data, string? Message)> GetWithMessageAsync<T>(string url)
    {
        try
        {
            var resp = await CreateClient().GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return (default, await BuildErrorMessageAsync(resp, "Không thể tải dữ liệu."));

            var json = await resp.Content.ReadAsStringAsync();
            return (JsonSerializer.Deserialize<T>(json, JsonOpts), null);
        }
        catch (HttpRequestException ex)
        {
            return (default, $"Không kết nối được API: {url}. {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return (default, $"API phản hồi quá chậm: {url}.");
        }
        catch (Exception ex)
        {
            return (default, $"Lỗi tải dữ liệu từ API: {url}. {ex.Message}");
        }
    }

    // ── POST JSON ──────────────────────────────────────────
    public async Task<T?> PostAsync<T>(string url, object body)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json");
            var resp = await CreateClient().PostAsync(url, content);
            if (!resp.IsSuccessStatusCode) return default;
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }
        catch
        {
            return default;
        }
    }

    public async Task<(bool Success, string? Message)> PostWithMessageAsync(
        string url, object body)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json");
            var resp = await CreateClient().PostAsync(url, content);

            if (resp.IsSuccessStatusCode)
            {
                return (true, await TryExtractMessageAsync(resp));
            }

            return (false, await BuildErrorMessageAsync(resp, "Không thể gửi dữ liệu."));
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Không kết nối được API: {url}. {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return (false, $"API phản hồi quá chậm: {url}.");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi gửi dữ liệu tới API: {url}. {ex.Message}");
        }
    }

    public async Task<(T? Data, string? Message)> PostWithMessageAsync<T>(
        string url, object body)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json");
            var resp = await CreateClient().PostAsync(url, content);

            if (!resp.IsSuccessStatusCode)
                return (default, await BuildErrorMessageAsync(resp, "Không thể gửi dữ liệu."));

            var json = await resp.Content.ReadAsStringAsync();
            var data = string.IsNullOrWhiteSpace(json)
                ? default
                : JsonSerializer.Deserialize<T>(json, JsonOpts);

            return (data, await TryExtractMessageAsync(resp));
        }
        catch (HttpRequestException ex)
        {
            return (default, $"Không kết nối được API: {url}. {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return (default, $"API phản hồi quá chậm: {url}.");
        }
        catch (Exception ex)
        {
            return (default, $"Lỗi gửi dữ liệu tới API: {url}. {ex.Message}");
        }
    }

    // ── POST Form (upload file) ────────────────────────────
    public async Task<T?> PostFormAsync<T>(string url, MultipartFormDataContent form)
    {
        var resp = await CreateClient().PostAsync(url, form);
        if (!resp.IsSuccessStatusCode) return default;
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    public async Task<(T? Data, string? Message)> PostFormWithMessageAsync<T>(
        string url,
        MultipartFormDataContent form)
    {
        try
        {
            var resp = await CreateClient().PostAsync(url, form);
            if (!resp.IsSuccessStatusCode)
                return (default, await BuildErrorMessageAsync(resp, "Không thể upload dữ liệu."));

            var json = await resp.Content.ReadAsStringAsync();
            return (JsonSerializer.Deserialize<T>(json, JsonOpts), await TryExtractMessageAsync(resp));
        }
        catch (HttpRequestException ex)
        {
            return (default, $"Không kết nối được API: {url}. {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return (default, $"API phản hồi quá chậm: {url}.");
        }
        catch (Exception ex)
        {
            return (default, $"Lỗi upload dữ liệu tới API: {url}. {ex.Message}");
        }
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

    public async Task<(bool Success, string? Message)> PutWithMessageAsync(
        string url, object body)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8, "application/json");
            var resp = await CreateClient().PutAsync(url, content);

            if (resp.IsSuccessStatusCode)
                return (true, await TryExtractMessageAsync(resp));

            return (false, await BuildErrorMessageAsync(resp, "Không thể cập nhật dữ liệu."));
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Không kết nối được API: {url}. {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return (false, $"API phản hồi quá chậm: {url}.");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi cập nhật dữ liệu tới API: {url}. {ex.Message}");
        }
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
        try
        {
            var resp = await CreateClient().DeleteAsync(url);

            if (resp.IsSuccessStatusCode)
                return (true, await TryExtractMessageAsync(resp));

            return (false, await BuildErrorMessageAsync(resp, "Không thể xóa."));
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Không kết nối được API: {url}. {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return (false, $"API phản hồi quá chậm: {url}.");
        }
        catch (Exception ex)
        {
            return (false, $"Lỗi xóa dữ liệu trên API: {url}. {ex.Message}");
        }
    }

    private static async Task<string> BuildErrorMessageAsync(HttpResponseMessage resp, string fallback)
    {
        var extracted = await TryExtractMessageAsync(resp);
        if (!string.IsNullOrWhiteSpace(extracted))
            return extracted!;

        var raw = await resp.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var compact = raw.Length > 300 ? raw[..300] + "..." : raw;
            return $"{fallback} ({(int)resp.StatusCode} {resp.StatusCode}): {compact}";
        }

        return $"{fallback} ({(int)resp.StatusCode} {resp.StatusCode})";
    }

    private static async Task<string?> TryExtractMessageAsync(HttpResponseMessage resp)
    {
        try
        {
            var json = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
                return null;

            using var doc = JsonDocument.Parse(json);
            return ExtractMessage(doc.RootElement);
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractMessage(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("message", out var messageElement) &&
                messageElement.ValueKind == JsonValueKind.String)
                return messageElement.GetString();

            if (element.TryGetProperty("errors", out var errorsElement))
                return ExtractMessage(errorsElement);

            foreach (var property in element.EnumerateObject())
            {
                var nested = ExtractMessage(property.Value);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    if (item.TryGetProperty("description", out var description) &&
                        description.ValueKind == JsonValueKind.String)
                        return description.GetString();

                    if (item.TryGetProperty("code", out var code) &&
                        code.ValueKind == JsonValueKind.String)
                        return code.GetString();
                }

                var nested = ExtractMessage(item);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }

        return element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }
}
