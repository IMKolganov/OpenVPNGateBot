namespace DataGateVPNBot.Extensions;

public static class HostAddressNormalizer
{
    public static string Normalize(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return string.Empty;

        return host
            .Trim()
            .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/')
            .Trim();
    }
}
