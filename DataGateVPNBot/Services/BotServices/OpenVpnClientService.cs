using DataGateVPNBot.Models;
using DataGateVPNBot.Models.Helpers;
using DataGateVPNBot.Services.BotServices.Interfaces;
using DataGateVPNBot.Services.DataServices.Interfaces;
using DataGateVPNBot.Services.UntilsServices.Interfaces;

namespace DataGateVPNBot.Services.BotServices;

public class OpenVpnClientService : IOpenVpnClientService
{
    private readonly ILogger<OpenVpnClientService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEasyRsaService _easyRsaService;
    private readonly string _pkiPath;
    private readonly string _caCetrPath;
    private readonly string _revokedDirPath;
    
    private readonly OpenVpnSettings _openVpnSettings;
    private readonly int _maxAttempts = 10;

    public OpenVpnClientService(ILogger<OpenVpnClientService> logger, IConfiguration configuration,
        IServiceProvider serviceProvider, IEasyRsaService easyRsaService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _easyRsaService = easyRsaService;
        _openVpnSettings = configuration.GetSection("OpenVpn").Get<OpenVpnSettings>() 
                           ?? throw new InvalidOperationException("OpenVpn configuration section is missing.");

        if (string.IsNullOrEmpty(_openVpnSettings.EasyRsaPath) ||
            string.IsNullOrEmpty(_openVpnSettings.OutputDir) ||
            string.IsNullOrEmpty(_openVpnSettings.TlsAuthKey) ||
            string.IsNullOrEmpty(_openVpnSettings.ServerIp))
        {
            throw new InvalidOperationException("One or more OpenVpn configuration values are missing.");
        }
        _pkiPath = Path.Combine(_openVpnSettings.EasyRsaPath, "pki");//todo:fix
        _caCetrPath = Path.Combine(_pkiPath, "ca.crt");
        _revokedDirPath = Path.Combine(_openVpnSettings.OutputDir, "revoked");
    }

    public async Task<GetAllFilesResult> GetAllClientConfigurations(long telegramId, 
        CancellationToken  cancellationToken)
    {
        var issuedOvpnFiles = await GetIssuedOvpnFilesByTelegramIdAsync(telegramId);
        _logger.LogInformation("Found {Count} issued files in database.", issuedOvpnFiles.Count);

        List<FileInfo> fileInfos = new List<FileInfo>();

        foreach (var issuedOvpnFile in issuedOvpnFiles)
        {
            if (issuedOvpnFile.FileName == string.Empty) throw new Exception("File name is empty.");
            string existingOvpnFilePath = Path.Combine(_openVpnSettings.OutputDir, issuedOvpnFile.FileName);
            _logger.LogInformation("Checking existence of file: {FilePath}", existingOvpnFilePath);

            if (File.Exists(existingOvpnFilePath))
            {
                _logger.LogInformation("File exists: {FilePath}", existingOvpnFilePath);
                fileInfos.Add(new FileInfo(existingOvpnFilePath));
            }
            else
            {
                _logger.LogCritical("File not found: {FilePath}", existingOvpnFilePath);
            }
        }

        var responseMessage = await GetResponseText(telegramId, "HereIsConfig", cancellationToken);
        _logger.LogInformation("Generated response message for user: {TelegramId}", telegramId);

        return new GetAllFilesResult
        {
            FileInfo = fileInfos,
            Message = responseMessage
        };
    }

    public async Task<FileCreationResult> CreateClientConfiguration(long telegramId, CancellationToken cancellationToken)
    {
        try
        {
            var issuedOvpnFiles = await GetIssuedOvpnFilesByTelegramIdAsync(telegramId);
            if (issuedOvpnFiles.Count >= _maxAttempts)
            {
                return new FileCreationResult { FileInfo = null, Message = 
                    await GetResponseText(telegramId, "MaxConfigError", cancellationToken) };
            }

            _logger.LogInformation("Step 1.1: Checking if configuration already exists for this client with TelegramId: {TelegramId}.", telegramId);

            int attempt = 0;
            string baseFileName = GetBaseFileNameForCerts(telegramId.ToString(), attempt);
            _logger.LogInformation("Step 1.2: Initial base file name generated: {BaseFileName}", baseFileName);

            string baseOvpnFileName = $"{baseFileName}.ovpn";
            string ovpnFilePath = Path.Combine(_openVpnSettings.OutputDir, baseOvpnFileName);

            _logger.LogInformation("Step 1.3:Initial .ovpn file path: {OvpnFilePath}", ovpnFilePath);

            while (File.Exists(ovpnFilePath) && attempt < _maxAttempts)
            {
                _logger.LogInformation("File already exists: {OvpnFilePath}. Incrementing attempt counter.", ovpnFilePath);
                attempt++;
                baseFileName = GetBaseFileNameForCerts(telegramId.ToString(), attempt);
                ovpnFilePath = Path.Combine(_openVpnSettings.OutputDir, $"{baseFileName}.ovpn");
                _logger.LogInformation("New file path after attempt {Attempt}: {OvpnFilePath}", attempt, ovpnFilePath);
            }

            if (attempt >= _maxAttempts)
            {
                _logger.LogError(
                    "Maximum limit of {MaxAttempts} configurations reached for client '{TelegramId}'. Cannot create more files.",
                    _maxAttempts, telegramId);
                throw new InvalidOperationException(
                    $"Maximum limit of {_maxAttempts} configurations for client '{telegramId}' has been reached. Cannot create more files.");
            }
            _logger.LogInformation("Step 1.4: Final file path determined: {OvpnFilePath}. Proceeding with configuration creation.", ovpnFilePath);

            _logger.LogInformation("Step 2: Building client certificate...");
            RevokeCertByCnName(baseFileName);//remove old certs with this CN name if we have
            var certificateResult =_easyRsaService.BuildCertificate($"{baseFileName}");

            _logger.LogInformation("Step 3: Defining paths to certificates and keys...");
            string caCertContent = _easyRsaService.ReadPemContent(_caCetrPath);

            string clientCertContent = _easyRsaService.ReadPemContent(certificateResult.CertificatePath);
            string clientKeyContent = await File.ReadAllTextAsync(certificateResult.KeyPath, cancellationToken);

            _logger.LogInformation("Step 4: Generating .ovpn configuration file...");
            string ovpnContent = GenerateOvpnFile(_openVpnSettings.ServerIp, caCertContent,
                clientCertContent, clientKeyContent, _openVpnSettings.TlsAuthKey);

            _logger.LogInformation("Step 5: Writing .ovpn file...");
            await File.WriteAllTextAsync(ovpnFilePath, ovpnContent, cancellationToken);

            _logger.LogInformation($"Client configuration file created: {ovpnFilePath}");
            var fileInfo = new FileInfo(ovpnFilePath);
            await SaveInfoInDataBase(telegramId, fileInfo, certificateResult);
            return new FileCreationResult { FileInfo = fileInfo, Message = await GetResponseText(telegramId,
                "HereIsConfig", cancellationToken) };

        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteAllClientConfigurations(long telegramId)
    {
        _logger.LogInformation("Starting deletion process for client with Telegram ID: {TelegramId}", telegramId);
        var issuedOvpnFiles = await GetIssuedOvpnFilesByTelegramIdAsync(telegramId);
        _logger.LogInformation("Found {Count} issued files in database for deletion.", issuedOvpnFiles.Count);
    
        foreach (var issuedOvpnFile in issuedOvpnFiles)
        {
            await RevokeCert(issuedOvpnFile, telegramId);
        }
    
        _logger.LogInformation("Completed deletion process for client with Telegram ID: {TelegramId}", telegramId);
    }

    public async Task DeleteClientConfiguration(long telegramId, string filename)
    {
        _logger.LogInformation("Starting deletion process for client with Telegram ID: {TelegramId}", telegramId);
        var issuedOvpnFile = await GetIssuedOvpnFilesByTelegramAndFileNameIdAsync(telegramId, filename);
        if (issuedOvpnFile != null)
        { 
            await RevokeCert(issuedOvpnFile, telegramId);
        }
    }

    private async Task RevokeCert(IssuedOvpnFile issuedOvpnFile, long telegramId)
    {
        string message = _easyRsaService.RevokeCertificate(issuedOvpnFile.CertName);
        _logger.LogInformation($"RevokeCertificate result: {message} for CertName: {issuedOvpnFile.CertName}");
        string revokedFilePath = MoveRevokedOvpnFile(issuedOvpnFile);
        _logger.LogInformation($"Successfully moved revoked .ovpn file to: {revokedFilePath}");

        await SetIsRevokeIssuedOvpnFile(issuedOvpnFile.Id, telegramId, revokedFilePath, 
            issuedOvpnFile.CertName, message);
        _logger.LogInformation($"Updated database for revoked certificate: {issuedOvpnFile.CertName}, " +
                               $"Telegram ID: {telegramId}");
    }

    private void RevokeCertByCnName(string baseFileName)
    {
        //looking for another cert with the same CN
        var oldCertSerials = _easyRsaService.FindAllCertificateInfoInIndexFile(baseFileName);
        foreach (var oldCertSerial in oldCertSerials)
        {
            _logger.LogInformation($"Older certificate found: {oldCertSerial}");
            
            string message = _easyRsaService.RevokeCertificate(oldCertSerial.CommonName);
            _logger.LogInformation($"Revoke old certificate result: {message} for CertName: {baseFileName}, " +
                                   $"Serial:{oldCertSerial.SerialNumber}");
        }
        oldCertSerials.Clear();
        var certInfoInIndexFile = _easyRsaService.FindAllCertificateInfoInIndexFile(baseFileName);
        if (certInfoInIndexFile.Count >= 1)
        {
            throw new Exception($"Conflict in index.txt. Please check index.txt CA for client {baseFileName}," +
                                $"{certInfoInIndexFile.FirstOrDefault()?.SerialNumber}");
        }
    }
    
    public bool CheckHealthFileSystem()
    {
        _easyRsaService.InstallEasyRsa();
        if (string.IsNullOrEmpty(_openVpnSettings.ServerIp))
            throw new ArgumentNullException(nameof(_openVpnSettings.ServerIp));
        
        Directory.CreateDirectory(_openVpnSettings.OutputDir);
        Directory.CreateDirectory(_revokedDirPath);
        
        if (!Directory.Exists(_openVpnSettings.OutputDir))
        {
            throw new FileNotFoundException("The output directory could not be found.");
        }
        if (!Directory.Exists(_revokedDirPath))
        {
            throw new FileNotFoundException("Revoked folder not found");
        }
        
        string indexFilePath = Path.Combine(_pkiPath, "index.txt");
        if (!File.Exists(indexFilePath))
        {
            throw new FileNotFoundException($"Index file not found at path: {indexFilePath}");
        }

        if (string.IsNullOrEmpty(_caCetrPath))
            throw new ArgumentNullException(nameof(_caCetrPath));
        if (string.IsNullOrEmpty(_openVpnSettings.TlsAuthKey))
            throw new ArgumentNullException(nameof(_openVpnSettings.TlsAuthKey));

        return true;
    }

    private string MoveRevokedOvpnFile(IssuedOvpnFile issuedOvpnFile)
    {
        string ovpnFilePath = Path.Combine(_openVpnSettings.OutputDir, issuedOvpnFile.FileName);
        
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var uniqueFileName = 
            $"{Path.GetFileNameWithoutExtension(issuedOvpnFile.FileName)}" +
            $"_{issuedOvpnFile.Id}" +
            $"_{timestamp}" +
            $"{Path.GetExtension(issuedOvpnFile.FileName)}";

        string revokedFilePath = Path.Combine(_revokedDirPath, uniqueFileName);

        if (File.Exists(ovpnFilePath))
        {
            File.Move(ovpnFilePath, revokedFilePath);
            _logger.LogInformation($"Moved .ovpn file to revoked folder: {revokedFilePath}");
        }
        else
        {
            _logger.LogWarning($".ovpn file not found for moving: {ovpnFilePath}");
        }

        return revokedFilePath; 
    }


    private async Task<string> GetResponseText(long telegramId, string key, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        return await localizationService.GetTextAsync(key, telegramId, cancellationToken);
    }

    private async Task SaveInfoInDataBase(long telegramId, FileInfo fileInfo, CertificateResult certificateResult)
    {
        using var scope = _serviceProvider.CreateScope();
        var issuedOvpnFileService = scope.ServiceProvider.GetRequiredService<IIssuedOvpnFileService>();
        await issuedOvpnFileService.AddIssuedOvpnFileAsync(telegramId, fileInfo, certificateResult);
    }
    
    private async Task<List<IssuedOvpnFile>> GetIssuedOvpnFilesByTelegramIdAsync(long telegramId)
    {
        using var scope = _serviceProvider.CreateScope();
        var issuedOvpnFileService = scope.ServiceProvider.GetRequiredService<IIssuedOvpnFileService>();
        return await issuedOvpnFileService.GetIssuedOvpnFilesByTelegramIdAsync(telegramId);
    }
    
    private async Task<IssuedOvpnFile?> GetIssuedOvpnFilesByTelegramAndFileNameIdAsync(long telegramId, string fileName)
    {
        using var scope = _serviceProvider.CreateScope();
        var issuedOvpnFileService = scope.ServiceProvider.GetRequiredService<IIssuedOvpnFileService>();
        return await issuedOvpnFileService.GetIssuedOvpnFilesByTelegramAndFileNameIdAsync(telegramId, fileName);
    }
    
    private async Task SetIsRevokeIssuedOvpnFile(int id, long telegramId, 
        string revokedFilePath, string certName, string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var issuedOvpnFileService = scope.ServiceProvider.GetRequiredService<IIssuedOvpnFileService>();
        await issuedOvpnFileService.SetIsRevokeIssuedOvpnFileByTelegramIdAndCertNameAsync(id, telegramId, 
            revokedFilePath, certName, message);
    }
    
    private string GetBaseFileNameForCerts(string fileName, int attempt)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var prefix = environment != "Production" ? $"{environment}_" : string.Empty;
        return $"{prefix}{fileName}_{attempt}";
    }
    
    private static string GenerateOvpnFile(string serverIp, string caCert, string clientCert, 
        string clientKey, string tlsAuthKey)
    {
        if (string.IsNullOrEmpty(serverIp))
            throw new ArgumentNullException(nameof(serverIp));
        if (string.IsNullOrEmpty(caCert))
            throw new ArgumentNullException(nameof(caCert));
        if (string.IsNullOrEmpty(clientCert))
            throw new ArgumentNullException(nameof(clientCert));
        if (string.IsNullOrEmpty(clientKey))
            throw new ArgumentNullException(nameof(clientKey));
        if (string.IsNullOrEmpty(tlsAuthKey))
            throw new ArgumentNullException(nameof(tlsAuthKey));
        
        return $@"client
dev tun
proto udp
remote {serverIp} 1291
resolv-retry infinite
nobind
remote-cert-tls server
tls-version-min 1.2
verify-x509-name raspberrypi_2e39d597-c642-4f69-a6c8-149e7c9ac064 name
cipher AES-256-CBC
auth SHA256
auth-nocache
verb 3
<ca>
{caCert}
</ca>
<cert>
{clientCert}
</cert>
<key>
{clientKey}
</key>
<tls-crypt>
{File.ReadAllText(tlsAuthKey)}
</tls-crypt>
";
    }
}