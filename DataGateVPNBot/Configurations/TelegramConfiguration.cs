using DataGateVPNBot.Handlers;
using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.LetsEncrypt;
using DataGateVPNBot.Services.TelegramApi;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace DataGateVPNBot.Configurations;

public static class TelegramConfiguration
{
    public static void ConfigureTelegram(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration section to BotConfiguration with default values
        services.Configure<BotConfiguration>(configuration.GetSection("BotConfiguration"));

        // Override with environment variables
        services.PostConfigure<BotConfiguration>(botConfig =>
        {
            var envBotToken = Environment.GetEnvironmentVariable("TELEGRAMBOT_BOT_TOKEN");
            var envHost = Environment.GetEnvironmentVariable("HOST_ADDRESS");
            var envPort = Environment.GetEnvironmentVariable("TELEGRAMBOT_PORT");
            var envEmail = Environment.GetEnvironmentVariable("EMAIL");
            var envCertPfxPath = Environment.GetEnvironmentVariable("CERTIFICATE_PFX_PATH");
            var envCertPemPath = Environment.GetEnvironmentVariable("CERTIFICATE_PEM_PATH");
            var envUseCert = Environment.GetEnvironmentVariable("USE_CERTIFICATE");
            var envAutoGen = Environment.GetEnvironmentVariable("AUTO_GENERATE_CERTIFICATE");

            if (!string.IsNullOrWhiteSpace(envBotToken)) botConfig.BotToken = envBotToken;
            if (!string.IsNullOrWhiteSpace(envHost)) botConfig.HostAddress = envHost;
            if (!string.IsNullOrWhiteSpace(envPort) && int.TryParse(envPort, out var port)) botConfig.Port = port;

            if (!string.IsNullOrWhiteSpace(envEmail)) botConfig.Email = envEmail;
            if (!string.IsNullOrWhiteSpace(envCertPfxPath)) botConfig.CertificatePfxPath = envCertPfxPath;
            if (!string.IsNullOrWhiteSpace(envCertPemPath)) botConfig.CertificatePemPath = envCertPemPath;

            if (bool.TryParse(envUseCert, out var useCert)) botConfig.UseCertificate = useCert;
            if (bool.TryParse(envAutoGen, out var autoGen)) botConfig.AutoGenerateCertificate = autoGen;

            if (string.IsNullOrWhiteSpace(botConfig.BotToken))
                throw new NullReferenceException("BotToken is missing in configuration or environment variables.");

            if (string.IsNullOrWhiteSpace(botConfig.HostAddress))
                throw new NullReferenceException("HostAddress is missing in configuration or environment variables.");
        });

        // Register TelegramBotClient using options
        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BotConfiguration>>().Value;
            return new TelegramBotClient(options.BotToken);
        });

        services.AddSingleton<OpensslCertificateGenerator>();
        services.AddTransient<LetsEncryptCertificateGenerator>();
        services.AddHttpClient<WebhookService>();
        services.AddHostedService<StartupBackgroundService>();
    }
}
