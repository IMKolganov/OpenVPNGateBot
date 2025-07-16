using DataGateVPNBot.Services.BotServices.Interfaces;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OvpnFileController(IOvpnFileService ovpnFileService, ILogger<OvpnFileController> logger) : ControllerBase
{
    [HttpPost("DownloadClientOvpnFile")]
    public async Task<ActionResult<ApiResponse<DownloadOvpnFileResponse>>> DownloadClientOvpnFile(
        [FromBody] DownloadClientOvpnFileRequest request,
        CancellationToken cancellationToken)
    {
        //todo: make links list with life around 1 hour with GUID
        try
        {
            var response = await ovpnFileService.DownloadOvpnFileAsync(request, cancellationToken);
            return Ok(ApiResponse<DownloadOvpnFileResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to download OVPN file {IssuedOvpnFileId} for {VpnServerId}",
                request.IssuedOvpnFileId, request.VpnServerId);

            return BadRequest(ApiResponse<DownloadOvpnFileResponse>.ErrorResponse(ex.Message));
        }
    }
    
    [HttpGet("DownloadClientOvpnFile/{vpnServerId:int}/{issuedOvpnFileId:int}/{fileName}")]
    public async Task<IActionResult> DownloadClientOvpnFile(
        [FromRoute] int vpnServerId,
        [FromRoute] int issuedOvpnFileId,
        [FromRoute] string fileName, // not used
        CancellationToken cancellationToken)
    {
        if (vpnServerId <= 0 || issuedOvpnFileId <= 0)
            return BadRequest("Both vpnServerId and issuedOvpnFileId must be greater than zero.");

        try
        {
            var request = new DownloadClientOvpnFileRequest
            {
                VpnServerId = vpnServerId,
                IssuedOvpnFileId = issuedOvpnFileId
            };

            var response = await ovpnFileService.DownloadOvpnFileAsync(request, cancellationToken);

            if (response.Content.Length == 0)
                return NotFound("OVPN file is empty.");

            var actualFileName = string.IsNullOrWhiteSpace(response.FileName)
                ? $"client_{issuedOvpnFileId}.ovpn"
                : Path.GetFileNameWithoutExtension(response.FileName) + ".ovpn";

            return File(
                fileContents: response.Content,
                contentType: "application/x-openvpn-profile",
                fileDownloadName: actualFileName
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to serve OVPN file for issuedOvpnFileId={IssuedOvpnFileId}, vpnServerId={VpnServerId}",
                issuedOvpnFileId, vpnServerId);

            return BadRequest(ApiResponse<DownloadOvpnFileResponse>.ErrorResponse(ex.Message));
        }
    }
}