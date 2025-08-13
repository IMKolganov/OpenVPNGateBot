using System.Net;
using System.Text.Json;
using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.LetsEncrypt;
using Microsoft.Extensions.Options;

namespace DataGateVPNBot.Services.TelegramApi;

public class WebhookService(
    HttpClient httpClient,
    ILogger<WebhookService> logger,
    IOptions<BotConfiguration> options,
    OpensslCertificateGenerator opensslCertificateGenerator,
    LetsEncryptCertificateGenerator letsEncryptCertificateGenerator)
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
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("ok", out var ok) && ok.GetBoolean())
        {
            var result = root.GetProperty("result");
            var currentUrl = result.TryGetProperty("url", out var urlElement) ? urlElement.GetString() : null;
            var hasCustomCertificate = result.TryGetProperty("has_custom_certificate", out var certElement) 
                                       && certElement.GetBoolean();

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

        if (_botConfig.UseCertificate || _botConfig.AutoGenerateCertificate)
        {
            var pemPath = _botConfig.CertificatePemPath;

            if (_botConfig.AutoGenerateCertificate)
            {
                if (IsDomainName(_botConfig.HostAddress))
                {
                    logger.LogInformation("Auto-generating Let's Encrypt certificate (domain detected)...");
                    await letsEncryptCertificateGenerator.EnsureCertificateAsync(_botConfig.HostAddress, 
                        "imkolganov@gmail.com", cancellationToken);
                }
                else
                {
                    logger.LogInformation("Auto-generating self-signed certificate (IP detected)...");
                    await opensslCertificateGenerator.EnsureCertificateAsync(_botConfig.HostAddress, cancellationToken);
                }
            }

            if (string.IsNullOrEmpty(pemPath))
                throw new NullReferenceException("CertificatePemPath is missing in configuration.");

            if (!File.Exists(pemPath))
                throw new FileNotFoundException($"Certificate file '{pemPath}' not found.");

            logger.LogInformation("Using certificate: {PemPath}", pemPath);
            var certStream = File.OpenRead(pemPath);
            form.Add(new StreamContent(certStream), "certificate", "datagatetgbot.pem");
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
        var host = _botConfig.HostAddress
            .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');

        var portPart = _botConfig.Port == 443 ? "" : $":{_botConfig.Port}";
        return $"https://{host}{portPart}/api/bot";
    }

    private static bool IsDomainName(string input)
    {
        if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            input = new Uri(input).Host;
        }

        return Uri.CheckHostName(input) == UriHostNameType.Dns &&
               !IPAddress.TryParse(input, out _);
    }
}
