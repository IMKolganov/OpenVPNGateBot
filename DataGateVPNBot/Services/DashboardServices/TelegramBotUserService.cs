using System.Text.Json;
using DataGateVPNBot.Services.Http;

namespace DataGateVPNBot.Services.DashboardServices;

public class TelegramBotUserService
{
    private readonly IHttpRequestService _httpRequestService;
    private readonly RedisCacheService _redisCacheService;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ILogger<AuthService> _logger;

    private const string TokenCacheKey = "dashboard_openvpn_token";
    private readonly TimeSpan _tokenExpiration = TimeSpan.FromHours(1);

    public TelegramBotUserService(
        IHttpRequestService httpRequestService,
        RedisCacheService redisCacheService,
        string clientId,
        string clientSecret,
        ILogger<AuthService> logger)
    {
        _httpRequestService = httpRequestService;
        _redisCacheService = redisCacheService;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _logger = logger;
    }
}