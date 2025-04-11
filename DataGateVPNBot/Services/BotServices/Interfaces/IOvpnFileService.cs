using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.BotServices.Interfaces;

public interface IOvpnFileService
{
    Task<List<OvpnFileResponse>> GetAllOvpnFilesListAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<List<IAlbumInputMedia>> GetOvpnFilesAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<InputFile?> MakeOvpnFileAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken);
    Task<bool> RevokeAllOvpnFileAsync(int vpnServerId, long telegramId, CancellationToken cancellationToken);
    Task<bool> RevokeOvpnFileAsync(int vpnServerId, long telegramId, string fileName, CancellationToken cancellationToken);

}