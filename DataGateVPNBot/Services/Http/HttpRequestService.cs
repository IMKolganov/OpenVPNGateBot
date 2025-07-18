using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DataGateVPNBot.Services.Interfaces;

namespace DataGateVPNBot.Services.Http;

public class HttpRequestService(
    IHttpClientFactoryService httpClientFactoryService,
    IErrorService errorService,
    ILogger<HttpRequestService> logger)
    : IHttpRequestService
{
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    private HttpClient CreateClient(string? token)
    {
        var client = httpClientFactoryService.CreateDashboardClient();
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    public async Task<T?> GetAsync<T>(string url, string? token = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending GET request to {Url}", url);
        var client = CreateClient(token);
        var response = await SendRequestAsync<HttpResponseMessage>(
            () => client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken), url,
            cancellationToken);

        if (response == null || !response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to fetch data from {Url}. StatusCode: {StatusCode}", url, response?.StatusCode);
            return default;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogInformation("Received JSON from {Url}: {Json}", url, json);

        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task<T?> PostAsync<T>(string url, object data, string? token = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending POST request to {Url} with data: {Data}", url, JsonSerializer.Serialize(data));
        var client = CreateClient(token);
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        return await SendRequestAsync<T>(() => client.PostAsync(url, content, cancellationToken), url,
            cancellationToken);
    }

    public async Task<T?> PutAsync<T>(string url, object data, string? token = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending PUT request to {Url} with data: {Data}", url, JsonSerializer.Serialize(data));
        var client = CreateClient(token);
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        return await SendRequestAsync<T>(() => client.PutAsync(url, content, cancellationToken), url,
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(string url, string? token = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending DELETE request to {Url}", url);
        var client = CreateClient(token);
        return await SendRequestAsync<bool>(() => client.DeleteAsync(url, cancellationToken), url, cancellationToken);
    }

    public async Task<Stream> GetStreamAsync(string url, string? token = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending GET request for stream to {Url}", url);
        var client = CreateClient(token);
        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to download stream from {Url}. Status code: {StatusCode}", url,
                response.StatusCode);
            throw new HttpRequestException($"Failed to download stream. Status code: {response.StatusCode}");
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

   private async Task<T?> SendRequestAsync<T>(Func<Task<HttpResponseMessage>> httpRequest, string url,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_defaultTimeout);

            try
            {
                logger.LogInformation("Attempt {Attempt}: Sending HTTP request to {Url}...", attempt, url);

                var response = await httpRequest();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogInformation("Response from {Url} (Attempt {Attempt}): {StatusCode} - {ResponseContent}",
                    url, attempt, response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Request to {Url} failed (Attempt {Attempt}): {StatusCode} - {ReasonPhrase}",
                        url, attempt, response.StatusCode, response.ReasonPhrase);
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        response.Dispose();
                        return default;
                    }

                    response.Dispose();
                    await Task.Delay(1000 * attempt, cancellationToken);
                    continue;
                }

                if (typeof(T) == typeof(HttpResponseMessage))
                {
                    return (T)(object)response;
                }

                var result = JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                response.Dispose();
                return result;
            }
            catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
            {
                // await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
                logger.LogError("Request to {Url} timed out (Attempt {Attempt})", url, attempt);
                return default;
            }
            catch (HttpRequestException ex)
            {
                await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
                logger.LogError("Network error while accessing {Url} (Attempt {Attempt}): {Message}", url, attempt,
                    ex.Message);
            }
            catch (Exception ex)
            {
                await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
                logger.LogError("Unexpected error while accessing {Url} (Attempt {Attempt}): {Message}", url, attempt,
                    ex.Message);
            }
        }
        
        var exception = new HttpRequestException($"Failed to complete HTTP request to {url} after 3 attempts.");
        await errorService.NotifyAdminsAboutExceptionAsync(exception, null, cancellationToken);
        throw exception;
        // return default;
    }
}