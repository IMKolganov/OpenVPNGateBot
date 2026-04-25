using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses.Dto;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.BotServices.Interfaces;

/// <summary>Telegram-side flows for Xray (VLESS) client links via dashboard <c>api/xray-client-links</c> (same DTOs as OpenVPN exports).</summary>
public interface IXrayClientLinkBotService
{
    Task<List<IssuedOvpnFileDto>> GetAllOvpnFilesListAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<DownloadFileResponse> DownloadOvpnFileByTokenAsync(string token,
        CancellationToken cancellationToken);
    Task<List<IAlbumInputMedia>> GetOvpnFilesAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<List<IAlbumInputMedia>> GetOvpnFilesWithTokenAsync(int vpnServerId, long telegramId, string hostUrl,
        CancellationToken cancellationToken);
    Task<string> GetClientLinksTextWithTokenAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<List<IAlbumInputMedia>> MakeOvpnFileAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<List<IAlbumInputMedia>> MakeOvpnFileWithTokenAsync(int vpnServerId, long telegramId, string hostUrl,
        CancellationToken cancellationToken);
    Task<string> MakeClientLinkTextWithTokenAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<bool> RevokeAllOvpnFileAsync(int vpnServerId, long telegramId, CancellationToken cancellationToken);
    Task<bool> RevokeOvpnFileAsync(int vpnServerId, long telegramId, string fileName, CancellationToken cancellationToken);
    Task<bool> CheckMaxCountOvpnFilesForClient(int vpnServerId, long telegramId,
        CancellationToken cancellationToken, int maxCountFiles = 10);
}
