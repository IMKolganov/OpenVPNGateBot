using System.Text.Json;
using DataGateVPNBot.Models.Configurations;

namespace DataGateVPNBot.Services.TelegramApi;

public class WebhookService(
    HttpClient httpClient,
    ILogger<WebhookService> logger,
    BotConfiguration botConfig,
    CertificateGenerator certificateGenerator)
{
    public async Task<bool> IsWebhookSetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(botConfig.BotToken))
            throw new NullReferenceException("BotToken is missing in configuration.");

        if (string.IsNullOrEmpty(botConfig.HostAddress))
            throw new NullReferenceException("HostAddress is missing in configuration.");

        var url = $"https://api.telegram.org/bot{botConfig.BotToken}/getWebhookInfo";
        var response = await httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError($"Failed to fetch webhook info: {response.StatusCode}");
            return false;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("ok", out var ok) && ok.GetBoolean())
        {
            var result = root.GetProperty("result");

            var currentUrl = result.TryGetProperty("url", out var urlElement) ? urlElement.GetString() : null;
            var hasCustomCertificate = result.TryGetProperty("has_custom_certificate", out var certElement) &&
                                       certElement.GetBoolean();

            var expectedUrl = $"https://{botConfig.HostAddress}:{botConfig.Port}/bot";

            logger.LogInformation($"Current webhook URL: {currentUrl}, Custom Certificate: {hasCustomCertificate}");

            if (!string.Equals(currentUrl, expectedUrl, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning($"Webhook URL mismatch! Expected: {expectedUrl}, Got: {currentUrl}");
                return false;
            }

            if (botConfig.UseCertificate != hasCustomCertificate)
            {
                logger.LogWarning($"Webhook certificate mismatch! Expected custom cert: {botConfig.UseCertificate}, Got: {hasCustomCertificate}");
                return false;
            }

            logger.LogInformation("Webhook is correctly set.");
            if (botConfig.AutoGenerateCertificate && !File.Exists(botConfig.CertificatePemPath))
            {
                logger.LogWarning("AutoGenerateCertificate is enabled, but certificate file is missing.");
                return false;
            }
            
            return true;
        }

        logger.LogWarning("Webhook is missing or incorrect.");
        return false;
    }

    public async Task SetWebhookAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(botConfig.BotToken))
            throw new NullReferenceException("BotToken is missing in configuration.");

        if (string.IsNullOrEmpty(botConfig.HostAddress))
            throw new NullReferenceException("HostAddress is missing in configuration.");

        var url = $"https://api.telegram.org/bot{botConfig.BotToken}/setWebhook";
        var webhookUrl = $"https://{botConfig.HostAddress}:{botConfig.Port}/bot";

        logger.LogInformation($"Set webhook URL: {url}");
        logger.LogInformation($"{webhookUrl}");

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(webhookUrl), "url");

        if (botConfig.UseCertificate || botConfig.AutoGenerateCertificate)
        {
            var pemPath = botConfig.CertificatePemPath;

            if (botConfig.AutoGenerateCertificate)
            {
                logger.LogInformation("Auto-generating certificate...");
                await certificateGenerator.EnsureCertificateAsync(botConfig.HostAddress, cancellationToken);
            }

            if (string.IsNullOrEmpty(pemPath))
                throw new NullReferenceException("CertificatePemPath is missing in configuration.");

            if (!File.Exists(pemPath))
                throw new FileNotFoundException($"Certificate file '{pemPath}' not found.");

            logger.LogInformation($"Using certificate: {pemPath}");
            var certStream = File.OpenRead(pemPath);
            form.Add(new StreamContent(certStream), "certificate", "datagatetgbot.pem");
        }

        var response = await httpClient.PostAsync(url, form, cancellationToken);
        var result = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation($"Webhook successfully set. Response: {result}");
        }
        else
        {
            logger.LogError($"Failed to set webhook. Response: {result}");
        }
    }
    
    public async Task DeleteWebhookAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(botConfig.BotToken))
            throw new NullReferenceException("BotToken is missing in configuration.");

        if (string.IsNullOrEmpty(botConfig.HostAddress))
            throw new NullReferenceException("HostAddress is missing in configuration.");

        var url = $"https://api.telegram.org/bot{botConfig.BotToken}/deleteWebhook";

        logger.LogInformation($"Sending request to delete webhook. URL: {url}");

        try
        {
            var response = await httpClient.GetAsync(url, cancellationToken);
            var result = await response.Content.ReadAsStringAsync(cancellationToken);

            logger.LogInformation($"Delete webhook response: {result}");

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

}
