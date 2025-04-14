using System.Text.Json;
using DataGateVPNBot.Models.Configurations;

namespace DataGateVPNBot.Services.TelegramApi;

public class WebhookService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookService> _logger;
    private readonly BotConfiguration _botConfig;
    private readonly CertificateGenerator _certificateGenerator;

    public WebhookService(HttpClient httpClient, ILogger<WebhookService> logger, BotConfiguration botConfig,
        CertificateGenerator certificateGenerator)
    {
        _httpClient = httpClient;
        _logger = logger;
        _botConfig = botConfig;
        _certificateGenerator = certificateGenerator;
    }

    public async Task<bool> IsWebhookSetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_botConfig.BotToken))
            throw new NullReferenceException("BotToken is missing in configuration.");

        if (string.IsNullOrEmpty(_botConfig.HostAddress))
            throw new NullReferenceException("TelegramWebHook is missing in configuration.");

        var url = $"https://api.telegram.org/bot{_botConfig.BotToken}/getWebhookInfo";
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to fetch webhook info: {response.StatusCode}");
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

            _logger.LogInformation($"Current webhook URL: {currentUrl}, Custom Certificate: {hasCustomCertificate}");

            if (currentUrl != $"_botConfig.HostAddress:{_botConfig.Port}")
            {
                _logger.LogWarning($"Webhook URL mismatch! Expected: {_botConfig.HostAddress}:{_botConfig.Port}, " +
                                   $"Got: {currentUrl}");
                return false;
            }

            if (_botConfig.UseCertificate != hasCustomCertificate)
            {
                _logger.LogWarning($"Webhook certificate mismatch! Expected custom cert: {_botConfig.UseCertificate}, " +
                                   $"Got: {hasCustomCertificate}");
                return false;
            }

            _logger.LogInformation("Webhook is correctly set.");
            return true;
        }

        _logger.LogWarning("Webhook is missing or incorrect.");
        return false;
    }

    public async Task SetWebhookAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_botConfig.BotToken))
            throw new NullReferenceException("BotToken is missing in configuration.");

        if (string.IsNullOrEmpty(_botConfig.HostAddress))
            throw new NullReferenceException("TelegramWebHook is missing in configuration.");

        var url = $"https://api.telegram.org/bot{_botConfig.BotToken}/setWebhook";
        _logger.LogInformation($"Set webhook URL: {url}");

        using var form = new MultipartFormDataContent();

        var webhookUrl = $"https://{_botConfig.HostAddress}:{_botConfig.Port}/bot";
        form.Add(new StringContent(webhookUrl), "url");
        _logger.LogInformation($"{webhookUrl}");

        if (_botConfig.UseCertificate || _botConfig.AutoGenerateCertificate)
        {
            Stream certStream;
            string certName = "datagatetgbot.crt";

            if (_botConfig.AutoGenerateCertificate)
            {
                _logger.LogInformation("Auto-generating certificate...");
                _certificateGenerator.EnsureCertificate(_botConfig.HostAddress);
            }

            var crtPath = _botConfig.CertificateCrtPath;
            if (string.IsNullOrEmpty(crtPath))
                throw new NullReferenceException("CertificateCrtPath is missing in configuration.");

            if (!File.Exists(crtPath))
                throw new FileNotFoundException($"Certificate file '{crtPath}' not found.");

            _logger.LogInformation($"Using certificate: {crtPath}");
            certStream = File.OpenRead(crtPath);

            form.Add(new StreamContent(certStream), "certificate", certName);
        }

        var response = await _httpClient.PostAsync(url, form, cancellationToken);
        var result = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation($"Webhook successfully set. Response: {result}");
        }
        else
        {
            _logger.LogError($"Failed to set webhook. Response: {result}");
        }
    }
}