using DataGateVPNBot.Models.Configurations;

namespace DataGateVPNBot.Configurations;

/// <summary>
/// Helper for Dashboard API configuration. Exposed for unit testing.
/// </summary>
public static class DashboardApiConfigurationHelper
{
    public static DashboardApiConfig GetConfig(IConfiguration configuration)
    {
        return new DashboardApiConfig
        {
            Url = Environment.GetEnvironmentVariable("DASHBOARDAPI_URL")
                  ?? configuration["DashboardApi:Url"]
                  ?? string.Empty,

            ClientId = Environment.GetEnvironmentVariable("DASHBOARDAPI_CLIENTID")
                       ?? configuration["DashboardApi:ClientId"]
                       ?? string.Empty,

            ClientSecret = Environment.GetEnvironmentVariable("DASHBOARDAPI_CLIENTSECRET")
                           ?? configuration["DashboardApi:ClientSecret"]
                           ?? string.Empty
        };
    }
}
