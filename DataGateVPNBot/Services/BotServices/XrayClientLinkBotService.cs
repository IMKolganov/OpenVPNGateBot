using System.Text;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;
using DataGateVPNBot.Services.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.XrayClientLinks.Responses.Dto;
using Telegram.Bot.Types;

namespace DataGateVPNBot.Services.BotServices;

public class XrayClientLinkBotService(XrayClientLinksDashboardService dashboard, IErrorService errorService,
    ILogger<XrayClientLinkBotService> logger)
    : IXrayClientLinkBotService
{
    public async Task<List<IssuedXrayClientLinkDto>> GetAllClientLinksListAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        var listRequest = new GetXrayClientLinksByExternalIdAndVpnServerIdRequest()
        {
            VpnServerId = vpnServerId, ExternalId = telegramId.ToString()
        };
        var issuedXrayClientLinkResponses =
            await dashboard.GetAllClientLinksByExternalIdAsync(
                listRequest, cancellationToken);
        issuedXrayClientLinkResponses = issuedXrayClientLinkResponses?.Where(x =>
            !x.IsRevoked).ToList() ?? [];

        return issuedXrayClientLinkResponses;
    }

    public async Task<DownloadXrayClientLinkResponse> DownloadClientLinkByTokenAsync(string token, CancellationToken ct)
    {
        var byToken = new GetXrayClientLinkByTokenRequest(){ Token = token };
        var issuedXrayClientLinkResponse = await dashboard.GetClientLinkByTokenAsync(byToken, ct);

        if (issuedXrayClientLinkResponse == null)
        {
            throw new FileNotFoundException($"Xray client link not found: {token}");
        }

        var downloadRequest = new DownloadXrayClientLinkRequest()
        {
            VpnServerId = issuedXrayClientLinkResponse.VpnServerId,
            IssuedXrayClientLinkId = issuedXrayClientLinkResponse.Id
        };
        
        var downloadXrayClientLinkResponse = await dashboard.DownloadClientLinkByIdAndServerIdAsync(
            downloadRequest, ct);
        
        return downloadXrayClientLinkResponse;
    }


    public async Task<List<IAlbumInputMedia>> GetClientLinksAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        var listRequest = new GetXrayClientLinksByExternalIdAndVpnServerIdRequest()
        {
            VpnServerId = vpnServerId, ExternalId = telegramId.ToString()
        };
        logger.LogInformation($"Fetching Xray client links for telegramId: {telegramId}, ServerId: {vpnServerId}");

        var issuedXrayClientLinkResponses =
            await dashboard.GetAllClientLinksByExternalIdAsync(
                listRequest, cancellationToken);

        issuedXrayClientLinkResponses = issuedXrayClientLinkResponses?.Where(x =>
            !x.IsRevoked).ToList() ?? [];

        if (!issuedXrayClientLinkResponses.Any())
        {
            logger.LogInformation("No valid Xray client links found.");
            return new List<IAlbumInputMedia>();
        }

        var mediaGroup = new List<IAlbumInputMedia>();

        foreach (var issuedXrayClientLinkResponse in issuedXrayClientLinkResponses)
        {
            try
            {
                logger.LogInformation(
                    $"Processing file: {issuedXrayClientLinkResponse.FileName}, " +
                    $"ServerId: {issuedXrayClientLinkResponse.VpnServerId}, " +
                    $"FileId: {issuedXrayClientLinkResponse.Id}");
                var downloadRequest = new DownloadXrayClientLinkRequest()
                {
                    VpnServerId = issuedXrayClientLinkResponse.VpnServerId,
                    IssuedXrayClientLinkId = issuedXrayClientLinkResponse.Id
                };

                var downloadXrayClientLinkResponse = await dashboard.DownloadClientLinkByIdAndServerIdAsync(
                    downloadRequest, cancellationToken);

                var stream = new MemoryStream(downloadXrayClientLinkResponse.Content ?? Array.Empty<byte>());
                var inputFile = new InputFileStream(stream, downloadXrayClientLinkResponse.IssuedXrayClientLink.FileName);
                var media = new InputMediaDocument(inputFile)
                {
                    Caption = issuedXrayClientLinkResponse.FileName
                };
                mediaGroup.Add(media);
            }
            catch (Exception ex)
            {
                await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
                logger.LogError($"Error processing file " +
                                 $"{issuedXrayClientLinkResponse.FileName}: {ex.Message}");

                var errorMessage = new StringBuilder()
                    .AppendLine($"Error processing file: {issuedXrayClientLinkResponse.FileName}")
                    .AppendLine($"ServerId: {issuedXrayClientLinkResponse.VpnServerId}")
                    .AppendLine($"FileId: {issuedXrayClientLinkResponse.Id}")
                    .AppendLine($"Error: {ex.Message}")
                    .AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                    .ToString();

                var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));
                var errorFile = new InputFileStream(errorStream,
                    $"{issuedXrayClientLinkResponse.FileName}.error.txt");

                var errorMedia = new InputMediaDocument(errorFile)
                {
                    Caption = $"Error file: {issuedXrayClientLinkResponse.FileName}"
                };

                mediaGroup.Add(errorMedia);
            }
        }

        return mediaGroup;
    }

    public async Task<List<IAlbumInputMedia>> GetClientLinksWithTokenAsync(int vpnServerId, long telegramId, 
        string hostUrl, CancellationToken cancellationToken)
    {
        var listRequest = new GetXrayClientLinksByExternalIdAndVpnServerIdRequest()
        {
            VpnServerId = vpnServerId, ExternalId = telegramId.ToString()
        };
        logger.LogInformation($"Fetching Xray client links for telegramId: {telegramId}, ServerId: {vpnServerId}");

        var issuedXrayClientLinkResponses =
            await dashboard.GetAllClientLinksByExternalIdWithTokenAsync(
                listRequest, cancellationToken);
        
        if (issuedXrayClientLinkResponses != null && !issuedXrayClientLinkResponses.IssuedXrayClientLinks.Any())
        {
            logger.LogInformation("No valid Xray client links found.");
            return new List<IAlbumInputMedia>();
        }

        if (issuedXrayClientLinkResponses != null)//todo: fix it
        {
            issuedXrayClientLinkResponses.IssuedXrayClientLinks = issuedXrayClientLinkResponses.IssuedXrayClientLinks
                .Where(x => !x.IsRevoked)
                .ToList();
        }
        
        var mediaGroup = new List<IAlbumInputMedia>();

        //todo: fix backend response
        foreach (var issuedXrayClientLinkResponse in issuedXrayClientLinkResponses!.IssuedXrayClientLinks)
        {
            var downloadUrl = string.Empty;
            
            var response = issuedXrayClientLinkResponses; // type: XrayClientLinksWithTokensResponse

            var matchingToken = response.IssuedXrayClientLinkTokens
                .FirstOrDefault(t => t.IssuedXrayClientLinkId == issuedXrayClientLinkResponse.Id);

            if (!string.IsNullOrWhiteSpace(matchingToken?.Token))
            {
                downloadUrl = BuildDownloadUrlWithToken(hostUrl, matchingToken.Token);
                logger.LogInformation("Generated tokenized download URL: {DownloadUrl}", downloadUrl);
            }
            
            try
            {
                logger.LogInformation(
                    $"Processing file: {issuedXrayClientLinkResponse.FileName}, " +
                    $"ServerId: {issuedXrayClientLinkResponse.VpnServerId}, " +
                    $"FileId: {issuedXrayClientLinkResponse.Id}");
                var downloadRequest = new DownloadXrayClientLinkRequest()
                {
                    VpnServerId = issuedXrayClientLinkResponse.VpnServerId,
                    IssuedXrayClientLinkId = issuedXrayClientLinkResponse.Id
                };

                var downloadXrayClientLinkResponse = await dashboard.DownloadClientLinkByIdAndServerIdAsync(
                    downloadRequest, cancellationToken);

                var stream = new MemoryStream(downloadXrayClientLinkResponse.Content ?? Array.Empty<byte>());
                var inputFile = new InputFileStream(stream, downloadXrayClientLinkResponse.IssuedXrayClientLink.FileName);
                var media = new InputMediaDocument(inputFile)
                {
                    Caption = $"{issuedXrayClientLinkResponse.FileName} Url: {downloadUrl}"
                };
                mediaGroup.Add(media);
            }
            catch (Exception ex)
            {
                await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
                logger.LogError($"Error processing file " +
                                 $"{issuedXrayClientLinkResponse.FileName}: {ex.Message}");

                var errorMessage = new StringBuilder()
                    .AppendLine($"Error processing file: {issuedXrayClientLinkResponse.FileName}")
                    .AppendLine($"ServerId: {issuedXrayClientLinkResponse.VpnServerId}")
                    .AppendLine($"FileId: {issuedXrayClientLinkResponse.Id}")
                    .AppendLine($"Error: {ex.Message}")
                    .AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                    .ToString();

                var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));
                var errorFile = new InputFileStream(errorStream,
                    $"{issuedXrayClientLinkResponse.FileName}.error.txt");

                var errorMedia = new InputMediaDocument(errorFile)
                {
                    Caption = $"Error file: {issuedXrayClientLinkResponse.FileName}"
                };

                mediaGroup.Add(errorMedia);
            }
        }

        return mediaGroup;
    }

    public async Task<string> GetClientLinksTextWithTokenAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        var request = new GetXrayClientLinksByExternalIdAndVpnServerIdRequest
        {
            VpnServerId = vpnServerId,
            ExternalId = telegramId.ToString()
        };

        var response = await dashboard.GetAllClientLinksByExternalIdWithTokenAsync(request, cancellationToken);
        if (response == null || !response.IssuedXrayClientLinks.Any())
            return string.Empty;

        var activeFiles = response.IssuedXrayClientLinks.Where(x => !x.IsRevoked).ToList();
        if (!activeFiles.Any())
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var file in activeFiles)
        {
            try
            {
                var download = await dashboard.DownloadClientLinkByIdAndServerIdAsync(new DownloadXrayClientLinkRequest
                {
                    VpnServerId = file.VpnServerId,
                    IssuedXrayClientLinkId = file.Id
                }, cancellationToken);

                var text = Encoding.UTF8.GetString(download.Content ?? Array.Empty<byte>()).Trim();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (sb.Length > 0)
                    sb.AppendLine().AppendLine("-----").AppendLine();

                sb.AppendLine(file.FileName);
                sb.AppendLine(text);
            }
            catch (Exception ex)
            {
                await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
                logger.LogError(ex, "Failed to prepare XRay text link for file {FileName}", file.FileName);
            }
        }

        return sb.ToString().Trim();
    }

    public async Task<List<(string FileName, string Text)>> GetClientLinkItemsWithTokenAsync(int vpnServerId,
        long telegramId, CancellationToken cancellationToken)
    {
        var request = new GetXrayClientLinksByExternalIdAndVpnServerIdRequest
        {
            VpnServerId = vpnServerId,
            ExternalId = telegramId.ToString()
        };

        var response = await dashboard.GetAllClientLinksByExternalIdWithTokenAsync(request, cancellationToken);
        if (response == null || !response.IssuedXrayClientLinks.Any())
            return [];

        var activeFiles = response.IssuedXrayClientLinks.Where(x => !x.IsRevoked).ToList();
        if (!activeFiles.Any())
            return [];

        var items = new List<(string FileName, string Text)>();
        foreach (var file in activeFiles)
        {
            try
            {
                var download = await dashboard.DownloadClientLinkByIdAndServerIdAsync(new DownloadXrayClientLinkRequest
                {
                    VpnServerId = file.VpnServerId,
                    IssuedXrayClientLinkId = file.Id
                }, cancellationToken);

                var text = Encoding.UTF8.GetString(download.Content ?? Array.Empty<byte>()).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    items.Add((file.FileName, text));
            }
            catch (Exception ex)
            {
                await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
                logger.LogError(ex, "Failed to prepare XRay item text for file {FileName}", file.FileName);
            }
        }

        return items;
    }


    public async Task<List<IAlbumInputMedia>> MakeClientLinkWithTokenAsync(int vpnServerId, long telegramId, 
        string hostUrl, CancellationToken cancellationToken)
    {
        var mediaGroup = new List<IAlbumInputMedia>();
        logger.LogInformation("Creating client link with token. " +
                              "TelegramId: {TelegramId}, ServerId: {VpnServerId}", telegramId, vpnServerId);

        var addRequest = new AddXrayClientLinkRequest
        {
            VpnServerId = vpnServerId,
            CommonName = await MakeCommonNameForClientLinkAsync(vpnServerId, telegramId, cancellationToken),
            ExternalId = telegramId.ToString(),
            IssuedTo = $"telegram user {telegramId} with token"
        };

        var addXrayClientLinkResponse =
            await dashboard.AddClientLinkWithTokenAsync(addRequest, cancellationToken);

        if (addXrayClientLinkResponse?.IssuedXrayClientLink == null)
        {
            logger.LogWarning("Failed to create client link with token " +
                              "for telegramId: {TelegramId}, ServerId: {VpnServerId}", telegramId, vpnServerId);
            return mediaGroup;
        }

        var issuedLink = addXrayClientLinkResponse.IssuedXrayClientLink;

        var token = addXrayClientLinkResponse.IssuedXrayClientLinkToken;
        
        var downloadUrl = BuildDownloadUrlWithToken(hostUrl, token.Token);
        logger.LogInformation("Generated tokenized download URL: {DownloadUrl}", downloadUrl);
        try
        {
            logger.LogInformation(
                $"Downloading newly created file with token: {issuedLink.FileName}, " +
                $"ServerId: {issuedLink.VpnServerId}, FileId: {issuedLink.Id}");

            var downloadRequest = new DownloadXrayClientLinkRequest
            {
                VpnServerId = issuedLink.VpnServerId,
                IssuedXrayClientLinkId = issuedLink.Id
            };
            var downloadXrayClientLinkResponse = await dashboard.DownloadClientLinkByIdAndServerIdAsync(
                downloadRequest, cancellationToken);

            var stream = new MemoryStream(downloadXrayClientLinkResponse.Content ?? Array.Empty<byte>());
            var inputFile = new InputFileStream(stream, downloadXrayClientLinkResponse.IssuedXrayClientLink.FileName);
            var media = new InputMediaDocument(inputFile)
            {
                Caption = $"{issuedLink.FileName} Url: {downloadUrl}"
            };
            mediaGroup.Add(media);
        }
        catch (Exception ex)
        {
            await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
            logger.LogError("Error processing file with token {FileName}: {ErrorMessage}", issuedLink.FileName,
                ex.Message);

            var errorMessage = new StringBuilder()
                .AppendLine($"Error downloading file: {issuedLink.FileName}")
                .AppendLine($"ServerId: {issuedLink.VpnServerId}")
                .AppendLine($"FileId: {issuedLink.Id}")
                .AppendLine($"Error: {ex.Message}")
                .AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                .ToString();

            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));
            var errorFile = new InputFileStream(errorStream, $"{issuedLink.FileName}.error.txt");

            var errorMedia = new InputMediaDocument(errorFile)
            {
                Caption = $"Error file with token: {issuedLink.FileName}"
            };

            mediaGroup.Add(errorMedia);
        }

        return mediaGroup;
    }

    public async Task<string> MakeClientLinkTextWithTokenAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating XRay client link text. TelegramId: {TelegramId}, ServerId: {VpnServerId}",
            telegramId, vpnServerId);

        var addRequest = new AddXrayClientLinkRequest
        {
            VpnServerId = vpnServerId,
            CommonName = await MakeCommonNameForClientLinkAsync(vpnServerId, telegramId, cancellationToken),
            ExternalId = telegramId.ToString(),
            IssuedTo = $"telegram user {telegramId} with token"
        };

        var created = await dashboard.AddClientLinkWithTokenAsync(addRequest, cancellationToken);
        if (created?.IssuedXrayClientLink == null)
            return string.Empty;

        var download = await dashboard.DownloadClientLinkByIdAndServerIdAsync(new DownloadXrayClientLinkRequest
        {
            VpnServerId = created.IssuedXrayClientLink.VpnServerId,
            IssuedXrayClientLinkId = created.IssuedXrayClientLink.Id
        }, cancellationToken);

        var text = Encoding.UTF8.GetString(download.Content ?? Array.Empty<byte>()).Trim();
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return $"{created.IssuedXrayClientLink.FileName}{Environment.NewLine}{text}";
    }

    public async Task<(string FileName, string Text)?> MakeClientLinkItemWithTokenAsync(int vpnServerId,
        long telegramId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating XRay client link item. TelegramId: {TelegramId}, ServerId: {VpnServerId}",
            telegramId, vpnServerId);

        var addRequest = new AddXrayClientLinkRequest
        {
            VpnServerId = vpnServerId,
            CommonName = await MakeCommonNameForClientLinkAsync(vpnServerId, telegramId, cancellationToken),
            ExternalId = telegramId.ToString(),
            IssuedTo = $"telegram user {telegramId} with token"
        };

        var created = await dashboard.AddClientLinkWithTokenAsync(addRequest, cancellationToken);
        if (created?.IssuedXrayClientLink == null)
            return null;

        var download = await dashboard.DownloadClientLinkByIdAndServerIdAsync(new DownloadXrayClientLinkRequest
        {
            VpnServerId = created.IssuedXrayClientLink.VpnServerId,
            IssuedXrayClientLinkId = created.IssuedXrayClientLink.Id
        }, cancellationToken);

        var text = Encoding.UTF8.GetString(download.Content ?? Array.Empty<byte>()).Trim();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return (created.IssuedXrayClientLink.FileName, text);
    }


    public async Task<List<IAlbumInputMedia>> MakeClientLinkAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        var mediaGroup = new List<IAlbumInputMedia>();
        logger.LogInformation($"Creating client link for telegramId: {telegramId}, ServerId: {vpnServerId}");

        var addRequest = new AddXrayClientLinkRequest
        {
            VpnServerId = vpnServerId,
            CommonName = await MakeCommonNameForClientLinkAsync(vpnServerId, telegramId, cancellationToken),
            ExternalId = telegramId.ToString(),
            IssuedTo = $"telegram user {telegramId}"
        };

        var addXrayClientLinkResponse =
            await dashboard.AddClientLinkAsync(addRequest, cancellationToken);

        if (addXrayClientLinkResponse?.IssuedXrayClientLink == null)
        {
            logger.LogWarning($"Failed to create client link for telegramId: {telegramId}, ServerId: {vpnServerId}");
            return mediaGroup;
        }

        var issuedLink = addXrayClientLinkResponse.IssuedXrayClientLink;
        try
        {
            logger.LogInformation(
                $"Downloading newly created file: {issuedLink.FileName}, " +
                $"ServerId: {issuedLink.VpnServerId}, FileId: {issuedLink.Id}");

            var downloadRequest = new DownloadXrayClientLinkRequest
            {
                VpnServerId = issuedLink.VpnServerId,
                IssuedXrayClientLinkId = issuedLink.Id
            };
            var downloadXrayClientLinkResponse = await dashboard.DownloadClientLinkByIdAndServerIdAsync(
                downloadRequest, cancellationToken);

            var stream = new MemoryStream(downloadXrayClientLinkResponse.Content ?? Array.Empty<byte>());
            var inputFile = new InputFileStream(stream, downloadXrayClientLinkResponse.IssuedXrayClientLink.FileName);
            var media = new InputMediaDocument(inputFile)
            {
                Caption = issuedLink.FileName
            };
            mediaGroup.Add(media);
        }
        catch (Exception ex)
        {
            await errorService.NotifyAdminsAboutExceptionAsync(ex, null, cancellationToken);
            logger.LogError("Error processing file {FileName}: {ErrorMessage}", issuedLink.FileName,
                ex.Message);

            var errorMessage = new StringBuilder()
                .AppendLine($"Error downloading file: {issuedLink.FileName}")
                .AppendLine($"ServerId: {issuedLink.VpnServerId}")
                .AppendLine($"FileId: {issuedLink.Id}")
                .AppendLine($"Error: {ex.Message}")
                .AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                .ToString();

            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));
            var errorFile = new InputFileStream(errorStream, $"{issuedLink.FileName}.error.txt");

            var errorMedia = new InputMediaDocument(errorFile)
            {
                Caption = $"Error file: {issuedLink.FileName}"
            };

            mediaGroup.Add(errorMedia);
        }

        return mediaGroup;
    }

    public async Task<bool> RevokeAllClientLinksAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Revoking all client links for telegramId: {TelegramId}, ServerId: {ServerId}", telegramId,
            vpnServerId);

        var files = await GetAllClientLinksListAsync(vpnServerId, telegramId, cancellationToken);
        if (files.Count == 0)
        {
            logger.LogWarning("No client links found to revoke for telegramId {TelegramId} on server {ServerId}.",
                telegramId, vpnServerId);
            return false;
        }

        var total = files.Count;
        var success = 0;

        foreach (var file in files)
        {
            var request = new RevokeXrayClientLinkRequest
            {
                VpnServerId = file.VpnServerId,
                IssuedXrayClientLinkId = file.Id,
                CommonName = file.CommonName,
                IsRevoked = file.IsRevoked
            };

            try
            {
                var revoked = await dashboard.RevokeClientLinkAsync(request, cancellationToken);
                if (revoked.IssuedXrayClientLink.IsRevoked)
                {
                    success++;
                    logger.LogInformation("Revoked client link: {CommonName} (ServerId: {ServerId})", file.CommonName,
                        vpnServerId);
                }
                else
                {
                    logger.LogWarning("Failed to revoke client link: {CommonName} (ServerId: {ServerId})",
                        file.CommonName, vpnServerId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error revoking client link: {CommonName} (ServerId: {ServerId})", file.CommonName,
                    vpnServerId);
            }
        }

        var allRevoked = success == total;
        logger.LogInformation("Revocation summary for telegramId {TelegramId}, server {ServerId}: {Success}/{Total}",
            telegramId, vpnServerId, success, total);
        return allRevoked;
    }

    public async Task<bool> RevokeClientLinkAsync(int vpnServerId, long telegramId, string fileName,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            $"Revoking client link '{fileName}' for telegramId: {telegramId}, ServerId: {vpnServerId}");

        var issuedXrayClientLinkResponses = await GetAllClientLinksListAsync(vpnServerId, telegramId, 
            cancellationToken);

        var fileToRevoke = issuedXrayClientLinkResponses.FirstOrDefault(f =>
            string.Equals(f.FileName, fileName, StringComparison.OrdinalIgnoreCase));

        if (fileToRevoke == null)
        {
            logger.LogWarning(
                "client link '{fileName}' not found for telegramId {telegramId} on server {vpnServerId}.",
                fileName, telegramId, vpnServerId);
            return false;
        }

        var request = new RevokeXrayClientLinkRequest
        {
            VpnServerId = fileToRevoke.VpnServerId,
            IssuedXrayClientLinkId = fileToRevoke.Id,
            CommonName = fileToRevoke.CommonName,
            IsRevoked = fileToRevoke.IsRevoked
        };

        var revoked = await dashboard.RevokeClientLinkAsync(request, cancellationToken);

        if (!revoked.IssuedXrayClientLink.IsRevoked)
        {
            logger.LogError(
                "Failed to revoke client link: {FileName} for telegramId {telegramId} on server {VpnServerId}",
                fileName, telegramId, vpnServerId);
        }

        return revoked.IssuedXrayClientLink.IsRevoked;
    }

    public async Task<bool> CheckMaxCountClientLinksForClient(int vpnServerId, long telegramId,
        CancellationToken cancellationToken, int maxCountFiles = 10)
    {
        var files = await GetAllClientLinksListAsync(vpnServerId, telegramId, cancellationToken);

        var prefix = $"tg-{vpnServerId}-{telegramId}-";
        var usedCount = files.Count(f => f.CommonName
            .StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return usedCount >= maxCountFiles;
    }

    private async Task<string> MakeCommonNameForClientLinkAsync(int vpnServerId, long telegramId,
        CancellationToken cancellationToken, int maxCountFiles = 10)
    {
        var files = await GetAllClientLinksListAsync(vpnServerId, telegramId, cancellationToken);

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
    
    private static string BuildDownloadUrlWithToken(string baseUrl, string token)
    {
        baseUrl = baseUrl.TrimEnd('/');
        return $"{baseUrl}/DownloadByToken?token={Uri.EscapeDataString(token)}";
    }
}