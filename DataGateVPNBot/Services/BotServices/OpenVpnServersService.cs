using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

namespace DataGateVPNBot.Services.BotServices;

public class OpenVpnServersService(ServerService serverService, ILogger<OvpnFileService> logger)
    : IOpenVpnServersService
{
    private readonly ILogger<OvpnFileService> _logger = logger;

    public async Task<VpnServersResponse> GetAllOpenVpnServersListAsync(CancellationToken cancellationToken)
    {
        return await serverService.GetOpenVpnServersListAsync(cancellationToken) 
               ?? throw new NullReferenceException("OpenVPN servers list is null");
    }
}