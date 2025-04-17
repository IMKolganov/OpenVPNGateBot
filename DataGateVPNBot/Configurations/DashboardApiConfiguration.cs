using System.Net.Http.Headers;
using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Http;
using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;

namespace DataGateVPNBot.Configurations;

public static class DashboardApiConfiguration
{
    public static void ConfigureDashboardApi(this IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("dashboardapi.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var dashboardConfig = new DashboardApiConfig
        {
            Url = Environment.GetEnvironmentVariable("DASHBOARDAPI_URL")
                  ?? config["DashboardApi:Url"]
                  ?? string.Empty,

            ClientId = Environment.GetEnvironmentVariable("DASHBOARDAPI_CLIENTID")
                       ?? config["DashboardApi:ClientId"]
                       ?? string.Empty,

            ClientSecret = Environment.GetEnvironmentVariable("DASHBOARDAPI_CLIENTSECRET")
                           ?? config["DashboardApi:ClientSecret"]
                           ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(dashboardConfig.Url))
        {
            Log.ForContext("SourceContext", "DashboardApi")
                .Error("❌ DashboardApi:Url is missing.");
            throw new NullReferenceException("DashboardApi:Url is required.");
        }

        Log.ForContext("SourceContext", "DashboardApi")
            .Information("📡 DashboardClient will be configured with base URL: {Url}", dashboardConfig.Url);

        services.AddSingleton(dashboardConfig);

        // Redis
        services.AddSingleton<RedisConnectionFactory>();
        services.AddSingleton<RedisCacheService>(sp =>
        {
            var factory = sp.GetRequiredService<RedisConnectionFactory>();
            var redis = factory.CreateConnection();
            return new RedisCacheService(redis);
        });

        // HTTP Client
        services.AddHttpClient("DashboardClient", (provider, client) =>
        {
            client.BaseAddress = new Uri(dashboardConfig.Url);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        // HTTP
        services.AddSingleton<IHttpClientFactoryService, HttpClientFactoryService>();
        services.AddSingleton<IHttpRequestService, HttpRequestService>();

        services.AddSingleton<AuthService>(provider =>
            new AuthService(
                provider.GetRequiredService<IHttpRequestService>(),
                provider.GetRequiredService<RedisCacheService>(),
                dashboardConfig.ClientId,
                dashboardConfig.ClientSecret,
                provider.GetRequiredService<ILogger<AuthService>>())
        );

        services.AddScoped<OvpnFileService>();
    }
}