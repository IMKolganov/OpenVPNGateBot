namespace DataGateVPNBot.Services.Http;

public interface IHttpRequestService
{
    Task<T?> GetAsync<T>(string url, string? token = null, CancellationToken cancellationToken = default);
    Task<T?> PostAsync<T>(string url, object data, string? token = null, CancellationToken cancellationToken = default);
    Task<T?> PutAsync<T>(string url, object data, string? token = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string url, string? token = null, CancellationToken cancellationToken = default);
    Task<Stream> GetStreamAsync(string url, string? token = null,
        CancellationToken cancellationToken = default);
}