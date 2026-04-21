using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotLocalization.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.TelegramBotLocalization.Responses;

namespace DataGateVPNBot.Services.DashboardServices.Interfaces;

public interface ILocalizationService
{
    Task<SetTelegramUserLanguageResponse> SetTelegramUserLanguageAsync(
        SetTelegramUserLanguageRequest request, CancellationToken cancellationToken);
    Task<GetTelegramUserLanguageResponse> GetTelegramUserLanguageAsync(
        GetTelegramUserLanguageRequest request,
        CancellationToken cancellationToken);
    Task<IsExistTelegramUserLanguagePreferenceResponse> IsExistTelegramUserLanguagePreferenceAsync(
        IsExistTelegramUserLanguagePreferenceRequest request, CancellationToken cancellationToken);
    Task<GetTextForTelegramUserResponse> GetTextForTelegramUser(GetTextForTelegramUserRequest request,
        CancellationToken cancellationToken);
}