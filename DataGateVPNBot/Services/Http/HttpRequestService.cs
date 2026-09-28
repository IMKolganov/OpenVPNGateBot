using System.Net;
using System.Net.Http.Headers;
using System.Text;
using DataGateVPNBot.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DataGateVPNBot.Services.Http;

public class HttpRequestService(
    IHttpClientFactoryService httpClientFactoryService,
    IServiceProvider serviceProvider,
    ILogger<HttpRequestService> logger)
    : IHttpRequestService
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };

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
            response?.Dispose();
            return default;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogInformation("Received JSON from {Url}: {Json}", url, json);

        response.Dispose();
        return JsonConvert.DeserializeObject<T>(json, JsonSettings);
    }

    public async Task<T?> PostAsync<T>(string url, object data, string? token = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending POST request to {Url} with data: {Data}", url, JsonConvert.SerializeObject(data, JsonSettings));
        var client = CreateClient(token);
        var content = new StringContent(JsonConvert.SerializeObject(data, JsonSettings), Encoding.UTF8, "application/json");
        return await SendRequestAsync<T>(() => client.PostAsync(url, content, cancellationToken), url,
            cancellationToken);
    }

    public async Task<T?> PutAsync<T>(string url, object data, string? token = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending PUT request to {Url} with data: {Data}", url, JsonConvert.SerializeObject(data, JsonSettings));
        var client = CreateClient(token);
        var content = new StringContent(JsonConvert.SerializeObject(data, JsonSettings), Encoding.UTF8, "application/json");
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
        using var scope = serviceProvider.CreateScope();
        var errorService = scope.ServiceProvider.GetRequiredService<IErrorService>();
        var errorDetails = new StringBuilder();
        errorDetails.AppendLine($"Failed to complete HTTP request to {url} after 3 attempts.");

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

                    errorDetails.AppendLine($"Attempt {attempt}: {response.StatusCode} - {response.ReasonPhrase}");
                    errorDetails.AppendLine($"Response body: {responseContent}");

                    if (IsNonRetriableClientError(response.StatusCode))
                    {
                        if (typeof(T) == typeof(HttpResponseMessage))
                            return (T)(object)response;

                        T? errorResult = default;
                        try
                        {
                            errorResult = JsonConvert.DeserializeObject<T>(responseContent, JsonSettings);
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug(ex,
                                "Could not deserialize client-error body from {Url} as {Type}",
                                url, typeof(T).Name);
                        }

                        response.Dispose();
                        return errorResult;
                    }

                    if (IsAuthTokenEndpoint(url))
                    {
                        response.Dispose();
                        throw new HttpRequestException(
                            $"Dashboard auth token request failed: {response.StatusCode}. Body: {responseContent}");
                    }

                    response.Dispose();
                    await Task.Delay(1000 * attempt, cancellationToken);
                    continue;
                }

                if (typeof(T) == typeof(HttpResponseMessage))
                {
                    return (T)(object)response;
                }

                var result = JsonConvert.DeserializeObject<T>(responseContent, JsonSettings);

                response.Dispose();
                return result;
            }
            catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
            {
                logger.LogError("Request to {Url} timed out (Attempt {Attempt}) Error: {Error}",
                    url, attempt, ex.Message);
                errorDetails.AppendLine($"Attempt {attempt}: Timeout after {_defaultTimeout.TotalSeconds} seconds.");
                return default;
            }
            catch (HttpRequestException ex)
            {
                await MaybeNotifyAdminsAsync(errorService, url, ex, cancellationToken);
                logger.LogError(ex, "Network error while accessing {Url} (Attempt {Attempt}): {Message}", url, attempt,
                    ex.Message);
                errorDetails.AppendLine($"Attempt {attempt}: HttpRequestException - {ex.Message}");
            }
            catch (Exception ex)
            {
                await MaybeNotifyAdminsAsync(errorService, url, ex, cancellationToken);
                logger.LogError(ex, "Unexpected error while accessing {Url} (Attempt {Attempt}): {Message}", url, attempt,
                    ex.Message);
                errorDetails.AppendLine($"Attempt {attempt}: Exception - {ex.GetType().Name}: {ex.Message}");
            }
        }

        var exception = new HttpRequestException(errorDetails.ToString());
        await MaybeNotifyAdminsAsync(errorService, url, exception, cancellationToken);
        throw exception;
    }

    private static Task MaybeNotifyAdminsAsync(
        IErrorService errorService,
        string url,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (IsAuthTokenEndpoint(url))
            return Task.CompletedTask;

        return errorService.NotifyAdminsAboutExceptionAsync(exception, null, cancellationToken);
    }

    private static bool IsAuthTokenEndpoint(string url) =>
        url.Contains("api/auth/token", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 4xx except timeouts: business/validation and rate-limit errors that must not be retried.
    /// </summary>
    private static bool IsNonRetriableClientError(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        if (code is < 400 or >= 500)
            return false;

        return statusCode is not HttpStatusCode.RequestTimeout;
    }
}
