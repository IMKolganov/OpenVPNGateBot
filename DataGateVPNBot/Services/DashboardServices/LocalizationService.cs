using System.Security.Authentication;
using DataGateVPNBot.Services.DashboardServices.Interfaces;
using DataGateVPNBot.Services.Http;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotLocalization.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotLocalization.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Services.DashboardServices;

public class LocalizationService(ILogger<LocalizationService> logger, IHttpRequestService httpRequestService,
    AuthService authService) : ILocalizationService
{
    private const string EndpointSetTelegramUserLanguage = "api/tgbot-localizations/set-tg-user-language";
    private const string EndpointGetTelegramUserLanguage = "api/tgbot-localizations/get-tg-user-language";
    private const string EndpointIsExistTelegramUserLanguagePreference = 
        "api/tgbot-localizations/is-exist-tg-user-language-preference";
    private const string EndpointGetTextForTelegramUser = "api/tgbot-localizations/get-text-for-tg-user";

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
            logger.LogWarning($"Failed to get user language: {response?.Message}");
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
            logger.LogWarning($"Failed to get user language: {response?.Message}");
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
        
        var url = $"{EndpointGetTextForTelegramUser}/{request.TelegramId}/{request.Key}";
        
        var response =
            await httpRequestService.GetAsync<ApiResponse<GetTextForTelegramUserResponse>>(
                url, token, cancellationToken);

        if (response is { Success: true, Data: not null })
        {
        }
        else
        {
            logger.LogWarning($"Failed to get text for user language: {response?.Message}");
        }

        if (response == null)
        {
            logger.LogError("Failed to fetch text for user from API.");
        }

        return response!.Data!;
    }
}