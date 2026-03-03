namespace DataGateVPNBot.Configurations;

public static class WebHostConfiguration
{
    public static void ConfigureWebHost(this WebApplicationBuilder builder, Serilog.ILogger? logger = null)
    {
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            var useCert = GetUseCertificate(context.Configuration);
            var listenPort = GetListenPort(context.Configuration);

            if (!useCert)
            {
                options.ListenAnyIP(listenPort);
                logger?.Information(
                    "USE_CERTIFICATE=false: Kestrel listening HTTP only on port {Port}.",
                    listenPort);
            }
            else
            {
                options.Configure(context.Configuration.GetSection("Kestrel"));
            }
        });
    }

    private static bool GetUseCertificate(IConfiguration configuration)
    {
        if (bool.TryParse(Environment.GetEnvironmentVariable("USE_CERTIFICATE"), out var env))
            return env;
        return configuration.GetValue<bool>("BotConfiguration:UseCertificate");
    }

    private static int GetListenPort(IConfiguration configuration)
    {
        if (int.TryParse(Environment.GetEnvironmentVariable("TELEGRAMBOT_LISTEN_PORT"), out var port) && port > 0)
            return port;
        return 5050;
    }
}