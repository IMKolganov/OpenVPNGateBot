
using System.Text;
using Telegram.Bot.Types;
using DataGateVPNBot.Models.DashBoardApi;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;

namespace DataGateVPNBot.Services.BotServices;

public class OpenVpnServersService : IOpenVpnServersService
{
    private readonly DashBoardApiOvpnFileService _dashBoardApiOvpnFileService;
    private readonly ILogger<OvpnFileService> _logger;

    public OpenVpnServersService(DashBoardApiOvpnFileService dashBoardApiOvpnFileService, ILogger<OvpnFileService> logger)
    {
        _dashBoardApiOvpnFileService = dashBoardApiOvpnFileService;
        _logger = logger;
    }

    public async Task<List<IssuedOvpnFileResponse>> GetAllOpenVpnServersListAsync(int vpnServerId, long userId,
        CancellationToken cancellationToken)
    {
        var issuedOvpnFileResponses = await _dashBoardApiOvpnFileService.GetAllOvpnFilesByExternalIdAsync(
            vpnServerId, userId.ToString(), cancellationToken);
        issuedOvpnFileResponses = issuedOvpnFileResponses?.Where(x => !x.IsRevoked).ToList() ??
                                  new List<IssuedOvpnFileResponse>();
        
        return issuedOvpnFileResponses;
    }
}