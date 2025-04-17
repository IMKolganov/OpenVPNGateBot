using System.Security.Authentication;
using DataGateVPNBot.Models.Enums;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.DataServices.Interfaces;
using DataGateVPNBot.Services.Http;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Requests;
using OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class LocalizationService(ILogger<LocalizationService> logger, IHttpRequestService httpRequestService,
    AuthService authService) : ILocalizationService
{
    private const string EndpointSetTelegramUserLanguage = "api/TelegramBotLocalization/SetTelegramUserLanguage";
    private const string EndpointGetTelegramUserLanguage = "api/TelegramBotLocalization/GetTelegramUserLanguage";
    private const string EndpointIsExistTelegramUserLanguagePreference = 
        "api/TelegramBotLocalization/IsExistTelegramUserLanguagePreference";

    public async Task<SetTelegramUserLanguageResponse> SetTelegramUserLanguageAsync(
        SetTelegramUserLanguageRequest request, CancellationToken cancellationToken)
    {
        if (request.TelegramId <= 0)
            throw new ArgumentException("TelegramId is required.");

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        logger.LogInformation("Sending request to set user language " +
                               $"TelegramId: {request.TelegramId}");

        var response =
            await httpRequestService.PostAsync<ApiResponse<SetTelegramUserLanguageResponse>>(
                EndpointSetTelegramUserLanguage, request, token, cancellationToken);

        if (response is { Success: true, Data: not null })
        {
        }
        else
        {
            logger.LogWarning($"Failed to set user language: {response?.Message}");
        }

        if (response == null)
        {
            logger.LogError("Failed to fetch user language from API.");
        }

        return response!.Data!;
    }

    public async Task<GetTelegramUserLanguageResponse> GetTelegramUserLanguageAsync(GetTelegramUserLanguageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TelegramId <= 0)
            throw new ArgumentException("TelegramId is required.");

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        logger.LogInformation("Sending request to get user language " +
                              $"TelegramId: {request.TelegramId}");
        
        var url = $"{EndpointGetTelegramUserLanguage}/{request.TelegramId}";
        
        var response =
            await httpRequestService.GetAsync<ApiResponse<GetTelegramUserLanguageResponse>>(
                url, token, cancellationToken);

        if (response is { Success: true, Data: not null })
        {
        }
        else
        {
            logger.LogWarning($"Failed to get VPN servers: {response?.Message}");
        }

        if (response == null)
        {
            logger.LogError("Failed to fetch user language from API.");
        }

        return response!.Data!;
    }
    
    public async Task<IsExistTelegramUserLanguagePreferenceResponse> IsExistTelegramUserLanguagePreferenceAsync(
        IsExistTelegramUserLanguagePreferenceRequest request, CancellationToken cancellationToken)
    {
        if (request.TelegramId <= 0)
            throw new ArgumentException("TelegramId is required.");

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        logger.LogInformation("Sending request to get user language " +
                              $"TelegramId: {request.TelegramId}");
        
        var url = $"{EndpointIsExistTelegramUserLanguagePreference}/{request.TelegramId}";
        
        var response =
            await httpRequestService.GetAsync<ApiResponse<IsExistTelegramUserLanguagePreferenceResponse>>(
                url, token, cancellationToken);

        if (response is { Success: true, Data: not null })
        {
        }
        else
        {
            logger.LogWarning($"Failed to get VPN servers: {response?.Message}");
        }

        if (response == null)
        {
            logger.LogError("Failed to fetch user language from API.");
        }

        return response!.Data!;
    }
    
    public async Task<GetTextForTelegramUserResponse> GetTextForTelegramUser(GetTextForTelegramUserRequest request, 
        CancellationToken cancellationToken)
    {
        if (request.TelegramId <= 0)
            throw new ArgumentException("TelegramId is required.");
        if (string.IsNullOrEmpty(request.Key))
            throw new ArgumentException("Key is required.");

        var token = await authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new AuthenticationException("Authentication failed. Failed to obtain a valid token from API.");
        }

        logger.LogInformation("Sending request to get user language " +
                              $"TelegramId: {request.TelegramId}");
        
        var url = $"{EndpointSetTelegramUserLanguage}/{request.TelegramId}/{request.Key}";
        
        var response =
            await httpRequestService.GetAsync<ApiResponse<GetTextForTelegramUserResponse>>(
                url, token, cancellationToken);

        if (response is { Success: true, Data: not null })
        {
        }
        else
        {
            logger.LogWarning($"Failed to get VPN servers: {response?.Message}");
        }

        if (response == null)
        {
            logger.LogError("Failed to fetch user language from API.");
        }

        return response!.Data!;
    }
}