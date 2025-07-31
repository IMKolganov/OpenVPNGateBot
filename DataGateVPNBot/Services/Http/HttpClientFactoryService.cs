namespace DataGateVPNBot.Services.Http;

public class HttpClientFactoryService(IHttpClientFactory httpClientFactory, ILogger<HttpClientFactoryService> logger)
    : IHttpClientFactoryService
{
    public HttpClient CreateDashboardClient()
    {
        logger.LogInformation("Creating HttpClient for Dashboard API.");
        return httpClientFactory.CreateClient("DashboardClient");
    }
    
    //todo: telegram bot httpclient
}
