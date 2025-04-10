using System.Text.Json;
using DataGateVPNBot.Services.Http;

namespace DataGateVPNBot.Services.DashboardServices;

public class DashBoardApiAuthService
{
    private readonly IHttpRequestService _httpRequestService;
    private readonly RedisCacheService _redisCacheService;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ILogger<DashBoardApiAuthService> _logger;

    private const string TokenCacheKey = "dashboard_openvpn_token";
    private readonly TimeSpan _tokenExpiration = TimeSpan.FromHours(1);

    public DashBoardApiAuthService(
        IHttpRequestService httpRequestService,
        RedisCacheService redisCacheService,
        string clientId,
        string clientSecret,
        ILogger<DashBoardApiAuthService> logger)
    {
        _httpRequestService = httpRequestService;
        _redisCacheService = redisCacheService;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _logger = logger;
    }

    public async Task<string?> GetTokenAsync()
    {
        string? cachedToken = null;

        try
        {
            cachedToken = await _redisCacheService.GetTokenWithExpirationAsync(TokenCacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable while getting token.");
        }

        if (cachedToken != null)
        {
            _logger.LogInformation("Using cached token from Redis.");
            return cachedToken;
        }

        _logger.LogInformation("Cached token not found or expired. Requesting new token...");

        var requestBody = new
        {
            ClientId = _clientId,
            ClientSecret = _clientSecret
        };

        var response = await _httpRequestService.PostAsync<JsonElement>("/api/Auth/token", requestBody);

        if (response.ValueKind == JsonValueKind.Object &&
            response.TryGetProperty("token", out var tokenProperty))
        {
            var newToken = tokenProperty.GetString();
            if (!string.IsNullOrEmpty(newToken))
            {
                try
                {
                    await _redisCacheService.SetTokenWithExpirationAsync(TokenCacheKey, newToken, _tokenExpiration);
                    _logger.LogInformation("New token saved in Redis.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis unavailable while setting token.");
                }

                return newToken;
            }
        }
        else
        {
            _logger.LogWarning("Invalid response structure or missing 'token' field.");
        }

        _logger.LogError("Failed to obtain a valid token from API.");
        return null;
    }

}