using System.Text;
using DataGateVPNBot.Services.BotServices.Interfaces;
using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.OpenVpnFiles.Responses;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.BotServices;

public class OvpnFileService : IOvpnFileService
{
    private readonly DashboardServices.OvpnFileService _ovpnFileService;
    private readonly ILogger<OvpnFileService> _logger;

    public OvpnFileService(DashboardServices.OvpnFileService ovpnFileService, ILogger<OvpnFileService> logger)
    {
        _ovpnFileService = ovpnFileService;
        _logger = logger;
    }

    public async Task<List<OvpnFileResponse>> GetAllOvpnFilesListAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        var getAllByExternalIdOvpnFilesRequest = new GetAllByExternalIdOvpnFilesRequest()
        {
            VpnServerId = vpnServerId, ExternalId = telegramId.ToString()
        };
        var issuedOvpnFileResponses =
            await _ovpnFileService.GetAllOvpnFilesByExternalIdAsync(
                getAllByExternalIdOvpnFilesRequest, cancellationToken);
        issuedOvpnFileResponses = issuedOvpnFileResponses?.Where(x =>
            !x.IssuedOvpnFile.IsRevoked).ToList() ?? [];

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
            await _ovpnFileService.GetAllOvpnFilesByExternalIdAsync(
                getAllByExternalIdOvpnFilesRequest, cancellationToken);

        issuedOvpnFileResponses = issuedOvpnFileResponses?.Where(x =>
            !x.IssuedOvpnFile.IsRevoked).ToList() ?? [];

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
                    $"Processing file: {issuedOvpnFileResponse.IssuedOvpnFile.FileName}, " +
                    $"ServerId: {issuedOvpnFileResponse.IssuedOvpnFile.VpnServerId}, " +
                    $"FileId: {issuedOvpnFileResponse.IssuedOvpnFile.Id}");
                var downloadOvpnFileRequest = new DownloadOvpnFileRequest()
                {
                    VpnServerId = issuedOvpnFileResponse.IssuedOvpnFile.VpnServerId,
                    IssuedOvpnFileId = issuedOvpnFileResponse.IssuedOvpnFile.Id
                };

                var issuedOvpnFileStream = await _ovpnFileService.DownloadOvpnFileByIdAndServerIdAsync(
                    downloadOvpnFileRequest, cancellationToken);

                var inputFile = new InputFileStream(issuedOvpnFileStream,
                    issuedOvpnFileResponse.IssuedOvpnFile.FileName);
                var media = new InputMediaDocument(inputFile)
                {
                    Caption = issuedOvpnFileResponse.IssuedOvpnFile.FileName
                };
                mediaGroupOpenVpnFiles.Add(media);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing file " +
                                 $"{issuedOvpnFileResponse.IssuedOvpnFile.FileName}: {ex.Message}");

                var errorMessage = new StringBuilder()
                    .AppendLine($"Error processing file: {issuedOvpnFileResponse.IssuedOvpnFile.FileName}")
                    .AppendLine($"ServerId: {issuedOvpnFileResponse.IssuedOvpnFile.VpnServerId}")
                    .AppendLine($"FileId: {issuedOvpnFileResponse.IssuedOvpnFile.Id}")
                    .AppendLine($"Error: {ex.Message}")
                    .AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                    .ToString();

                var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));
                var errorFile = new InputFileStream(errorStream,
                    $"{issuedOvpnFileResponse.IssuedOvpnFile.FileName}.error.txt");

                var errorMedia = new InputMediaDocument(errorFile)
                {
                    Caption = $"Error file: {issuedOvpnFileResponse.IssuedOvpnFile.FileName}"
                };

                mediaGroupOpenVpnFiles.Add(errorMedia);
            }
        }

        return mediaGroupOpenVpnFiles;
    }

    public async Task<List<IAlbumInputMedia>> MakeOvpnFileAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        var mediaGroupOpenVpnFiles = new List<IAlbumInputMedia>();
        _logger.LogInformation($"Creating OVPN file for telegramId: {telegramId}, ServerId: {vpnServerId}");

        var addOvpnFileRequest = new AddOvpnFileRequest
        {
            VpnServerId = vpnServerId,
            CommonName = await MakeCommonNameForOvpnFileAsync(vpnServerId, telegramId, cancellationToken),
            ExternalId = telegramId.ToString(),
            IssuedTo = $"telegram user {telegramId}"
        };

        var addOvpnFileResponse =
            await _ovpnFileService.AddOvpnFileAsync(addOvpnFileRequest, cancellationToken);

        if (addOvpnFileResponse?.IssuedOvpnFile == null)
        {
            _logger.LogWarning($"Failed to create OVPN file for telegramId: {telegramId}, ServerId: {vpnServerId}");
            return mediaGroupOpenVpnFiles;
        }

        var issuedOvpnFile = addOvpnFileResponse.IssuedOvpnFile;
        try
        {
            _logger.LogInformation(
                $"Downloading newly created file: {issuedOvpnFile.FileName}, " +
                $"ServerId: {issuedOvpnFile.VpnServerId}, FileId: {issuedOvpnFile.Id}");

            var downloadRequest = new DownloadOvpnFileRequest
            {
                VpnServerId = issuedOvpnFile.VpnServerId,
                IssuedOvpnFileId = issuedOvpnFile.Id
            };
            var issuedOvpnFileStream = await _ovpnFileService.DownloadOvpnFileByIdAndServerIdAsync(
                downloadRequest, cancellationToken);

            var inputFile = new InputFileStream(issuedOvpnFileStream, issuedOvpnFile.FileName);
            var media = new InputMediaDocument(inputFile)
            {
                Caption = issuedOvpnFile.FileName
            };
            mediaGroupOpenVpnFiles.Add(media);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error processing file {FileName}: {ErrorMessage}", issuedOvpnFile.FileName,
                ex.Message);

            var errorMessage = new StringBuilder()
                .AppendLine($"Error downloading file: {issuedOvpnFile.FileName}")
                .AppendLine($"ServerId: {issuedOvpnFile.VpnServerId}")
                .AppendLine($"FileId: {issuedOvpnFile.Id}")
                .AppendLine($"Error: {ex.Message}")
                .AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                .ToString();

            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));
            var errorFile = new InputFileStream(errorStream, $"{issuedOvpnFile.FileName}.error.txt");

            var errorMedia = new InputMediaDocument(errorFile)
            {
                Caption = $"Error file: {issuedOvpnFile.FileName}"
            };

            mediaGroupOpenVpnFiles.Add(errorMedia);
        }

        return mediaGroupOpenVpnFiles;
    }

    public async Task<bool> RevokeAllOvpnFileAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Revoking all OVPN files for telegramId: {telegramId}, ServerId: {vpnServerId}");
        var issuedOvpnFileResponses = await GetAllOvpnFilesListAsync(vpnServerId,
            telegramId, cancellationToken);

        if (!issuedOvpnFileResponses.Any())
        {
            _logger.LogWarning($"No OVPN files found to revoke for telegramId {telegramId} on server {vpnServerId}.");
            return false;
        }

        var success = true;
        foreach (var file in issuedOvpnFileResponses)
        {
            var request = new RevokeOvpnFileRequest()
            {
                VpnServerId = file.IssuedOvpnFile.VpnServerId,
                CommonName = file.IssuedOvpnFile.CommonName,
            };
            var revoked = await _ovpnFileService.RevokeOvpnFileAsync(request, cancellationToken);
            if (!revoked.Success)
            {
                _logger.LogError($"Failed to revoke OVPN file: " +
                                 $"{file.IssuedOvpnFile.FileName} for telegramId {telegramId} on server {vpnServerId}");
                success = false;
            }
        }

        return success;
    }

    public async Task<bool> RevokeOvpnFileAsync(int vpnServerId, long telegramId, string fileName,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            $"Revoking OVPN file '{fileName}' for telegramId: {telegramId}, ServerId: {vpnServerId}");

        var issuedOvpnFileResponses = await GetAllOvpnFilesListAsync(vpnServerId, telegramId, 
            cancellationToken);

        var fileToRevoke = issuedOvpnFileResponses.FirstOrDefault(f =>
            string.Equals(f.IssuedOvpnFile.FileName, fileName, StringComparison.OrdinalIgnoreCase));

        if (fileToRevoke == null)
        {
            _logger.LogWarning(
                $"OVPN file '{fileName}' not found for telegramId {telegramId} on server {vpnServerId}.");
            return false;
        }

        var request = new RevokeOvpnFileRequest
        {
            VpnServerId = fileToRevoke.IssuedOvpnFile.VpnServerId,
            CommonName = fileToRevoke.IssuedOvpnFile.CommonName,
        };

        var revoked = await _ovpnFileService.RevokeOvpnFileAsync(request, cancellationToken);

        if (!revoked.Success)
        {
            _logger.LogError(
                "Failed to revoke OVPN file: {FileName} for telegramId {telegramId} on server {VpnServerId}",
                fileName, telegramId, vpnServerId);
        }

        return revoked.Success;
    }

    public async Task<bool> CheckMaxCountOvpnFilesForClient(int vpnServerId, long telegramId,
        CancellationToken cancellationToken, int maxCountFiles = 10)
    {
        var files = await GetAllOvpnFilesListAsync(vpnServerId, telegramId, cancellationToken);

        var prefix = $"tg-{vpnServerId}-{telegramId}-";
        var usedCount = files.Count(f => f.IssuedOvpnFile.CommonName
            .StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return usedCount >= maxCountFiles;
    }

    private async Task<string> MakeCommonNameForOvpnFileAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken, int maxCountFiles = 10)
    {
        var files = await GetAllOvpnFilesListAsync(vpnServerId, telegramId, cancellationToken);

        var prefix = $"tg-{vpnServerId}-{telegramId}-";
        var usedNames = files
            .Where(f => f.IssuedOvpnFile.CommonName
                .StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.IssuedOvpnFile.CommonName)
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