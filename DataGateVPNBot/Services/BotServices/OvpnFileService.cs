using System.Text;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnFiles.Responses.Dto;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.BotServices;

public class OvpnFileService(DashboardServices.OvpnFileService ovpnFileService, IErrorService errorService,
    ILogger<OvpnFileService> logger)
    : IOvpnFileService
{
    public async Task<List<IssuedOvpnFileDto>> GetAllOvpnFilesListAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        var getAllByExternalIdOvpnFilesRequest = new ByExternalIdAndVpnServerIdRequest()
        {
            VpnServerId = vpnServerId, ExternalId = telegramId.ToString()
        };
        var issuedOvpnFileResponses =
            await ovpnFileService.GetAllOvpnFilesByExternalIdAsync(
                getAllByExternalIdOvpnFilesRequest, cancellationToken);
        issuedOvpnFileResponses = issuedOvpnFileResponses?.Where(x =>
            !x.IsRevoked).ToList() ?? [];

        return issuedOvpnFileResponses;
    }

    public async Task<DownloadFileResponse> DownloadOvpnFileByTokenAsync(string token, CancellationToken ct)
    {
        var byToken = new ByTokenRequest(){ Token = token };
        var issuedOvpnFileResponse = await ovpnFileService.GetOvpnFileByTokenAsync(byToken, ct);

        if (issuedOvpnFileResponse == null)
        {
            throw new FileNotFoundException($"Ovpn file not found: {token}");
        }

        var downloadOvpnFileRequest = new DownloadFileRequest()
        {
            VpnServerId = issuedOvpnFileResponse.VpnServerId,
            IssuedOvpnFileId = issuedOvpnFileResponse.Id
        };
        
        var downloadOvpnFileResponse = await ovpnFileService.DownloadOvpnFileByIdAndServerIdAsync(
            downloadOvpnFileRequest, ct);
        
        return downloadOvpnFileResponse;
    }


    public async Task<List<IAlbumInputMedia>> GetOvpnFilesAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        var getAllByExternalIdOvpnFilesRequest = new ByExternalIdAndVpnServerIdRequest()
        {
            VpnServerId = vpnServerId, ExternalId = telegramId.ToString()
        };
        logger.LogInformation($"Fetching OVPN files for telegramId: {telegramId}, ServerId: {vpnServerId}");

        var issuedOvpnFileResponses =
            await ovpnFileService.GetAllOvpnFilesByExternalIdAsync(
                getAllByExternalIdOvpnFilesRequest, cancellationToken);

        issuedOvpnFileResponses = issuedOvpnFileResponses?.Where(x =>
            !x.IsRevoked).ToList() ?? [];

        if (!issuedOvpnFileResponses.Any())
        {
            logger.LogInformation("No valid OVPN files found.");
            return new List<IAlbumInputMedia>();
        }

        var mediaGroupOpenVpnFiles = new List<IAlbumInputMedia>();

        foreach (var issuedOvpnFileResponse in issuedOvpnFileResponses)
        {
            try
            {
                logger.LogInformation(
                    $"Processing file: {issuedOvpnFileResponse.FileName}, " +
                    $"ServerId: {issuedOvpnFileResponse.VpnServerId}, " +
                    $"FileId: {issuedOvpnFileResponse.Id}");
                var downloadOvpnFileRequest = new DownloadFileRequest()
                {
                    VpnServerId = issuedOvpnFileResponse.VpnServerId,
                    IssuedOvpnFileId = issuedOvpnFileResponse.Id
                };

                var downloadOvpnFileResponse = await ovpnFileService.DownloadOvpnFileByIdAndServerIdAsync(
                    downloadOvpnFileRequest, cancellationToken);

                var stream = new MemoryStream(downloadOvpnFileResponse.Content);
                var inputFile = new InputFileStream(stream, downloadOvpnFileResponse.IssuedOvpn.FileName);
                var media = new InputMediaDocument(inputFile)
                {
                    Caption = issuedOvpnFileResponse.FileName
                };
                mediaGroupOpenVpnFiles.Add(media);
            }
            catch (Exception ex)
            {
                await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
                logger.LogError($"Error processing file " +
                                 $"{issuedOvpnFileResponse.FileName}: {ex.Message}");

                var errorMessage = new StringBuilder()
                    .AppendLine($"Error processing file: {issuedOvpnFileResponse.FileName}")
                    .AppendLine($"ServerId: {issuedOvpnFileResponse.VpnServerId}")
                    .AppendLine($"FileId: {issuedOvpnFileResponse.Id}")
                    .AppendLine($"Error: {ex.Message}")
                    .AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                    .ToString();

                var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));
                var errorFile = new InputFileStream(errorStream,
                    $"{issuedOvpnFileResponse.FileName}.error.txt");

                var errorMedia = new InputMediaDocument(errorFile)
                {
                    Caption = $"Error file: {issuedOvpnFileResponse.FileName}"
                };

                mediaGroupOpenVpnFiles.Add(errorMedia);
            }
        }

        return mediaGroupOpenVpnFiles;
    }

    public async Task<List<IAlbumInputMedia>> GetOvpnFilesWithTokenAsync(int vpnServerId, long telegramId, 
        string hostUrl, CancellationToken cancellationToken)
    {
        var getAllByExternalIdOvpnFilesRequest = new ByExternalIdAndVpnServerIdRequest()
        {
            VpnServerId = vpnServerId, ExternalId = telegramId.ToString()
        };
        logger.LogInformation($"Fetching OVPN files for telegramId: {telegramId}, ServerId: {vpnServerId}");

        var issuedOvpnFileResponses =
            await ovpnFileService.GetAllOvpnFilesByExternalIdWithTokenAsync(
                getAllByExternalIdOvpnFilesRequest, cancellationToken);
        
        if (issuedOvpnFileResponses != null && !issuedOvpnFileResponses.IssuedOvpnFiles.Any())
        {
            logger.LogInformation("No valid OVPN files found.");
            return new List<IAlbumInputMedia>();
        }

        if (issuedOvpnFileResponses != null)//todo: fix it
        {
            issuedOvpnFileResponses.IssuedOvpnFiles = issuedOvpnFileResponses.IssuedOvpnFiles
                .Where(x => !x.IsRevoked)
                .ToList();
        }
        
        var mediaGroupOpenVpnFiles = new List<IAlbumInputMedia>();

        //todo: fix backend response
        foreach (var issuedOvpnFileResponse in issuedOvpnFileResponses!.IssuedOvpnFiles)
        {
            var downloadUrl = string.Empty;
            
            var response = issuedOvpnFileResponses; // type: OvpnFilesWithTokensResponse

            var matchingToken = response.IssuedOvpnFileTokens
                .FirstOrDefault(t => t.IssuedOvpnFileId == issuedOvpnFileResponse.Id);

            if (!string.IsNullOrWhiteSpace(matchingToken?.Token))
            {
                downloadUrl = BuildDownloadUrlWithToken(hostUrl, matchingToken.Token);
                logger.LogInformation("Generated tokenized download URL: {DownloadUrl}", downloadUrl);
            }
            
            try
            {
                logger.LogInformation(
                    $"Processing file: {issuedOvpnFileResponse.FileName}, " +
                    $"ServerId: {issuedOvpnFileResponse.VpnServerId}, " +
                    $"FileId: {issuedOvpnFileResponse.Id}");
                var downloadOvpnFileRequest = new DownloadFileRequest()
                {
                    VpnServerId = issuedOvpnFileResponse.VpnServerId,
                    IssuedOvpnFileId = issuedOvpnFileResponse.Id
                };

                var downloadOvpnFileResponse = await ovpnFileService.DownloadOvpnFileByIdAndServerIdAsync(
                    downloadOvpnFileRequest, cancellationToken);

                var stream = new MemoryStream(downloadOvpnFileResponse.Content);
                var inputFile = new InputFileStream(stream, downloadOvpnFileResponse.IssuedOvpn.FileName);
                var media = new InputMediaDocument(inputFile)
                {
                    Caption = $"{issuedOvpnFileResponse.FileName} Url: {downloadUrl}"
                };
                mediaGroupOpenVpnFiles.Add(media);
            }
            catch (Exception ex)
            {
                await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
                logger.LogError($"Error processing file " +
                                 $"{issuedOvpnFileResponse.FileName}: {ex.Message}");

                var errorMessage = new StringBuilder()
                    .AppendLine($"Error processing file: {issuedOvpnFileResponse.FileName}")
                    .AppendLine($"ServerId: {issuedOvpnFileResponse.VpnServerId}")
                    .AppendLine($"FileId: {issuedOvpnFileResponse.Id}")
                    .AppendLine($"Error: {ex.Message}")
                    .AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                    .ToString();

                var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));
                var errorFile = new InputFileStream(errorStream,
                    $"{issuedOvpnFileResponse.FileName}.error.txt");

                var errorMedia = new InputMediaDocument(errorFile)
                {
                    Caption = $"Error file: {issuedOvpnFileResponse.FileName}"
                };

                mediaGroupOpenVpnFiles.Add(errorMedia);
            }
        }

        return mediaGroupOpenVpnFiles;
    }


    public async Task<List<IAlbumInputMedia>> MakeOvpnFileWithTokenAsync(int vpnServerId, long telegramId, 
        string hostUrl, CancellationToken cancellationToken)
    {
        var mediaGroupOpenVpnFiles = new List<IAlbumInputMedia>();
        logger.LogInformation("Creating OVPN file with token. " +
                              "TelegramId: {TelegramId}, ServerId: {VpnServerId}", telegramId, vpnServerId);

        var addOvpnFileRequest = new AddFileRequest
        {
            VpnServerId = vpnServerId,
            CommonName = await MakeCommonNameForOvpnFileAsync(vpnServerId, telegramId, cancellationToken),
            ExternalId = telegramId.ToString(),
            IssuedTo = $"telegram user {telegramId} with token"
        };

        var addOvpnFileResponse =
            await ovpnFileService.AddOvpnFileWithTokenAsync(addOvpnFileRequest, cancellationToken);

        if (addOvpnFileResponse?.IssuedOvpnFile == null)
        {
            logger.LogWarning("Failed to create OVPN file with token " +
                              "for telegramId: {TelegramId}, ServerId: {VpnServerId}", telegramId, vpnServerId);
            return mediaGroupOpenVpnFiles;
        }

        var issuedOvpnFile = addOvpnFileResponse.IssuedOvpnFile;

        var token = addOvpnFileResponse.IssuedOvpnFileToken;
        
        var downloadUrl = BuildDownloadUrlWithToken(hostUrl, token.Token);
        logger.LogInformation("Generated tokenized download URL: {DownloadUrl}", downloadUrl);
        try
        {
            logger.LogInformation(
                $"Downloading newly created file with token: {issuedOvpnFile.FileName}, " +
                $"ServerId: {issuedOvpnFile.VpnServerId}, FileId: {issuedOvpnFile.Id}");

            var downloadRequest = new DownloadFileRequest
            {
                VpnServerId = issuedOvpnFile.VpnServerId,
                IssuedOvpnFileId = issuedOvpnFile.Id
            };
            var downloadOvpnFileResponse = await ovpnFileService.DownloadOvpnFileByIdAndServerIdAsync(
                downloadRequest, cancellationToken);

            var stream = new MemoryStream(downloadOvpnFileResponse.Content);
            var inputFile = new InputFileStream(stream, downloadOvpnFileResponse.IssuedOvpn.FileName);
            var media = new InputMediaDocument(inputFile)
            {
                Caption = $"{issuedOvpnFile.FileName} Url: {downloadUrl}"
            };
            mediaGroupOpenVpnFiles.Add(media);
        }
        catch (Exception ex)
        {
            await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
            logger.LogError("Error processing file with token {FileName}: {ErrorMessage}", issuedOvpnFile.FileName,
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
                Caption = $"Error file with token: {issuedOvpnFile.FileName}"
            };

            mediaGroupOpenVpnFiles.Add(errorMedia);
        }

        return mediaGroupOpenVpnFiles;
    }


    public async Task<List<IAlbumInputMedia>> MakeOvpnFileAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        var mediaGroupOpenVpnFiles = new List<IAlbumInputMedia>();
        logger.LogInformation($"Creating OVPN file for telegramId: {telegramId}, ServerId: {vpnServerId}");

        var addOvpnFileRequest = new AddFileRequest
        {
            VpnServerId = vpnServerId,
            CommonName = await MakeCommonNameForOvpnFileAsync(vpnServerId, telegramId, cancellationToken),
            ExternalId = telegramId.ToString(),
            IssuedTo = $"telegram user {telegramId}"
        };

        var addOvpnFileResponse =
            await ovpnFileService.AddOvpnFileAsync(addOvpnFileRequest, cancellationToken);

        if (addOvpnFileResponse?.IssuedOvpnFile == null)
        {
            logger.LogWarning($"Failed to create OVPN file for telegramId: {telegramId}, ServerId: {vpnServerId}");
            return mediaGroupOpenVpnFiles;
        }

        var issuedOvpnFile = addOvpnFileResponse.IssuedOvpnFile;
        try
        {
            logger.LogInformation(
                $"Downloading newly created file: {issuedOvpnFile.FileName}, " +
                $"ServerId: {issuedOvpnFile.VpnServerId}, FileId: {issuedOvpnFile.Id}");

            var downloadRequest = new DownloadFileRequest
            {
                VpnServerId = issuedOvpnFile.VpnServerId,
                IssuedOvpnFileId = issuedOvpnFile.Id
            };
            var downloadOvpnFileResponse = await ovpnFileService.DownloadOvpnFileByIdAndServerIdAsync(
                downloadRequest, cancellationToken);

            var stream = new MemoryStream(downloadOvpnFileResponse.Content);
            var inputFile = new InputFileStream(stream, downloadOvpnFileResponse.IssuedOvpn.FileName);
            var media = new InputMediaDocument(inputFile)
            {
                Caption = issuedOvpnFile.FileName
            };
            mediaGroupOpenVpnFiles.Add(media);
        }
        catch (Exception ex)
        {
            await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
            logger.LogError("Error processing file {FileName}: {ErrorMessage}", issuedOvpnFile.FileName,
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
        logger.LogInformation("Revoking all OVPN files for telegramId: {TelegramId}, ServerId: {ServerId}", telegramId,
            vpnServerId);

        var files = await GetAllOvpnFilesListAsync(vpnServerId, telegramId, cancellationToken);
        if (files.Count == 0)
        {
            logger.LogWarning("No OVPN files found to revoke for telegramId {TelegramId} on server {ServerId}.",
                telegramId, vpnServerId);
            return false;
        }

        var total = files.Count;
        var success = 0;

        foreach (var file in files)
        {
            var request = new RevokeFileRequest
            {
                VpnServerId = file.VpnServerId,
                OvpnFileId = file.Id,
                CommonName = file.CommonName,
                IsRevoked = file.IsRevoked
            };

            try
            {
                var revoked = await ovpnFileService.RevokeOvpnFileAsync(request, cancellationToken);
                if (revoked.IssuedOvpnFile.IsRevoked)
                {
                    success++;
                    logger.LogInformation("Revoked OVPN file: {CommonName} (ServerId: {ServerId})", file.CommonName,
                        vpnServerId);
                }
                else
                {
                    logger.LogWarning("Failed to revoke OVPN file: {CommonName} (ServerId: {ServerId})",
                        file.CommonName, vpnServerId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error revoking OVPN file: {CommonName} (ServerId: {ServerId})", file.CommonName,
                    vpnServerId);
            }
        }

        var allRevoked = success == total;
        logger.LogInformation("Revocation summary for telegramId {TelegramId}, server {ServerId}: {Success}/{Total}",
            telegramId, vpnServerId, success, total);
        return allRevoked;
    }

    public async Task<bool> RevokeOvpnFileAsync(int vpnServerId, long telegramId, string fileName,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            $"Revoking OVPN file '{fileName}' for telegramId: {telegramId}, ServerId: {vpnServerId}");

        var issuedOvpnFileResponses = await GetAllOvpnFilesListAsync(vpnServerId, telegramId, 
            cancellationToken);

        var fileToRevoke = issuedOvpnFileResponses.FirstOrDefault(f =>
            string.Equals(f.FileName, fileName, StringComparison.OrdinalIgnoreCase));

        if (fileToRevoke == null)
        {
            logger.LogWarning(
                "OVPN file '{fileName}' not found for telegramId {telegramId} on server {vpnServerId}.",
                fileName, telegramId, vpnServerId);
            return false;
        }

        var request = new RevokeFileRequest
        {
            VpnServerId = fileToRevoke.VpnServerId,
            OvpnFileId = fileToRevoke.Id,
            CommonName = fileToRevoke.CommonName,
            IsRevoked = fileToRevoke.IsRevoked
        };

        var revoked = await ovpnFileService.RevokeOvpnFileAsync(request, cancellationToken);

        if (!revoked.IssuedOvpnFile.IsRevoked)
        {
            logger.LogError(
                "Failed to revoke OVPN file: {FileName} for telegramId {telegramId} on server {VpnServerId}",
                fileName, telegramId, vpnServerId);
        }

        return revoked.IssuedOvpnFile.IsRevoked;
    }

    public async Task<bool> CheckMaxCountOvpnFilesForClient(int vpnServerId, long telegramId,
        CancellationToken cancellationToken, int maxCountFiles = 10)
    {
        var files = await GetAllOvpnFilesListAsync(vpnServerId, telegramId, cancellationToken);

        var prefix = $"tg-{vpnServerId}-{telegramId}-";
        var usedCount = files.Count(f => f.CommonName
            .StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return usedCount >= maxCountFiles;
    }

    private async Task<string> MakeCommonNameForOvpnFileAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken, int maxCountFiles = 10)
    {
        var files = await GetAllOvpnFilesListAsync(vpnServerId, telegramId, cancellationToken);

        var prefix = $"tg-{vpnServerId}-{telegramId}-";
        var usedNames = files
            .Where(f => f.CommonName
                .StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
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
    
    private string BuildDownloadUrlWithToken(string baseUrl, string token)
    {
        baseUrl = baseUrl.TrimEnd('/');

        var url = $"{baseUrl}/openvpn-api/profile?token={Uri.EscapeDataString(token)}";

        return url;
    }
}