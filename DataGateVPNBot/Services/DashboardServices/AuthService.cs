using System.Text.Json;
using DataGateVPNBot.Services.Http;

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

        var response = await httpRequestService.PostAsync<JsonElement>("/api/Auth/token", requestBody);

        if (response.ValueKind == JsonValueKind.Object &&
            response.TryGetProperty("token", out var tokenProperty))
        {
            var newToken = tokenProperty.GetString();
            if (!string.IsNullOrEmpty(newToken))
            {
                _cachedToken = newToken;
                _tokenExpiry = DateTime.UtcNow.Add(_tokenExpiration);
                logger.LogInformation("Token cached in memory.");
                return newToken;
            }
        }
        else
        {
            logger.LogWarning("Invalid response structure or missing 'token' field.");
        }

        logger.LogError("Failed to obtain a valid token from API.");
        return null;
    }
}