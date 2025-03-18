using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.BotServices.Interfaces;

public interface IOvpnFileService
{
    Task<List<IAlbumInputMedia>> GetOvpnFilesAsync(int vpnServerId, long userId,
        CancellationToken cancellationToken);
    Task<InputFile?> MakeOvpnFileAsync(int vpnServerId, long userId,
        CancellationToken cancellationToken);
}