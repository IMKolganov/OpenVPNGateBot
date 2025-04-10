using System.Net.Http.Headers;
using DataGateVPNBot.Models.Helpers.Configurations;
using DataGateVPNBot.Services;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;

namespace DataGateVPNBot.Configurations;

public static class DashboardApiConfiguration
{
    public static void ConfigureDashboardApi(this IServiceCollection services, IConfiguration configuration)
    {
        // Конфигурации
        services.Configure<RedisConfig>(configuration.GetSection("Redis"));
        services.Configure<DashboardApiConfig>(configuration.GetSection("DashboardApi"));

        // Лог подключения к Dashboard API
        var dashboardConfig = configuration.GetSection("DashboardApi").Get<DashboardApiConfig>();
        Log.ForContext("SourceContext", "DashboardApi")
            .Information("📡 DashboardClient will be configured with base URL: {Url}", dashboardConfig!.Url);

        // Redis
        services.AddSingleton<RedisConnectionFactory>();
        services.AddSingleton<RedisCacheService>(sp =>
        {
            var factory = sp.GetRequiredService<RedisConnectionFactory>();
            var redis = factory.CreateConnection();
            return new RedisCacheService(redis);
        });

        // HttpClient for Dashboard
        services.AddHttpClient("DashboardClient", (provider, client) =>
        {
            var dashboardApiConfig = provider.GetRequiredService<IOptions<DashboardApiConfig>>().Value;
            client.BaseAddress = new Uri(dashboardApiConfig.Url);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        // HTTP
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