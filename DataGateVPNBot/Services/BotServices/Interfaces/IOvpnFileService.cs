using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.BotServices.Interfaces;

public interface IOvpnFileService
{
    Task<List<IssuedOvpnFileDto>> GetAllOvpnFilesListAsync(int vpnServerId, long telegramId, 
        CancellationToken cancellationToken);
    Task<DownloadFileResponse> DownloadOvpnFileByTokenAsync(ByTokenRequest token,
        CancellationToken cancellationToken);
    Task<List<IAlbumInputMedia>> GetOvpnFilesAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<List<IAlbumInputMedia>> GetOvpnFilesWithTokenAsync(int vpnServerId, long telegramId, string hostUrl,
        CancellationToken cancellationToken);
    Task<List<IAlbumInputMedia>> MakeOvpnFileAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<List<IAlbumInputMedia>> MakeOvpnFileWithTokenAsync(int vpnServerId, long telegramId, string hostUrl,
        CancellationToken cancellationToken);
    Task<bool> RevokeAllOvpnFileAsync(int vpnServerId, long telegramId, CancellationToken cancellationToken);
    Task<bool> RevokeOvpnFileAsync(int vpnServerId, long telegramId, string fileName, CancellationToken cancellationToken);
    Task<bool> CheckMaxCountOvpnFilesForClient(int vpnServerId, long telegramId,
        CancellationToken cancellationToken, int maxCountFiles = 10);
}