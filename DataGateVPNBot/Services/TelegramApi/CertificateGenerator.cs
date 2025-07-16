using System.Diagnostics;
using DataGateVPNBot.Models.Configurations;
using DataGateVPNBot.Services.Interfaces;

namespace DataGateVPNBot.Services.TelegramApi;

public class CertificateGenerator(ILogger<CertificateGenerator> logger, BotConfiguration config, 
    IErrorService  errorService)
{
    public async Task EnsureCertificateAsync(string hostAddress, CancellationToken cancellationToken)
    {
        await errorService.SendMessageToAdminsAsync("🔐 Verifying certificate...", cancellationToken);

        var certPath = !string.IsNullOrEmpty(config.CertificatePfxPath)
            ? config.CertificatePfxPath
            : "certificates/datagatetgbot.pfx";

        if (string.IsNullOrEmpty(certPath))
            throw new InvalidOperationException("CertificatePath is not set in the configuration.");

        var certDir = Path.GetDirectoryName(certPath);
        if (string.IsNullOrWhiteSpace(certDir))
            certDir = Directory.GetCurrentDirectory();

        if (!Directory.Exists(certDir))
            Directory.CreateDirectory(certDir);


        var keyPath = Path.Combine(certDir, "datagatetgbot.key");
        var crtPath = Path.Combine(certDir, "datagatetgbot.crt");
        var pemPath = Path.Combine(certDir, "datagatetgbot.pem");
        var cnfPath = Path.Combine(certDir, "datagatetgbot.cnf");

        if (!IsOpenSslAvailable())
        {
            await errorService.SendMessageToAdminsAsync("🔐❌ OpenSSL is not installed or not available in PATH.", 
                cancellationToken);
            throw new InvalidOperationException("OpenSSL is not installed or not available in PATH.");
        }

        if (File.Exists(certPath) && File.Exists(crtPath))
        {
            logger.LogInformation($"✅ Existing certificate found: {certPath}");
            await errorService.SendMessageToAdminsAsync($"✅ Existing certificate found:\n`{certPath}`", cancellationToken);
            return;
        }

        await errorService.SendMessageToAdminsAsync(
            "⚠️ Certificate not found. Generating a new self-signed certificate...",  cancellationToken);

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
        await File.WriteAllTextAsync(cnfPath, cnf, cancellationToken);

        var genCert = RunOpenSslCommand([
            "req", "-x509", "-nodes", "-days", "365",
            "-newkey", "rsa:2048",
            "-keyout", keyPath,
            "-out", crtPath,
            "-config", cnfPath,
            "-extensions", "req_ext"
        ]);

        if (!genCert.Success)
        {
            logger.LogError($"❌ OpenSSL cert error:\n{genCert.Error}");
            await errorService.SendMessageToAdminsAsync(
                $"❌ OpenSSL certificate generation failed:\n{genCert.Error}", cancellationToken);
            throw new Exception("OpenSSL certificate generation failed.");
        }

        File.Copy(crtPath, pemPath, true);
        logger.LogInformation($"✅ CRT and KEY generated:\n - CRT: {crtPath}\n - KEY: {keyPath}");
        await errorService.SendMessageToAdminsAsync(
            $"✅ Certificate and key generated.\nCRT: `{crtPath}`\nKEY: `{keyPath}`",  cancellationToken);

        logger.LogInformation($"📦 PEM exported: {pemPath}");
        await errorService.SendMessageToAdminsAsync($"📦 PEM file exported:\n`{pemPath}`", cancellationToken);

        var genPfx = RunOpenSslCommand([
            "pkcs12", "-export",
            "-out", certPath,
            "-inkey", keyPath,
            "-in", crtPath,
            "-passout", "pass:"
        ]);

        if (!genPfx.Success)
        {
            logger.LogError($"❌ OpenSSL pfx export error:\n{genPfx.Error}");
            await errorService.SendMessageToAdminsAsync(
                $"❌ Failed to export PFX certificate:\n{genPfx.Error}", cancellationToken);
            throw new Exception("OpenSSL PFX export failed.");
        }

        logger.LogInformation($"📦 PFX certificate created: {certPath}");
        await errorService.SendMessageToAdminsAsync(
            $"📦 PFX certificate successfully created:\n`{certPath}`", cancellationToken);

        await errorService.SendMessageToAdminsAsync(
            "♻️ Restarting the application to apply the new certificate...", cancellationToken);
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
