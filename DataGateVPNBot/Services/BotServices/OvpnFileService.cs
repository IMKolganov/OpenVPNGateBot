using System.Text;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;
using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Requests;
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

    public async Task<List<OvpnFileResponse>> GetAllOvpnFilesListAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        var getAllByExternalIdOvpnFilesRequest = new GetAllByExternalIdOvpnFilesRequest()
        {
            VpnServerId = vpnServerId,  ExternalId = telegramId.ToString()
        };
        var issuedOvpnFileResponses =
            await _dashBoardApiOvpnFileService.GetAllOvpnFilesByExternalIdAsync(
                getAllByExternalIdOvpnFilesRequest, cancellationToken);
        issuedOvpnFileResponses = issuedOvpnFileResponses?.Where(x => !x.IsRevoked).ToList() ??
                                  new List<OvpnFileResponse>();
        
        return issuedOvpnFileResponses;
    }

    public async Task<List<IAlbumInputMedia>> GetOvpnFilesAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        var getAllByExternalIdOvpnFilesRequest = new GetAllByExternalIdOvpnFilesRequest()
        {
            VpnServerId = vpnServerId, ExternalId = telegramId.ToString()
        };
        _logger.LogInformation($"Fetching OVPN files for telegramId: {telegramId}, ServerId: {vpnServerId}");

        var issuedOvpnFileResponses =
            await _dashBoardApiOvpnFileService.GetAllOvpnFilesByExternalIdAsync(
                getAllByExternalIdOvpnFilesRequest, cancellationToken);

        issuedOvpnFileResponses = issuedOvpnFileResponses?.Where(x => !x.IsRevoked).ToList() ??
                                  new List<OvpnFileResponse>();

        if (!issuedOvpnFileResponses.Any())
        {
            _logger.LogInformation("No valid OVPN files found.");
            return new List<IAlbumInputMedia>();
        }

        var mediaGroupOpenVpnFiles = new List<IAlbumInputMedia>();

        foreach (var issuedOvpnFileResponse in issuedOvpnFileResponses)
        {
            try
            {
                _logger.LogInformation(
                    $"Processing file: {issuedOvpnFileResponse.FileName}, " +
                    $"ServerId: {issuedOvpnFileResponse.VpnServerId}, FileId: {issuedOvpnFileResponse.Id}");
                var downloadOvpnFileRequest = new DownloadOvpnFileRequest()
                {
                    VpnServerId = issuedOvpnFileResponse.VpnServerId, IssuedOvpnFileId = issuedOvpnFileResponse.Id
                };

                var issuedOvpnFileStream = await _dashBoardApiOvpnFileService.DownloadOvpnFileByIdAndServerIdAsync(
                    downloadOvpnFileRequest, cancellationToken);

                var inputFile = new InputFileStream(issuedOvpnFileStream, issuedOvpnFileResponse.FileName);
                var media = new InputMediaDocument(inputFile)
                {
                    Caption = issuedOvpnFileResponse.FileName
                };
                mediaGroupOpenVpnFiles.Add(media);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error processing file {FileName}: {ErrorMessage}", issuedOvpnFileResponse.FileName,
                    ex.Message);

                var errorMessage = new StringBuilder()
                    .AppendLine($"Error processing file: {issuedOvpnFileResponse.FileName}")
                    .AppendLine($"ServerId: {issuedOvpnFileResponse.VpnServerId}")
                    .AppendLine($"FileId: {issuedOvpnFileResponse.Id}")
                    .AppendLine($"Error: {ex.Message}")
                    .AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                    .ToString();

                var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));
                var errorFile = new InputFileStream(errorStream, $"{issuedOvpnFileResponse.FileName}.error.txt");

                var errorMedia = new InputMediaDocument(errorFile)
                {
                    Caption = $"Error file: {issuedOvpnFileResponse.FileName}"
                };

                mediaGroupOpenVpnFiles.Add(errorMedia);
            }
        }

        return mediaGroupOpenVpnFiles;
    }

    public async Task<InputFile?> MakeOvpnFileAsync(int vpnServerId, long telegramId, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Creating OVPN file for user: {telegramId}, ServerId: {vpnServerId}");

        var addOvpnFileRequest = new AddOvpnFileRequest
        {
            VpnServerId = vpnServerId,
            CommonName = await MakeCommonNameForOvpnFileAsync(vpnServerId, telegramId, cancellationToken),
            ExternalId = telegramId.ToString(),
            IssuedTo = $"telegram user {telegramId}"
        };

        var addOvpnFileResponse = await _dashBoardApiOvpnFileService.AddOvpnFileAsync(addOvpnFileRequest, cancellationToken);

        if (addOvpnFileResponse?.OvpnFile == null || string.IsNullOrEmpty(addOvpnFileResponse.OvpnFile.FullName))
        {
            _logger.LogWarning($"Failed to request OVPN file creation for user {telegramId} on server {vpnServerId}.");
            return null;
        }

        var issuedOvpnFile = addOvpnFileResponse.IssuedOvpnFile;

        _logger.LogInformation($"Downloading newly created OVPN file: {issuedOvpnFile.FileName}");

        try
        {
            using var issuedOvpnFileStream = File.OpenRead(addOvpnFileResponse.OvpnFile.FullName);
            var inputFile = new InputFileStream(issuedOvpnFileStream, issuedOvpnFile.FileName);
            return inputFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open OVPN file: {FilePath}", addOvpnFileResponse.OvpnFile.FullName);
            return null;
        }
    }

    public async Task<bool> RevokeAllOvpnFileAsync(int vpnServerId, long telegramId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        // _logger.LogInformation("Revoking all OVPN files for telegramId: {telegramId}, ServerId: {VpnServerId}", telegramId, vpnServerId);
        //
        // var issuedOvpnFileResponses = await _dashBoardApiOvpnFileService.GetAllOvpnFilesByExternalIdAsync(
        //     vpnServerId, telegramId.ToString(), cancellationToken);
        //
        // if (issuedOvpnFileResponses == null || !issuedOvpnFileResponses.Any())
        // {
        //     _logger.LogWarning("No OVPN files found to revoke for telegramId {telegramId} on server {VpnServerId}.", telegramId, vpnServerId);
        //     return false;
        // }
        //
        // var success = true;
        // foreach (var file in issuedOvpnFileResponses)
        // {
        //     var revoked = await _dashBoardApiOvpnFileService.RevokeOvpnFileAsync(telegramId.ToString(), file.CommonName, vpnServerId, cancellationToken);
        //     if (!revoked)
        //     {
        //         _logger.LogError("Failed to revoke OVPN file: {FileName} for telegramId {telegramId} on server {VpnServerId}", 
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
        // _logger.LogInformation("Revoking OVPN file {FileName} for telegramId {telegramId}, ServerId: {VpnServerId}", 
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

    private async Task<string> MakeCommonNameForOvpnFileAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken, int maxCountFiles = 10)
    {
        var files = await GetAllOvpnFilesListAsync(vpnServerId, telegramId, cancellationToken);

        var prefix = $"tg-{vpnServerId}-{telegramId}-";
        var usedNames = files
            .Where(f => f.CommonName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.CommonName)
            .ToHashSet();

        for (int i = 0; i < maxCountFiles; i++)
        {
            var candidate = $"{prefix}{i}";
            if (!usedNames.Contains(candidate))
            {
                return candidate;
            }
        }

        throw new Exception($"No available CommonName for Telegram ID {telegramId}. Limit of 10 reached.");
    }

}