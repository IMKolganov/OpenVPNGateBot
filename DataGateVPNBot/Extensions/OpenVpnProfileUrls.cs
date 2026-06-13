namespace DataGateVPNBot.Extensions;

public static class OpenVpnProfileUrls
{
    public static string ToHttpsBase(string hostAddress)
    {
        var host = HostAddressNormalizer.Normalize(hostAddress);
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("HostAddress is not configured.");

        return $"https://{host}";
    }

    /// <summary>HTML landing page that redirects OpenVPN Connect to the profile download.</summary>
    public static string BuildProfilePageUrl(string hostAddress, string token)
    {
        var baseUrl = ToHttpsBase(hostAddress);
        return $"{baseUrl}/openvpn-api/profile?token={Uri.EscapeDataString(token)}";
    }

    /// <summary>Deep link consumed by OpenVPN Connect to import the profile from <see cref="BuildDownloadByTokenUrl"/>.</summary>
    public static string BuildConnectImportUri(string hostAddress, string token) =>
        $"openvpn://import-profile/{BuildDownloadByTokenUrl(hostAddress, token)}";

    public static string BuildDownloadByTokenUrl(string hostAddress, string token)
    {
        var baseUrl = ToHttpsBase(hostAddress);
        return $"{baseUrl}/DownloadByToken?token={Uri.EscapeDataString(token)}";
    }
}
