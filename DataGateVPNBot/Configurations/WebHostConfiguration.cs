namespace DataGateVPNBot.Configurations;

public static class WebHostConfiguration
{
    public static void ConfigureWebHost(this WebApplicationBuilder builder, Serilog.ILogger? logger = null)
    {
        builder.WebHost.ConfigureKestrel((context, options) =>
        {
            var useCert = KestrelListenOptionsHelper.GetUseCertificate(context.Configuration);
            var listenPort = KestrelListenOptionsHelper.GetListenPort(context.Configuration);

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

}