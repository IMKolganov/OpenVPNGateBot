using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;
using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.BotServices;

public class OvpnFileService : IOvpnFileService
{
    private readonly DashBoardApiOvpnFileService _dashBoardApiOvpnFileService;
    private readonly ILogger<OvpnFileService> _logger;

    public OvpnFileService(DashBoardApiOvpnFileService dashBoardApiOvpnFileService, ILogger<OvpnFileService> logger)
    {
        _dashBoardApiOvpnFileService = dashBoardApiOvpnFileService;
        _logger = logger;
    }

    public async Task<List<OvpnFileResponse>> GetAllOvpnFilesListAsync(int vpnServerId, long userId,
        CancellationToken cancellationToken)
    {
        var issuedOvpnFileResponses = await _dashBoardApiOvpnFileService.GetAllOvpnFilesByExternalIdAsync(
            vpnServerId, userId.ToString(), cancellationToken);
        issuedOvpnFileResponses = issuedOvpnFileResponses?.Where(x => !x.IsRevoked).ToList() ??
                                  new List<OvpnFileResponse>();
        
        return issuedOvpnFileResponses;
    }

    public async Task<List<IAlbumInputMedia>> GetOvpnFilesAsync(int vpnServerId, long userId,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        // _logger.LogInformation($"Fetching OVPN files for user: {userId}, ServerId: {vpnServerId}");
        //
        // var issuedOvpnFileResponses = await _dashBoardApiOvpnFileService.GetAllOvpnFilesByExternalIdAsync(
        //     vpnServerId, userId.ToString(), cancellationToken);
        //
        // issuedOvpnFileResponses = issuedOvpnFileResponses?.Where(x => !x.IsRevoked).ToList() ??
        //                           new List<IssuedOvpnFileResponse>();
        //
        // if (!issuedOvpnFileResponses.Any())
        // {
        //     _logger.LogInformation("No valid OVPN files found.");
        //     return new List<IAlbumInputMedia>();
        // }
        //
        // var mediaGroupOpenVpnFiles = new List<IAlbumInputMedia>();
        //
        // foreach (var issuedOvpnFileResponse in issuedOvpnFileResponses)
        // {
        //     try
        //     {
        //         _logger.LogInformation(
        //             $"Processing file: {issuedOvpnFileResponse.FileName}, " +
        //             $"ServerId: {issuedOvpnFileResponse.ServerId}, FileId: {issuedOvpnFileResponse.Id}");
        //
        //         var issuedOvpnFileStream = await _dashBoardApiOvpnFileService.DownloadOvpnFileByIdAndServerIdAsync(
        //             issuedOvpnFileResponse.Id, issuedOvpnFileResponse.ServerId, cancellationToken);
        //
        //         var inputFile = new InputFileStream(issuedOvpnFileStream, issuedOvpnFileResponse.FileName);
        //         var media = new InputMediaDocument(inputFile)
        //         {
        //             Caption = issuedOvpnFileResponse.FileName
        //         };
        //         mediaGroupOpenVpnFiles.Add(media);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError("Error processing file {FileName}: {ErrorMessage}", issuedOvpnFileResponse.FileName,
        //             ex.Message);
        //
        //         var errorMessage = new StringBuilder()
        //             .AppendLine($"Error processing file: {issuedOvpnFileResponse.FileName}")
        //             .AppendLine($"ServerId: {issuedOvpnFileResponse.ServerId}")
        //             .AppendLine($"FileId: {issuedOvpnFileResponse.Id}")
        //             .AppendLine($"Error: {ex.Message}")
        //             .AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
        //             .ToString();
        //
        //         var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));
        //         var errorFile = new InputFileStream(errorStream, $"{issuedOvpnFileResponse.FileName}.error.txt");
        //
        //         var errorMedia = new InputMediaDocument(errorFile)
        //         {
        //             Caption = $"Error file: {issuedOvpnFileResponse.FileName}"
        //         };
        //
        //         mediaGroupOpenVpnFiles.Add(errorMedia);
        //     }
        // }
        //
        // return mediaGroupOpenVpnFiles;
    }

    public async Task<InputFile?> MakeOvpnFileAsync(int vpnServerId, long userId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        // _logger.LogInformation($"Creating OVPN file for user: {userId}, ServerId: {vpnServerId}");
        //
        // var success = await _dashBoardApiOvpnFileService.AddOvpnFileAsync(
        //     userId.ToString(), $"user-{userId}", vpnServerId, cancellationToken);
        //
        // if (!success)
        // {
        //     _logger.LogWarning("Failed to request OVPN file creation for user {UserId} on server {VpnServerId}.",
        //         userId, vpnServerId);
        //     return null;
        // }
        //
        // var issuedOvpnFileResponses = 
        //     await _dashBoardApiOvpnFileService.GetAllOvpnFilesByExternalIdAsync(
        //     vpnServerId, userId.ToString(), cancellationToken);
        //
        // var issuedOvpnFile = issuedOvpnFileResponses?.FirstOrDefault(x => !x.IsRevoked);
        // if (issuedOvpnFile == null)
        // {
        //     _logger.LogWarning("No valid OVPN file found for user {UserId} on server {VpnServerId} after creation.",
        //         userId, vpnServerId);
        //     return null;
        // }
        //
        // _logger.LogInformation("Downloading newly created OVPN file: {FileName}", issuedOvpnFile.FileName);
        //
        // var issuedOvpnFileStream = await _dashBoardApiOvpnFileService.DownloadOvpnFileByIdAndServerIdAsync(
        //     issuedOvpnFile.Id, issuedOvpnFile.ServerId, cancellationToken);
        //
        // var inputFile = new InputFileStream(issuedOvpnFileStream, issuedOvpnFile.FileName);
        // return inputFile;
    }

    public async Task<bool> RevokeAllOvpnFileAsync(int vpnServerId, long telegramId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        // _logger.LogInformation("Revoking all OVPN files for user: {UserId}, ServerId: {VpnServerId}", telegramId, vpnServerId);
        //
        // var issuedOvpnFileResponses = await _dashBoardApiOvpnFileService.GetAllOvpnFilesByExternalIdAsync(
        //     vpnServerId, telegramId.ToString(), cancellationToken);
        //
        // if (issuedOvpnFileResponses == null || !issuedOvpnFileResponses.Any())
        // {
        //     _logger.LogWarning("No OVPN files found to revoke for user {UserId} on server {VpnServerId}.", telegramId, vpnServerId);
        //     return false;
        // }
        //
        // var success = true;
        // foreach (var file in issuedOvpnFileResponses)
        // {
        //     var revoked = await _dashBoardApiOvpnFileService.RevokeOvpnFileAsync(telegramId.ToString(), file.CommonName, vpnServerId, cancellationToken);
        //     if (!revoked)
        //     {
        //         _logger.LogError("Failed to revoke OVPN file: {FileName} for user {UserId} on server {VpnServerId}", 
        //             file.FileName, telegramId, vpnServerId);
        //         success = false;
        //     }
        // }
        //
        // return success;
    }
    
    public async Task<bool> RevokeOvpnFileAsync(int vpnServerId, long telegramId, string fileName, 
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        // _logger.LogInformation("Revoking OVPN file {FileName} for user {UserId}, ServerId: {VpnServerId}", 
        //     fileName, telegramId, vpnServerId);
        //
        // var issuedOvpnFileResponses = await _dashBoardApiOvpnFileService.GetAllOvpnFilesByExternalIdAsync(
        //     vpnServerId, telegramId.ToString(), cancellationToken);
        //
        // var fileToRevoke = issuedOvpnFileResponses?.FirstOrDefault(f => f.FileName == fileName);
        // if (fileToRevoke == null)
        // {
        //     _logger.LogWarning("OVPN file {FileName} not found for user {telegramId} on server {VpnServerId}.", 
        //         fileName, telegramId, vpnServerId);
        //     return false;
        // }
        //
        // return await _dashBoardApiOvpnFileService.RevokeOvpnFileAsync(telegramId.ToString(), fileToRevoke.CommonName, vpnServerId, cancellationToken);
    }

}