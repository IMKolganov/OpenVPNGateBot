using Newtonsoft.Json.Linq;
using DataGateVPNBot.Extensions;
using DataGateVPNBot.Models.Configurations;
using Microsoft.Extensions.Options;

namespace DataGateVPNBot.Services.TelegramApi;

public class WebhookService(
    HttpClient httpClient,
    ILogger<WebhookService> logger,
    IOptions<BotConfiguration> options
)
{
    private readonly BotConfiguration _botConfig = options.Value;
    
    public async Task<bool> IsWebhookSetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_botConfig.BotToken))
            throw new NullReferenceException("BotToken is missing in configuration.");

        if (string.IsNullOrEmpty(_botConfig.HostAddress))
            throw new NullReferenceException("HostAddress is missing in configuration.");

        var url = $"https://api.telegram.org/bot{_botConfig.BotToken}/getWebhookInfo";
        var response = await httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to fetch webhook info: {StatusCode}", response.StatusCode);
            return false;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var root = JObject.Parse(json);

        if (root["ok"]?.Value<bool>() == true)
        {
            var result = root["result"] as JObject;
            var currentUrl = result?["url"]?.Value<string>();
            var hasCustomCertificate = result?["has_custom_certificate"]?.Value<bool>() == true;

            var expectedUrl = BuildWebhookUrl();

            logger.LogInformation("Current webhook URL: {CurrentUrl}, Custom Certificate: {CustomCert}", 
                currentUrl, hasCustomCertificate);

            if (!string.Equals(currentUrl, expectedUrl, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Webhook URL mismatch! Expected: {Expected}, Got: {Actual}", expectedUrl, currentUrl);
                return false;
            }

            if (_botConfig.UseCertificate != hasCustomCertificate)
            {
                logger.LogWarning("Webhook certificate mismatch! Expected custom cert: {Expected}, Got: {Actual}",
                    _botConfig.UseCertificate, hasCustomCertificate);
                return false;
            }

            if (_botConfig.AutoGenerateCertificate && !File.Exists(_botConfig.CertificatePemPath))
            {
                logger.LogWarning("AutoGenerateCertificate is enabled, but certificate file is missing.");
                return false;
            }

            logger.LogInformation("Webhook is correctly set.");
            return true;
        }

        logger.LogWarning("Webhook is missing or incorrect.");
        return false;
    }

    public async Task SetWebhookAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_botConfig.BotToken))
            throw new NullReferenceException("BotToken is missing in configuration.");

        if (string.IsNullOrEmpty(_botConfig.HostAddress))
            throw new NullReferenceException("HostAddress is missing in configuration.");

        var setWebhookUrl = $"https://api.telegram.org/bot{_botConfig.BotToken}/setWebhook";
        var webhookUrl = BuildWebhookUrl();

        logger.LogInformation("Set webhook URL: {Url}", setWebhookUrl);
        logger.LogInformation("Webhook will be set to: {WebhookUrl}", webhookUrl);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(webhookUrl), "url");
        if (_botConfig.UseCertificate)
        {
            logger.LogInformation("Using certificate: {PemPath}", _botConfig.CertificatePemPath);
            var certPath = _botConfig.CertificatePemPath ?? throw new InvalidOperationException("Certificate file path is missing.");
            if (!File.Exists(certPath))
                throw new InvalidOperationException($"Certificate file not found: {certPath}");
            var certStream = File.OpenRead(certPath);
            form.Add(new StreamContent(certStream), "certificate", "datagatetgbot.pem");
        }
        else
        {
            logger.LogInformation("UseCertificate=false: setting webhook without custom certificate (e.g. nginx handles TLS).");
        }

        var response = await httpClient.PostAsync(setWebhookUrl, form, cancellationToken);
        var result = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Webhook successfully set. Response: {Result}", result);
        }
        else
        {
            logger.LogError("Failed to set webhook. Response: {Result}", result);
            throw new Exception($"Failed to set webhook. Response: {result}");
        }
    }

    public async Task DeleteWebhookAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_botConfig.BotToken))
            throw new NullReferenceException("BotToken is missing in configuration.");

        var url = $"https://api.telegram.org/bot{_botConfig.BotToken}/deleteWebhook";
        logger.LogInformation("Sending request to delete webhook. URL: {Url}", url);

        try
        {
            var response = await httpClient.GetAsync(url, cancellationToken);
            var result = await response.Content.ReadAsStringAsync(cancellationToken);

            logger.LogInformation("Delete webhook response: {Result}", result);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Delete webhook request failed with status code {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while deleting webhook");
            throw;
        }
    }

    private string BuildWebhookUrl()
    {
        var host = HostAddressNormalizer.Normalize(_botConfig.HostAddress);
        return WebhookUrlBuilder.Build(host, _botConfig.Port);
    }


}
