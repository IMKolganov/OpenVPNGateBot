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
        var envWebHook = Environment.GetEnvironmentVariable("HOST_ADDRESS");
        var envPort = Environment.GetEnvironmentVariable("TELEGRAMBOT_PORT");
        var envCertPfxPath = Environment.GetEnvironmentVariable("CERTIFICATE_PFX_PATH");
        var envCertPemPath = Environment.GetEnvironmentVariable("CERTIFICATE_PEM_PATH");
        var envUseCert = Environment.GetEnvironmentVariable("USE_CERTIFICATE");
        var envAutoGenerateCertificate = Environment.GetEnvironmentVariable("AUTO_GENERATE_CERTIFICATE");

        if (!string.IsNullOrEmpty(envBotToken))
            botConfig.BotToken = envBotToken;

        if (!string.IsNullOrEmpty(envWebHook))
            botConfig.HostAddress = envWebHook;
        
        if (!string.IsNullOrEmpty(envPort))
            botConfig.Port = Convert.ToInt32(envPort);

        if (!string.IsNullOrEmpty(envCertPfxPath))
            botConfig.CertificatePfxPath = envCertPfxPath;
        
        if (!string.IsNullOrEmpty(envCertPemPath))
            botConfig.CertificatePemPath = envCertPemPath;


        if (!string.IsNullOrEmpty(envUseCert) && bool.TryParse(envUseCert, out var useCert))
            botConfig.UseCertificate = useCert;
        
        if (!string.IsNullOrEmpty(envAutoGenerateCertificate) 
            && bool.TryParse(envAutoGenerateCertificate, out var autoGenerateCertificate))
            botConfig.AutoGenerateCertificate = autoGenerateCertificate;

        if (string.IsNullOrEmpty(botConfig.BotToken))
            throw new NullReferenceException("BotToken is missing in configuration or environment variables.");

        if (string.IsNullOrEmpty(botConfig.HostAddress))
            throw new NullReferenceException("TelegramWebHook is missing in configuration or environment variables.");

        services.AddSingleton(botConfig);

        services.AddHttpClient(botConfig.HostAddress)
            .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(botConfig.BotToken, httpClient));

        services.AddSingleton<CertificateGenerator>();
        services.AddHttpClient<WebhookService>();
        services.AddHostedService<StartupNotificationHandler>();
    }
}