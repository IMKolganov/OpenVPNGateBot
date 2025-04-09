using DataGateVPNBot.Handlers;
using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.TelegramApi;
using Telegram.Bot;

namespace DataGateVPNBot.Configurations;

public static class TelegramConfiguration
{
    public static void ConfigureTelegram(this IServiceCollection services, IConfiguration configuration)
    {
        var botConfig = configuration.GetSection("BotConfiguration").Get<BotConfiguration>() ?? new BotConfiguration();

        var envBotToken = Environment.GetEnvironmentVariable("TELEGRAMBOT_BOT_TOKEN");
        var envWebHook = Environment.GetEnvironmentVariable("TELEGRAM_WEBHOOK");
        var envCertPath = Environment.GetEnvironmentVariable("CERTIFICATE_PATH");
        var envUseCert = Environment.GetEnvironmentVariable("USE_CERTIFICATE");

        if (!string.IsNullOrEmpty(envBotToken))
            botConfig.BotToken = envBotToken;

        if (!string.IsNullOrEmpty(envWebHook))
            botConfig.BotWebhookUrl = envWebHook;

        if (!string.IsNullOrEmpty(envCertPath))
            botConfig.CertificatePath = envCertPath;

        if (!string.IsNullOrEmpty(envUseCert) && bool.TryParse(envUseCert, out var useCert))
            botConfig.UseCertificate = useCert;

        if (string.IsNullOrEmpty(botConfig.BotToken))
            throw new NullReferenceException("BotToken is missing in configuration or environment variables.");

        if (string.IsNullOrEmpty(botConfig.BotWebhookUrl))
            throw new NullReferenceException("TelegramWebHook is missing in configuration or environment variables.");

        services.AddSingleton(botConfig);

        services.AddHttpClient(botConfig.BotWebhookUrl)
            .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(botConfig.BotToken, httpClient));

        services.AddHttpClient<WebhookService>();
        services.AddHostedService<StartupNotificationHandler>();
    }
}