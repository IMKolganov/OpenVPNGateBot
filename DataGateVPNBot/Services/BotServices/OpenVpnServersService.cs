using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;
using OpenVPNGateMonitor.SharedModels.OpenVpnServers.Responses;

namespace DataGateVPNBot.Services.BotServices;

public class OpenVpnServersService : IOpenVpnServersService
{
    private readonly DashBoardApiServerService _dashBoardApiServerService;
    private readonly ILogger<OvpnFileService> _logger;

    public OpenVpnServersService(DashBoardApiServerService dashBoardApiServerService, ILogger<OvpnFileService> logger)
    {
        _dashBoardApiServerService = dashBoardApiServerService;
        _logger = logger;
    }

    public async Task<List<OpenVpnServerResponse>> GetAllOpenVpnServersListAsync(CancellationToken cancellationToken)
    {
        return await _dashBoardApiServerService.GetOpenVpnServersListAsync(cancellationToken) 
               ?? throw new NullReferenceException("OpenVPN servers list is null");
    }
}