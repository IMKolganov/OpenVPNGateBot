using DataGateVPNBot.Services.Http;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class AuthService(
    IHttpRequestService httpRequestService,
    string clientId,
    string clientSecret,
    ILogger<AuthService> logger)
{
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly TimeSpan _tokenExpiration = TimeSpan.FromMinutes(55);
    private const string EndpointAuthByToken = "api/auth/token";
    private const string EndpointTelegramRequestLoginCode = "api/auth/telegram/request-login-code";

    public async Task<string?> GetTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
        {
            logger.LogInformation("Using cached token from memory.");
            return _cachedToken;
        }

        logger.LogInformation("Token not found or expired. Requesting new token...");

        var requestBody = new
        {
            ClientId = clientId,
            ClientSecret = clientSecret
        };

        try
        {
            var response = await httpRequestService.PostAsync<ApiResponse<TokenResponse>>(
                EndpointAuthByToken, requestBody);

            if (response == null)
            {
                logger.LogWarning("Empty response from API.");
                return null;
            }

            if (!response.Success || response.Data == null)
            {
                logger.LogWarning("Token request failed: {Message}", response.Message);
                return null;
            }

            var newToken = response.Data.Token;
            if (string.IsNullOrEmpty(newToken))
            {
                logger.LogWarning("Received empty token string.");
                return null;
            }

            _cachedToken = newToken;

            _tokenExpiry = response.Data.Expiration.UtcDateTime != default
                ? response.Data.Expiration.UtcDateTime
                : DateTime.UtcNow.Add(_tokenExpiration);

            logger.LogInformation("✅ Token cached until {Expiry}", _tokenExpiry);
            return newToken;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to obtain token from API.");
            return null;
        }
    }

    public async Task<TelegramRequestLoginCodeResponse?> RequestDashboardLoginCodeAsync(long telegramId, CancellationToken ct = default)
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            logger.LogWarning("Cannot request login code: App token unavailable.");
            return null;
        }

        var body = new TelegramRequestLoginCodeRequest { TelegramId = telegramId };

        try
        {
            var response = await httpRequestService.PostAsync<ApiResponse<TelegramRequestLoginCodeResponse>>(
                EndpointTelegramRequestLoginCode,
                body,
                token,
                ct);

            if (response is not { Success: true, Data: not null })
            {
                logger.LogWarning("Login code request failed for TelegramId {TelegramId}: {Message}",
                    telegramId, response?.Message);
                return null;
            }

            return response.Data;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to request dashboard login code for TelegramId {TelegramId}", telegramId);
            return null;
        }
    }
}