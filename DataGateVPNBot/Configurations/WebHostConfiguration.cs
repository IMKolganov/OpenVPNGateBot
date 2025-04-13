using ILogger = Serilog.ILogger;

namespace DataGateVPNBot.Configurations;

public static class WebHostConfiguration
{
    public static void ConfigureWebHost(this WebApplicationBuilder builder, ILogger logger)
    {
        var config = builder.Configuration;

        var certPath = config["CERTIFICATE_PATH"];
        var portStr = config["TELEGRAMBOT_PORT"];
        var port = int.TryParse(portStr, out var parsedPort) ? parsedPort : 8443;

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Configure(config.GetSection("Kestrel"));

            if (!string.IsNullOrWhiteSpace(certPath))
            {
                if (File.Exists(certPath))
                {
                    logger.Information($"🔐 HTTPS certificate found at: {certPath}");

                    serverOptions.ListenAnyIP(port, listen =>
                    {
                        listen.UseHttps(certPath);
                    });
                }
                else
                {
                    logger.Warning($"⚠ Certificate file '{certPath}' not found. Falling back to HTTP only.");
                    serverOptions.ListenAnyIP(port);
                }
            }
            else
            {
                logger.Warning("⚠ CERTIFICATE_PATH not provided. Running HTTP only.");
                serverOptions.ListenAnyIP(port);
            }
        });
    }
}