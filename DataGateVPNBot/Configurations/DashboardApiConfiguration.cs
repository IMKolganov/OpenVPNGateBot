using System.Net.Http.Headers;
using DataGateVPNBot.Models.Helpers.Configurations;
using DataGateVPNBot.Services;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DataGateVPNBot.Configurations;

public static class DashboardApiConfiguration
{
    public static void ConfigureDashboardApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisConfig>(configuration.GetSection("Redis"));
        services.Configure<DashboardApiConfig>(configuration.GetSection("DashboardApi"));

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var redisConfig = provider.GetRequiredService<IOptions<RedisConfig>>().Value;
            return ConnectionMultiplexer.Connect(redisConfig.ConnectionString);
        });

        services.AddSingleton<RedisCacheService>();

        services.AddHttpClient("DashboardClient", (provider, client) =>
        {
            var dashboardApiConfig = provider.GetRequiredService<IOptions<DashboardApiConfig>>().Value;
            client.BaseAddress = new Uri(dashboardApiConfig.Url);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddSingleton<IHttpClientFactoryService, HttpClientFactoryService>();
        services.AddSingleton<IHttpRequestService, HttpRequestService>();

        services.AddSingleton<DashBoardApiAuthService>(provider =>
        {
            var dashboardApiConfig = provider.GetRequiredService<IOptions<DashboardApiConfig>>().Value;

            return new DashBoardApiAuthService(
                provider.GetRequiredService<IHttpRequestService>(),
                provider.GetRequiredService<RedisCacheService>(),
                dashboardApiConfig.ClientId,
                dashboardApiConfig.ClientSecret,
                provider.GetRequiredService<ILogger<DashBoardApiAuthService>>());
        });
        
        services.AddScoped<DashBoardApiOvpnFileService>();
    }
}
