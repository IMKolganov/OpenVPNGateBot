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

    public void EnsureCertificate(string hostAddress)
    {
        var certPath = !string.IsNullOrEmpty(_config.CertificatePfxPath)
            ? _config.CertificatePfxPath
            : "datagatetgbot.pfx";

        if (string.IsNullOrEmpty(certPath))
            throw new InvalidOperationException("CertificatePath is not set in the configuration.");

        var certDir = Path.GetDirectoryName(certPath);
        if (string.IsNullOrWhiteSpace(certDir))
            certDir = Directory.GetCurrentDirectory();

        var keyPath = Path.Combine(certDir, "datagatetgbot.key");
        var crtPath = Path.Combine(certDir, "datagatetgbot.crt");
        var pemPath = Path.Combine(certDir, "datagatetgbot.pem");
        var cnfPath = Path.Combine(certDir, "datagatetgbot.cnf");

        if (!IsOpenSslAvailable())
            throw new InvalidOperationException("OpenSSL is not installed or not available in PATH.");

        if (File.Exists(certPath) && File.Exists(crtPath))
        {
            _logger.LogInformation($"✅ Existing certificate found: {certPath}");
            return;
        }

        _logger.LogWarning("⚠ Certificate not found. Generating new self-signed certificate...");

        var cnf = $"""
        [req]
        default_bits = 2048
        prompt = no
        default_md = sha256
        distinguished_name = dn
        req_extensions = req_ext

        [dn]
        C = CY
        ST = Cyprus
        L = Nicosia
        O = DataGate
        CN = {hostAddress}

        [req_ext]
        subjectAltName = @alt_names
        basicConstraints = critical, CA:FALSE
        keyUsage = critical, digitalSignature, keyEncipherment
        extendedKeyUsage = serverAuth

        [alt_names]
        IP.1 = {hostAddress}
        """;
        File.WriteAllText(cnfPath, cnf);

        var genCert = RunOpenSslCommand(new[]
        {
            "req", "-x509", "-nodes", "-days", "365",
            "-newkey", "rsa:2048",
            "-keyout", keyPath,
            "-out", crtPath,
            "-config", cnfPath,
            "-extensions", "req_ext"
        });

        if (!genCert.Success)
        {
            _logger.LogError($"❌ OpenSSL cert error:\n{genCert.Error}");
            throw new Exception("OpenSSL certificate generation failed.");
        }

        File.Copy(crtPath, pemPath, true);

        _logger.LogInformation($"✅ CRT and KEY generated:\n - CRT: {crtPath}\n - KEY: {keyPath}");
        _logger.LogInformation($"📦 PEM exported: {pemPath}");

        var pfxPath = certPath;
        var genPfx = RunOpenSslCommand(new[]
        {
            "pkcs12", "-export",
            "-out", pfxPath,
            "-inkey", keyPath,
            "-in", crtPath,
            "-passout", "pass:"
        });

        if (!genPfx.Success)
        {
            _logger.LogError($"❌ OpenSSL pfx export error:\n{genPfx.Error}");
            throw new Exception("OpenSSL PFX export failed.");
        }

        _logger.LogInformation($"📦 PFX certificate created: {pfxPath}");
        
        _logger.LogInformation("Try to restart app...");
        Environment.Exit(0);
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
