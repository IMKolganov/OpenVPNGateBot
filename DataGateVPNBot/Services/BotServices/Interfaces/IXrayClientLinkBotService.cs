using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses.Dto;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.BotServices.Interfaces;

/// <summary>Telegram-side flows for Xray (VLESS) client links via dashboard <c>api/v2/xray-client-links</c>.</summary>
public interface IXrayClientLinkBotService
{
    Task<List<IssuedXrayClientLinkDto>> GetAllClientLinksListAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<DownloadXrayClientLinkResponse> DownloadClientLinkByTokenAsync(string token,
        CancellationToken cancellationToken);
    Task<List<IAlbumInputMedia>> GetClientLinksAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<List<IAlbumInputMedia>> GetClientLinksWithTokenAsync(int vpnServerId, long telegramId, string hostUrl,
        CancellationToken cancellationToken);
    Task<List<(string FileName, string Text)>> GetClientLinkItemsWithTokenAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<string> GetClientLinksTextWithTokenAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<List<IAlbumInputMedia>> MakeClientLinkAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<List<IAlbumInputMedia>> MakeClientLinkWithTokenAsync(int vpnServerId, long telegramId, string hostUrl,
        CancellationToken cancellationToken);
    Task<(string FileName, string Text)?> MakeClientLinkItemWithTokenAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<string> MakeClientLinkTextWithTokenAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<bool> RevokeAllClientLinksAsync(int vpnServerId, long telegramId, CancellationToken cancellationToken);
    Task<bool> RevokeClientLinkAsync(int vpnServerId, long telegramId, string fileName, CancellationToken cancellationToken);
    Task<bool> CheckMaxCountClientLinksForClient(int vpnServerId, long telegramId,
        CancellationToken cancellationToken, int maxCountFiles = 10);
}
