namespace DataGateVPNBot.Services;

public interface IHttpClientFactoryService
{
    HttpClient CreateDashboardClient();
}