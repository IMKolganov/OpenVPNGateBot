
using System.Text;
using Telegram.Bot.Types;
using DataGateVPNBot.Models.DashBoardApi;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DashboardServices;

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

    public async Task<List<IAlbumInputMedia>> GetOvpnFilesAsync(int vpnServerId, long userId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Fetching OVPN files for user: {userId}, ServerId: {vpnServerId}");

        var issuedOvpnFileResponses = await _dashBoardApiOvpnFileService.GetAllOvpnFilesByExternalIdAsync(
            vpnServerId, userId.ToString(), cancellationToken);

        issuedOvpnFileResponses = issuedOvpnFileResponses?.Where(x => !x.IsRevoked).ToList() ??
                                  new List<IssuedOvpnFileResponse>();

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
                    $"ServerId: {issuedOvpnFileResponse.ServerId}, FileId: {issuedOvpnFileResponse.Id}");

                var issuedOvpnFileStream = await _dashBoardApiOvpnFileService.DownloadOvpnFileByIdAndServerIdAsync(
                    issuedOvpnFileResponse.Id, issuedOvpnFileResponse.ServerId, cancellationToken);

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
                    .AppendLine($"ServerId: {issuedOvpnFileResponse.ServerId}")
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

    public async Task<InputFile?> MakeOvpnFileAsync(int vpnServerId, long userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Creating OVPN file for user: {userId}, ServerId: {vpnServerId}");

        var success = await _dashBoardApiOvpnFileService.AddOvpnFileAsync(
            userId.ToString(), $"user-{userId}", vpnServerId, cancellationToken);

        if (!success)
        {
            _logger.LogWarning("Failed to request OVPN file creation for user {UserId} on server {VpnServerId}.", userId, vpnServerId);
            return null;
        }

        var issuedOvpnFileResponses = await _dashBoardApiOvpnFileService.GetAllOvpnFilesByExternalIdAsync(
            vpnServerId, userId.ToString(), cancellationToken);

        var issuedOvpnFile = issuedOvpnFileResponses?.FirstOrDefault(x => !x.IsRevoked);
        if (issuedOvpnFile == null)
        {
            _logger.LogWarning("No valid OVPN file found for user {UserId} on server {VpnServerId} after creation.", userId, vpnServerId);
            return null;
        }

        _logger.LogInformation("Downloading newly created OVPN file: {FileName}", issuedOvpnFile.FileName);

        var issuedOvpnFileStream = await _dashBoardApiOvpnFileService.DownloadOvpnFileByIdAndServerIdAsync(
            issuedOvpnFile.Id, issuedOvpnFile.ServerId, cancellationToken);

        var inputFile = new InputFileStream(issuedOvpnFileStream, issuedOvpnFile.FileName);
        return inputFile;
    }
}