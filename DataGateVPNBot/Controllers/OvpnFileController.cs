using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace DataGateVPNBot.Controllers;

[ApiController]
public class OvpnFileController(IOvpnFileService ovpnFileService, IErrorService errorService,
    ILogger<OvpnFileController> logger) : ControllerBase
{
    /// <summary>
    /// Short endpoint like https://host.ru/{token}
    /// Used by Telegram and OpenVPN Connect
    /// </summary>
    [HttpGet("/DownloadByToken")]
    public async Task<IActionResult> DownloadByToken([FromQuery] string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest("Token is required");

        try
        {
            var response = await ovpnFileService.DownloadOvpnFileByTokenAsync(token, cancellationToken);

            if (response?.Content == null || response.Content.Length == 0)
                return NotFound("OVPN file is empty or not found.");

            var actualFileName = string.IsNullOrWhiteSpace(response.FileName)
                ? $"client_{token}.ovpn"
                : Path.GetFileNameWithoutExtension(response.FileName) + ".ovpn";

            return File(
                fileContents: response.Content,
                contentType: "application/x-openvpn-profile",
                fileDownloadName: actualFileName
            );
        }
        catch (Exception ex)
        {
            await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
            logger.LogError(ex, "Failed to serve OVPN file for token {Token}", token);
            return BadRequest(ApiResponse<DownloadOvpnFileResponse>.ErrorResponse(ex.Message));
        }
    }

    // /// <summary>
    // /// POST /api/OvpnFile/DownloadClientOvpnFile
    // /// </summary>
    // [HttpPost]
    // [Route("api/[controller]/DownloadClientOvpnFile")]
    // public async Task<ActionResult<ApiResponse<DownloadOvpnFileResponse>>> DownloadClientOvpnFile(
    //     [FromBody] DownloadClientOvpnFileRequest request,
    //     CancellationToken cancellationToken)
    // {
    //     try
    //     {
    //         var response = await ovpnFileService.DownloadOvpnFileAsync(request, cancellationToken);
    //         return Ok(ApiResponse<DownloadOvpnFileResponse>.SuccessResponse(response));
    //     }
    //     catch (Exception ex)
    //     {
    //         logger.LogError(ex,
    //             "Failed to download OVPN file {IssuedOvpnFileId} for {VpnServerId}",
    //             request.IssuedOvpnFileId, request.VpnServerId);
    //
    //         return BadRequest(ApiResponse<DownloadOvpnFileResponse>.ErrorResponse(ex.Message));
    //     }
    // }
    //
    // /// <summary>
    // /// GET|HEAD /api/OvpnFile/DownloadClientOvpnFile/{vpnServerId}/{issuedOvpnFileId}/{fileName}
    // /// </summary>
    // [HttpGet]
    // [HttpHead]
    // [Route("api/[controller]/DownloadClientOvpnFile/{vpnServerId:int}/{issuedOvpnFileId:int}/{fileName}")]
    // public async Task<IActionResult> DownloadClientOvpnFileByIds(
    //     [FromRoute] int vpnServerId,
    //     [FromRoute] int issuedOvpnFileId,
    //     [FromRoute] string fileName,
    //     CancellationToken cancellationToken)
    // {
    //     if (vpnServerId <= 0 || issuedOvpnFileId <= 0)
    //         return BadRequest("Both vpnServerId and issuedOvpnFileId must be greater than zero.");
    //
    //     try
    //     {
    //         var request = new DownloadClientOvpnFileRequest
    //         {
    //             VpnServerId = vpnServerId,
    //             IssuedOvpnFileId = issuedOvpnFileId
    //         };
    //
    //         var response = await ovpnFileService.DownloadOvpnFileAsync(request, cancellationToken);
    //
    //         if (response.Content.Length == 0)
    //             return NotFound("OVPN file is empty.");
    //
    //         var actualFileName = string.IsNullOrWhiteSpace(response.FileName)
    //             ? $"client_{issuedOvpnFileId}.ovpn"
    //             : Path.GetFileNameWithoutExtension(response.FileName) + ".ovpn";
    //
    //         return File(
    //             fileContents: response.Content,
    //             contentType: "application/x-openvpn-profile",
    //             fileDownloadName: actualFileName
    //         );
    //     }
    //     catch (Exception ex)
    //     {
    //         logger.LogError(ex,
    //             "Failed to serve OVPN file for issuedOvpnFileId={IssuedOvpnFileId}, vpnServerId={VpnServerId}",
    //             issuedOvpnFileId, vpnServerId);
    //
    //         return BadRequest(ApiResponse<DownloadOvpnFileResponse>.ErrorResponse(ex.Message));
    //     }
    // }
}
