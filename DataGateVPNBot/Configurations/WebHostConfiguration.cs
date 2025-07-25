using ILogger = Serilog.ILogger;

namespace DataGateVPNBot.Configurations;

public static class WebHostConfiguration
{
    public static void ConfigureWebHost(this WebApplicationBuilder builder, ILogger logger)
    {
        var config = builder.Configuration;

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(80); // for Let's Encrypt challenge

            var certPath = config["CERTIFICATE_PEM_PATH"] ?? "/app/certificates/datagatetgbot.pem";
            var keyPath = config["CERTIFICATE_KEY_PATH"] ?? "/app/certificates/datagatetgbot.key";

            if (File.Exists(certPath) && File.Exists(keyPath))
            {
                options.ListenAnyIP(443, listen =>
                {
                    listen.UseHttps(certPath, keyPath);
                });
            }
            else
            {
                logger.Warning("⚠ No certificate files found at '{Cert}' and '{Key}'. HTTPS will not be enabled.", 
                    certPath, keyPath);
            }
        });
    }
}