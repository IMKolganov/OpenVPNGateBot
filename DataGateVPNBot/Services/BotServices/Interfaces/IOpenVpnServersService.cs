using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace DataGateVPNBot.Services.BotServices.Interfaces;

public interface IOpenVpnServersService
{
    Task<OpenVpnServersResponse> GetAllOpenVpnServersListAsync(CancellationToken cancellationToken);
}