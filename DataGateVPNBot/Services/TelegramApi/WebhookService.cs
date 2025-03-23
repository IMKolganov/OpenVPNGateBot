using System.Text.Json;
using DataGateVPNBot.Models.Configurations;

namespace DataGateVPNBot.Services.TelegramApi;

public class WebhookService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookService> _logger;
    private readonly BotConfiguration _botConfig;

    public WebhookService(HttpClient httpClient, ILogger<WebhookService> logger, BotConfiguration botConfig)
    {
        _httpClient = httpClient;
        _logger = logger;
        _botConfig = botConfig;
    }

    public async Task<bool> IsWebhookSetAsync(CancellationToken cancellationToken)
    {
        var url = $"https://api.telegram.org/bot{_botConfig.BotToken}/getWebhookInfo";
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch webhook info: {StatusCode}", response.StatusCode);
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
            var configuredCertificate =
                !string.IsNullOrEmpty(_botConfig.CertificatePath);

            _logger.LogInformation("Current webhook URL: {CurrentUrl}, Custom Certificate: {HasCertificate}",
                currentUrl, hasCustomCertificate);

            if (currentUrl != _botConfig.TelegramWebHook)
            {
                _logger.LogWarning("Webhook URL mismatch! Expected: {Expected}, Got: {Actual}",
                    _botConfig.TelegramWebHook, currentUrl);
                return false;
            }

            if (configuredCertificate != hasCustomCertificate)
            {
                _logger.LogWarning("Webhook certificate mismatch! Expected custom cert: {Expected}, Got: {Actual}",
                    configuredCertificate, hasCustomCertificate);
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
        var url = $"https://api.telegram.org/bot{_botConfig.BotToken}/setWebhook";
        using var form = new MultipartFormDataContent();
    
        form.Add(new StringContent(_botConfig.TelegramWebHook), "url");

        var certificatePath = _botConfig.CertificatePath;
        var certificateFileName = Path.GetFileName(certificatePath);

        form.Add(new ByteArrayContent(
                await File.ReadAllBytesAsync(certificatePath, cancellationToken)), 
            "certificate", 
            certificateFileName);

        var response = await _httpClient.PostAsync(url, form, cancellationToken);
        var result = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Webhook successfully set.");
        }
        else
        {
            _logger.LogError("Failed to set webhook: {Response}", result);
        }
    }
}