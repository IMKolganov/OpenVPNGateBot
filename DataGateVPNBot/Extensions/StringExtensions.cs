using System.Net;

namespace DataGateVPNBot.Extensions;

public static class StringExtensions
{
    public static bool IsDomainName(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        input = input.Trim();

        // If URL provided -> extract host
        if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
                return false;

            input = uri.Host;
        }

        // Must be DNS, not IP address
        return Uri.CheckHostName(input) == UriHostNameType.Dns &&
               !IPAddress.TryParse(input, out _);
    }
}