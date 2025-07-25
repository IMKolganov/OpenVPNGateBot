using ILogger = Serilog.ILogger;

namespace DataGateVPNBot.Configurations;

public static class WebHostConfiguration
{
    public static void ConfigureWebHost(this WebApplicationBuilder builder, ILogger logger)
    {
        var config = builder.Configuration;

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            // Load full Kestrel config (including HTTPS cert paths) from env or config files
            serverOptions.Configure(config.GetSection("Kestrel"));

            logger.Information("Kestrel is configured via environment or appsettings.");
        });
    }
}