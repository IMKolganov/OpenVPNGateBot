using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.OpenVpnFiles.Responses;

namespace DataGateVPNBot.Services.BotServices;

public sealed class VpnProfileTokenDownloadService(
    IOvpnFileService openVpnFileService,
    IXrayClientLinkBotService xrayClientLinkBotService,
    ILogger<VpnProfileTokenDownloadService> logger) : IVpnProfileTokenDownloadService
{
    public async Task<DownloadFileResponse> DownloadByTokenAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            return await openVpnFileService.DownloadOvpnFileByTokenAsync(token, cancellationToken);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogDebug(ex, "Token not found on OpenVPN files API; trying Xray client links.");
            return await xrayClientLinkBotService.DownloadOvpnFileByTokenAsync(token, cancellationToken);
        }
    }
}
