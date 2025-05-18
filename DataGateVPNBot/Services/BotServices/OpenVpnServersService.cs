using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServers.Responses;

namespace DataGateVPNBot.Services.BotServices;

public class OpenVpnServersService : IOpenVpnServersService
{
    private readonly ServerService _serverService;
    private readonly ILogger<OvpnFileService> _logger;

    public OpenVpnServersService(ServerService serverService, ILogger<OvpnFileService> logger)
    {
        _serverService = serverService;
        _logger = logger;
    }

    public async Task<List<OpenVpnServerResponse>> GetAllOpenVpnServersListAsync(CancellationToken cancellationToken)
    {
        return await _serverService.GetOpenVpnServersListAsync(cancellationToken) 
               ?? throw new NullReferenceException("OpenVPN servers list is null");
    }
}