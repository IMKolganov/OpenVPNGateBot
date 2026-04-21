using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

namespace DataGateVPNBot.Services.BotServices.Interfaces;

public interface IOpenVpnServersService
{
    Task<VpnServersResponse> GetAllOpenVpnServersListAsync(CancellationToken cancellationToken);
}