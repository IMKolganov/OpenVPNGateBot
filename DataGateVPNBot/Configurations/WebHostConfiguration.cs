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
            options.ListenAnyIP(80);

            var certPath = config["CERTIFICATE_PEM_PATH"] ?? "/app/certificates/datagatetgbot.pem";
            var keyPath = config["CERTIFICATE_KEY_PATH"] ?? "/app/certificates/datagatetgbot.key";

            if (File.Exists(certPath) && File.Exists(keyPath))
            {
                try
                {
                    var cert = X509Certificate2.CreateFromPemFile(certPath, keyPath);
                    cert = new X509Certificate2(cert.Export(X509ContentType.Pkcs12)); // make cert usable for Kestrel

                    options.ListenAnyIP(443, listen =>
                    {
                        listen.UseHttps(cert);
                    });
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "❌ Failed to load certificate from PEM/KEY: {Cert}, {Key}", certPath, keyPath);
                }
            }
            else
            {
                logger.Warning("⚠ No certificate files found at '{Cert}' and '{Key}'. HTTPS will not be enabled.",
                    certPath, keyPath);
            }
        });
    }
}