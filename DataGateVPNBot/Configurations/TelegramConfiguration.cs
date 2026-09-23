using DataGateVPNBot.Extensions;
using DataGateVPNBot.Handlers;
using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.CryptoPay;
using DataGateVPNBot.Services.Donations;
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
        services.Configure<CryptoPayConfiguration>(configuration.GetSection(CryptoPayConfiguration.SectionName));
        services.Configure<TelegramStarsConfiguration>(configuration.GetSection(TelegramStarsConfiguration.SectionName));

        // Override with environment variables
        services.PostConfigure<BotConfiguration>(TelegramConfigurationHelper.ApplyEnvAndValidate);
        services.PostConfigure<CryptoPayConfiguration>(CryptoPayConfigurationHelper.ApplyEnv);
        services.PostConfigure<TelegramStarsConfiguration>(TelegramStarsConfigurationHelper.ApplyEnv);

        // Register TelegramBotClient using options
        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BotConfiguration>>().Value;
            return new TelegramBotClient(options.BotToken);
        });

        services.AddSingleton<OpensslCertificateGenerator>();
        services.AddTransient<LetsEncryptCertificateGenerator>();
        services.AddHttpClient<WebhookService>();
        services.AddHttpClient<ICryptoPayApiClient, CryptoPayApiClient>((sp, client) =>
        {
            var cryptoPay = sp.GetRequiredService<IOptions<CryptoPayConfiguration>>().Value;
            client.BaseAddress = new Uri(cryptoPay.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<ICryptoPayPaidInvoiceStore, CryptoPayPaidInvoiceStore>();
        services.AddSingleton<StarsPaidChargeStore>();
        services.AddScoped<ICryptoPayDonationService, CryptoPayDonationService>();
        services.AddHostedService<StartupBackgroundService>();
    }
}
