using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using ILogger = Serilog.ILogger;

namespace DataGateVPNBot.Configurations;

public static class WebHostConfiguration
{
    public static void ConfigureWebHost(this WebApplicationBuilder builder, ILogger logger)
    {
        var config = builder.Configuration;

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(80); // HTTP — for Let's Encrypt also for debug

            var certPath = config["CERTIFICATE_PEM_PATH"] ?? "/app/certificates/datagatetgbot.pem";
            var keyPath = config["CERTIFICATE_KEY_PATH"] ?? "/app/certificates/datagatetgbot.key";

            if (File.Exists(certPath) && File.Exists(keyPath))
            {
                try
                {
                    var certificate = X509Certificate2.CreateFromPemFile(certPath, keyPath)
                        .CopyWithPrivateKey(LoadPrivateRsaKey(keyPath));

                    options.ListenAnyIP(443, listen =>
                    {
                        listen.UseHttps(certificate);
                    });

                    logger.Information("✅ HTTPS enabled using certificate from {CertPath}", certPath);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "❌ Failed to load certificate from PEM/KEY: {CertPath}, {KeyPath}", certPath, keyPath);
                }
            }
            else
            {
                logger.Warning("⚠️ Missing certificate files: {CertPath} or {KeyPath}", certPath, keyPath);
            }
        });
    }

    private static RSA LoadPrivateRsaKey(string keyPath)
    {
        var keyText = File.ReadAllText(keyPath);

        RSA rsa = RSA.Create();
        rsa.ImportFromPem(keyText.ToCharArray());
        return rsa;
    }
}