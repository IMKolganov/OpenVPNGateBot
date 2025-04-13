namespace DataGateVPNBot.Configurations;

public static class WebHostConfiguration
{
    public static void ConfigureWebHost(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        var certPath = config["CERTIFICATE_PATH"]; 
        var portStr = config["TELEGRAMBOT_PORT"];

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Configure(config.GetSection("Kestrel"));

            if (!string.IsNullOrWhiteSpace(certPath))
            {
                var port = int.TryParse(portStr, out var parsedPort) ? parsedPort : 8443;

                serverOptions.ListenAnyIP(port, listen =>
                {
                    listen.UseHttps(certPath);
                });
            }
        });
    }
}