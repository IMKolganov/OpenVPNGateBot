using System.Security.Cryptography;
using System.Text;

namespace DataGateVPNBot.Helpers;

public sealed class TelegramInitDataValidator(string botToken)
{
    private readonly byte[] _secretKey = ComputeHmac(Encoding.UTF8.GetBytes("WebAppData"), botToken);

    // secretKey = HMAC_SHA256("WebAppData", botToken)

    public TelegramInitData ValidateAndParse(string initDataRaw, int expiresInSeconds)
    {
        var kv = ParseQuery(initDataRaw);

        if (!kv.TryGetValue("hash", out var givenHash) || string.IsNullOrWhiteSpace(givenHash))
            throw new InvalidOperationException("hash is missing");

        // Build data-check-string
        var dataCheck = string.Join("\n",
            kv.Where(p => !p.Key.Equals("hash", StringComparison.Ordinal))
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => $"{p.Key}={p.Value}")
        );

        var expected = ComputeHmac(_secretKey, dataCheck);
        var expectedHex = ToHexLower(expected);

        if (!FixedTimeEquals(givenHash, expectedHex))
            throw new InvalidOperationException("Invalid hash (signature mismatch)");

        // Expiry check
        if (kv.TryGetValue("auth_date", out var authStr) && long.TryParse(authStr, out var authUnix))
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now - authUnix > expiresInSeconds)
                throw new InvalidOperationException("initData expired");
        }

        return TelegramInitData.FromDictionary(kv);
    }

    private static Dictionary<string, string> ParseQuery(string raw)
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = part.IndexOf('=');
            if (i <= 0) continue;
            var k = Uri.UnescapeDataString(part[..i]);
            var v = Uri.UnescapeDataString(part[(i + 1)..]);
            d[k] = v;
        }
        return d;
    }

    private static byte[] ComputeHmac(byte[] key, string data)
        => ComputeHmac(key, Encoding.UTF8.GetBytes(data));

    private static byte[] ComputeHmac(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    private static string ToHexLower(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aLower = a.Trim().ToLowerInvariant();
        var bLower = b.Trim().ToLowerInvariant();
        if (aLower.Length != bLower.Length) return false;

        var diff = 0;
        for (int i = 0; i < aLower.Length; i++)
            diff |= aLower[i] ^ bLower[i];

        return diff == 0;
    }
}