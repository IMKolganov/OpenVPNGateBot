using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DataGateVPNBot.Services.Http;

public class HttpRequestService : IHttpRequestService
{
    private readonly IHttpClientFactoryService _httpClientFactoryService;
    private readonly ILogger<HttpRequestService> _logger;
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    public HttpRequestService(IHttpClientFactoryService httpClientFactoryService, ILogger<HttpRequestService> logger)
    {
        _httpClientFactoryService = httpClientFactoryService;
        _logger = logger;
    }

    private HttpClient CreateClient(string? token)
    {
        var client = _httpClientFactoryService.CreateDashboardClient();
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    public async Task<T?> GetAsync<T>(string url, string? token = null, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(token);
        var response = await SendRequestAsync<HttpResponseMessage>(
            () => client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken), cancellationToken);
    
        if (response == null || !response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch data. StatusCode: {StatusCode}", response?.StatusCode);
            return default;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
    
        _logger.LogInformation("Received JSON: {Json}", json);

        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task<T?> PostAsync<T>(string url, object data, string? token = null, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(token);
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        return await SendRequestAsync<T>(() => client.PostAsync(url, content, cancellationToken), cancellationToken);
    }

    public async Task<T?> PutAsync<T>(string url, object data, string? token = null, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(token);
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        return await SendRequestAsync<T>(() => client.PutAsync(url, content, cancellationToken), cancellationToken);
    }

    public async Task<bool> DeleteAsync(string url, string? token = null, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(token);
        return await SendRequestAsync<bool>(() => client.DeleteAsync(url, cancellationToken), cancellationToken);
    }
    
    public async Task<Stream> GetStreamAsync(string url, string? token = null, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(token);
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to download stream. Status code: {response.StatusCode}");
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    private async Task<T?> SendRequestAsync<T>(Func<Task<HttpResponseMessage>> httpRequest, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_defaultTimeout);

            try
            {
                _logger.LogInformation($"Attempt {attempt}: Sending HTTP request...");
                using var response = await httpRequest();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Request failed (Attempt {attempt}): {response.StatusCode} - {response.ReasonPhrase}");
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return default;
                    }

                    await Task.Delay(1000 * attempt, cancellationToken);
                    continue;
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<T>(responseContent);
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                _logger.LogError($"Request timed out (Attempt {attempt})");
                return default;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Network error (Attempt {attempt}): {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error (Attempt {attempt}): {ex.Message}");
            }

            await Task.Delay(1000 * attempt, cancellationToken);
        }

        _logger.LogError("Failed to complete HTTP request after 3 attempts.");
        return default;
    }
}
