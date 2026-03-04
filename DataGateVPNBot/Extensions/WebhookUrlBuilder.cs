namespace DataGateVPNBot.Extensions;

public static class WebhookUrlBuilder
{
    public static string Build(string host, int port)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host is required.", nameof(host));

        var portPart = port == 443 ? "" : $":{port}";
        return $"https://{host.Trim()}{portPart}/api/bot";
    }
}
