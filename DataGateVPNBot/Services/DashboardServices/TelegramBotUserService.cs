using System.Security.Authentication;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Http;
using DataGateVPNBot.Services.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotUser.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class TelegramBotUserService(
    ILogger<TelegramBotUserService> logger,
    IHttpRequestService httpRequestService,
    AuthService authService,
    IErrorService errorService)
    : ITelegramBotUserService
{
    private const string EndpointRegisterUser = "api/TelegramBotUser/RegisterUser";
    private const string EndpointGetAdmins = "api/TelegramBotUser/GetAdmins";

    public async Task<RegisterUserResponse> RegisterUserAsync(RegisterUserRequest request, 
        CancellationToken cancellationToken)
    {
        var fullName = $"{request.FirstName} {request.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? "Unnamed" : fullName;
        var message = $"👤 New user registered:\n" +
                      $"ID: `{request.TelegramId}`\n" +
                      $"Username: @{request.Username?.Trim().TrimStart('@')}\n" +
                      $"Name: {displayName}";

        await errorService.NotifyAdmins(message, cancellationToken);
        if (request.TelegramId <= 0) 
            throw new ArgumentException("TelegramId is required.");
        
        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }
        
        logger.LogInformation($"Sending request to create TelegramBotUser TelegramId: {request.TelegramId}");

        var response =
            await httpRequestService.PostAsync<ApiResponse<RegisterUserResponse>>(EndpointRegisterUser, request, 
                token, cancellationToken);

        if (response is { Success: true, Data: not null })
        {
        }
        else
        {
            logger.LogWarning($"Failed to RegisterUserAsync: {response?.Message}");
        }

        if (response == null)
        {
            logger.LogError("Failed to RegisterUserAsync.");
        }

        return response!.Data!;
    }

    public async Task<GetAdminsResponse> GetAdminsAsync(CancellationToken cancellationToken)
    {
        var telegramBotAdmins = new GetAdminsResponse();
        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        var url = $"{EndpointGetAdmins}";

        logger.LogInformation($"Requesting GetAdminsAsync");
        
        var response = await httpRequestService.GetAsync<ApiResponse<GetAdminsResponse>>(url, token, cancellationToken);
        if (response is { Success: true, Data: not null })
        {
            telegramBotAdmins = response.Data;
        }
        else
        {
            logger.LogWarning($"Failed to get GetAdminsAsync: {response?.Message}");
        }

        if (response == null)
        {
            logger.LogError("Failed to GetAdminsAsync.");
        }

        return telegramBotAdmins;
    }
}