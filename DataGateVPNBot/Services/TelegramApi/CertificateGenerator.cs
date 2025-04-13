using System.Diagnostics;
using DataGateVPNBot.Models.Configurations;

namespace DataGateVPNBot.Services.TelegramApi;

public class CertificateGenerator
{
    private readonly ILogger<CertificateGenerator> _logger;
    private readonly BotConfiguration _config;

    public CertificateGenerator(ILogger<CertificateGenerator> logger, BotConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public Stream EnsureCertificate(string hostAddress)
    {
        var certPath = !string.IsNullOrEmpty(_config.CertificatePath) ? _config.CertificatePath : "datagatetgbot.pem";
        if (string.IsNullOrEmpty(certPath))
            throw new InvalidOperationException("CertificatePath is not set in the configuration.");

        var certDir = Path.GetDirectoryName(certPath);
        if (string.IsNullOrWhiteSpace(certDir))
            certDir = Directory.GetCurrentDirectory();
        Directory.CreateDirectory(certDir);
        
        var keyPath = Path.ChangeExtension(certPath, ".key");
        var pemPath = Path.ChangeExtension(certPath, ".pem");
        var cnfPath = Path.Combine(certDir, "datagatetgbot.cnf");

        // Check OpenSSL
        if (!IsOpenSslAvailable())
            throw new InvalidOperationException("OpenSSL is not installed or not available in PATH.");

        // If all certificate files exist — return PEM stream
        if (File.Exists(pemPath))
        {
            _logger.LogInformation("✅ Existing certificate found. Using existing PEM.");
            return File.OpenRead(pemPath);
        }

        _logger.LogWarning("⚠ Certificate not found. Generating new self-signed certificate...");
        
        var cnf = $"""
                   [req]
                   default_bits = 2048
                   prompt = no
                   default_md = sha256
                   req_extensions = req_ext
                   distinguished_name = dn

                   [dn]
                   CN = {hostAddress}

                   [req_ext]
                   subjectAltName = @alt_names

                   [alt_names]
                   IP.1 = {hostAddress}
                   """;
        File.WriteAllText(cnfPath, cnf);

        var result = RunOpenSslCommand(new[]
        {
            "req", "-x509", "-nodes", "-days", "365",
            "-newkey", "rsa:2048",
            "-keyout", keyPath,
            "-out", certPath,
            "-config", cnfPath,
            "-extensions", "req_ext"
        });

        if (!result.Success)
        {
            _logger.LogError("❌ OpenSSL error:\n{Error}", result.Error);
            throw new Exception("OpenSSL certificate generation failed.");
        }

        _logger.LogInformation($"✅ Certificate generated at: {certPath}");
        _logger.LogInformation($"🔑 Key file generated at: {keyPath}");
        _logger.LogInformation($"📦 PEM file exported at: {pemPath}");
        _logger.LogInformation($"✅ Certificate generated and PEM exported: {pemPath}");

        if (!File.Exists(pemPath))
        {
            File.Copy(certPath, pemPath);
        }

        var stream = File.OpenRead(pemPath);
        return stream;
    }


    private static bool IsOpenSslAvailable()
    {
        try
        {
            var result = RunOpenSslCommand(["version"]);
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    private static (bool Success, string Output, string Error) RunOpenSslCommand(string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "openssl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

        using var process = Process.Start(startInfo);
        if (process == null)
            return (false, "", "Failed to start openssl");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode == 0, output, error);
    }
}

