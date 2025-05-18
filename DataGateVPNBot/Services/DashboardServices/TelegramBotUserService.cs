using System.Security.Authentication;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Http;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class TelegramBotUserService : ITelegramBotUserService
{
    private readonly IHttpRequestService _httpRequestService;
    private readonly ILogger<TelegramBotUserService> _logger;
    private readonly AuthService _authService;
    private const string EndpointRegisterUser = "api/TelegramBotUser/RegisterUser";
    private const string EndpointGetAdmins = "api/TelegramBotUser/GetAdmins";

    public TelegramBotUserService(ILogger<TelegramBotUserService> logger,
        IHttpRequestService httpRequestService,
        AuthService authService)
    {
        _logger = logger;
        _httpRequestService = httpRequestService;
        _authService = authService;
    }

    public async Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request, 
        CancellationToken cancellationToken)
    {
        if (request.TelegramId <= 0) 
            throw new ArgumentException("TelegramId is required.");
        
        var token = await _authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }
        
        _logger.LogInformation($"Sending request to create TelegramBotUser TelegramId: {request.TelegramId}");

        var response =
            await _httpRequestService.PostAsync<ApiResponse<RegisterUserResponse>>(EndpointRegisterUser, request, 
                token, cancellationToken);

        if (response is { Success: true, Data: not null })
        {
        }
        else
        {
            _logger.LogWarning($"Failed to RegisterUserAsync: {response?.Message}");
        }

        if (response == null)
        {
            _logger.LogError("Failed to RegisterUserAsync.");
        }

        return response!.Data!;
    }

    public async Task<GetAdminsResponse> GetAdminsAsync(CancellationToken cancellationToken)
    {
        var telegramBotAdmins = new GetAdminsResponse();
        var token = await _authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        var url = $"{EndpointGetAdmins}";

        _logger.LogInformation($"Requesting GetAdminsAsync");
        
        var response = await _httpRequestService.GetAsync<ApiResponse<GetAdminsResponse>>(url, token, cancellationToken);
        if (response is { Success: true, Data: not null })
        {
            telegramBotAdmins = response.Data;
        }
        else
        {
            _logger.LogWarning($"Failed to get GetAdminsAsync: {response?.Message}");
        }

        if (response == null)
        {
            _logger.LogError("Failed to GetAdminsAsync.");
        }

        return telegramBotAdmins;
    }
}