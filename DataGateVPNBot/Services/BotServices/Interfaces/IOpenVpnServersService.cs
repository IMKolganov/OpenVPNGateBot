using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace DataGateVPNBot.Services.BotServices.Interfaces;

public interface IOpenVpnServersService
{
    Task<List<OpenVpnServerResponse>> GetAllOpenVpnServersListAsync(CancellationToken cancellationToken);
}