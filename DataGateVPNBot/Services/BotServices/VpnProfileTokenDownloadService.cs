using DataGateVPNBot.Services.BotServices.Interfaces;

namespace DataGateVPNBot.Services.BotServices;

public sealed class VpnProfileTokenDownloadService(
    IOvpnFileService openVpnFileService,
    IXrayClientLinkBotService xrayClientLinkBotService,
    ILogger<VpnProfileTokenDownloadService> logger) : IVpnProfileTokenDownloadService
{
    public async Task<VpnProfileDownload> DownloadByTokenAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            var ovpn = await openVpnFileService.DownloadOvpnFileByTokenAsync(token, cancellationToken);
            return new VpnProfileDownload
            {
                FileName = ovpn.IssuedOvpn.FileName,
                Content = ovpn.Content ?? []
            };
        }
        catch (FileNotFoundException ex)
        {
            logger.LogDebug(ex, "Token not found on OpenVPN files API; trying Xray client links.");
            var xray = await xrayClientLinkBotService.DownloadClientLinkByTokenAsync(token, cancellationToken);
            return new VpnProfileDownload
            {
                FileName = xray.IssuedXrayClientLink.FileName,
                Content = xray.Content ?? []
            };
        }
    }
}
