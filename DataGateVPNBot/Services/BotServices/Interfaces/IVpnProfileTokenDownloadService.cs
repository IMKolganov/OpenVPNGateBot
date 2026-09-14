namespace DataGateVPNBot.Services.BotServices.Interfaces;

/// <summary>
/// Resolves a public download token against the dashboard: OpenVPN-issued files first, then Xray client links.
/// </summary>
public interface IVpnProfileTokenDownloadService
{
    Task<VpnProfileDownload> DownloadByTokenAsync(string token, CancellationToken cancellationToken);
}
