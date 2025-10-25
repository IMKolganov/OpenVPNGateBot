using DataGateVPNBot.Services.Http;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

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
                "/api/Auth/token", requestBody);

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
}