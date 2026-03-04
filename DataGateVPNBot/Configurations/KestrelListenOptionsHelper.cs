namespace DataGateVPNBot.Configurations;

/// <summary>
/// Helper for Kestrel listen options used by WebHostConfiguration. Exposed for unit testing.
/// </summary>
public static class KestrelListenOptionsHelper
{
    public static bool GetUseCertificate(IConfiguration configuration)
    {
        if (bool.TryParse(Environment.GetEnvironmentVariable("USE_CERTIFICATE"), out var env))
            return env;
        return configuration.GetValue<bool>("BotConfiguration:UseCertificate");
    }

    public static int GetListenPort(IConfiguration configuration)
    {
        if (int.TryParse(Environment.GetEnvironmentVariable("TELEGRAMBOT_LISTEN_PORT"), out var port) && port > 0)
            return port;
        return 5050;
    }
}
