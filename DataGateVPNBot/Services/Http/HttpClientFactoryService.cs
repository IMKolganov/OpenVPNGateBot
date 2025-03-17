namespace DataGateVPNBot.Services;

public class HttpClientFactoryService : IHttpClientFactoryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpClientFactoryService> _logger;

    public HttpClientFactoryService(IHttpClientFactory httpClientFactory, ILogger<HttpClientFactoryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public HttpClient CreateDashboardClient()
    {
        _logger.LogInformation("Creating HttpClient for Dashboard API.");
        return _httpClientFactory.CreateClient("DashboardClient");
    }
    
    //todo: telegram bot httpclient
}
