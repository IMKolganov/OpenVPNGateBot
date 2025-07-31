namespace DataGateVPNBot.Services.Http;

public interface IHttpClientFactoryService
{
    HttpClient CreateDashboardClient();
}