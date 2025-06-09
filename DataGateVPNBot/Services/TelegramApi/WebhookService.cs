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
            throw new NullReferenceException("HostAddress is missing in configuration.");

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

            var expectedUrl = $"https://{_botConfig.HostAddress}:{_botConfig.Port}/bot";

            _logger.LogInformation($"Current webhook URL: {currentUrl}, Custom Certificate: {hasCustomCertificate}");

            if (!string.Equals(currentUrl, expectedUrl, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Webhook URL mismatch! Expected: {expectedUrl}, Got: {currentUrl}");
                return false;
            }

            if (_botConfig.UseCertificate != hasCustomCertificate)
            {
                _logger.LogWarning($"Webhook certificate mismatch! Expected custom cert: {_botConfig.UseCertificate}, Got: {hasCustomCertificate}");
                return false;
            }

            _logger.LogInformation("Webhook is correctly set.");
            if (_botConfig.AutoGenerateCertificate && !File.Exists(_botConfig.CertificatePemPath))
            {
                _logger.LogWarning("AutoGenerateCertificate is enabled, but certificate file is missing.");
                return false;
            }
            
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
            throw new NullReferenceException("HostAddress is missing in configuration.");

        var url = $"https://api.telegram.org/bot{_botConfig.BotToken}/setWebhook";
        var webhookUrl = $"https://{_botConfig.HostAddress}:{_botConfig.Port}/bot";

        _logger.LogInformation($"Set webhook URL: {url}");
        _logger.LogInformation($"{webhookUrl}");

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(webhookUrl), "url");

        if (_botConfig.UseCertificate || _botConfig.AutoGenerateCertificate)
        {
            var pemPath = _botConfig.CertificatePemPath;

            if (_botConfig.AutoGenerateCertificate)
            {
                _logger.LogInformation("Auto-generating certificate...");
                _certificateGenerator.EnsureCertificate(_botConfig.HostAddress);
            }

            if (string.IsNullOrEmpty(pemPath))
                throw new NullReferenceException("CertificatePemPath is missing in configuration.");

            if (!File.Exists(pemPath))
                throw new FileNotFoundException($"Certificate file '{pemPath}' not found.");

            _logger.LogInformation($"Using certificate: {pemPath}");
            var certStream = File.OpenRead(pemPath);
            form.Add(new StreamContent(certStream), "certificate", "datagatetgbot.pem");
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
    
    public async Task DeleteWebhookAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_botConfig.BotToken))
            throw new NullReferenceException("BotToken is missing in configuration.");

        if (string.IsNullOrEmpty(_botConfig.HostAddress))
            throw new NullReferenceException("HostAddress is missing in configuration.");

        var url = $"https://api.telegram.org/bot{_botConfig.BotToken}/deleteWebhook";

        _logger.LogInformation($"Sending request to delete webhook. URL: {url}");

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var result = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation($"Delete webhook response: {result}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Delete webhook request failed with status code {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while deleting webhook");
            throw;
        }
    }

}
