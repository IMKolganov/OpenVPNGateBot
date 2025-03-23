using DataGateVPNBot.Handlers;
using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.TelegramApi;
using Telegram.Bot;

namespace DataGateVPNBot.Configurations;

public static class TelegramConfiguration
{
    public static void ConfigureTelegram(this IServiceCollection services, IConfiguration configuration)
    {
        var botConfig = configuration.GetSection("BotConfiguration").Get<BotConfiguration>()
                        ?? throw new NullReferenceException("BotConfiguration section is missing in configuration.");

        services.AddSingleton(botConfig);

        services.AddHttpClient(botConfig.TelegramWebHook)
            .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(botConfig.BotToken, httpClient));
            
        services.AddHttpClient<WebhookService>();
        services.AddHostedService<StartupNotificationHandler>();
    }
}