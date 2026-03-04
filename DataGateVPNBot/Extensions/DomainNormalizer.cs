namespace DataGateVPNBot.Extensions;

public static class DomainNormalizer
{
    public static string Normalize(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domain is required.", nameof(domain));

        if (domain.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(domain);
            return uri.Host;
        }

        var colon = domain.LastIndexOf(':');
        if (colon > -1)
        {
            var hostPart = domain[..colon].Trim();
            if (!string.IsNullOrWhiteSpace(hostPart))
                return hostPart;
        }

        return domain.Trim();
    }
}
