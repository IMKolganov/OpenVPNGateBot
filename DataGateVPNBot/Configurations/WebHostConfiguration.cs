using ILogger = Serilog.ILogger;

namespace DataGateVPNBot.Configurations;

public static class WebHostConfiguration
{
    public static void ConfigureWebHost(this WebApplicationBuilder builder, ILogger logger)
    {
        var config = builder.Configuration;

        var certPfxPath = config["CERTIFICATE_PFX_PATH"];
        var portStr = config["TELEGRAMBOT_PORT"];
        var port = int.TryParse(portStr, out var parsedPort) ? parsedPort : 8443;

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Configure(config.GetSection("Kestrel"));

            if (!string.IsNullOrWhiteSpace(certPfxPath))
            {
                if (File.Exists(certPfxPath))
                {
                    logger.Information($"🔐 HTTPS certificate found at: {certPfxPath}");
                    serverOptions.ListenAnyIP(port, listen => listen.UseHttps(certPfxPath));
                }
                else
                {
                    logger.Warning($"⚠ Certificate file '{certPfxPath}' not found. Falling back to HTTP only.");
                    serverOptions.ListenAnyIP(port);
                }
            }
            else
            {
                logger.Warning("⚠ CERTIFICATE_PFX_PATH not provided. Running HTTP only.");
                serverOptions.ListenAnyIP(port);
            }
        });
    }
}