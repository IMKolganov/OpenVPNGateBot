using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace DataGateVPNBot.Helpers;

public static class TelegramInitDataValidator
{
    public static bool Validate(string initData, string botToken, TimeSpan expIn, out string? error)
    {
        error = null;
        if (string.IsNullOrEmpty(initData)) { error = "unexpected init data format"; return false; }
        if (string.IsNullOrEmpty(botToken)) { error = "bot token is required"; return false; }

        var q = TryParseQueryPublic(initData);

        string? tgHash = null;
        DateTimeOffset? authDate = null;
        var pairs = new List<string>(q.Count);

        foreach (var kv in q)
        {
            var k = kv.Key;
            var values = kv.Value;
            if (values.Count == 0) continue;

            if (k == "hash")
            {
                tgHash = values[0];
                continue;
            }
            if (k == "auth_date")
            {
                if (!long.TryParse(values[0], out var unix))
                {
                    error = "parse auth_date to int64: auth_date is invalid";
                    return false;
                }
                authDate = DateTimeOffset.FromUnixTimeSeconds(unix);
            }

            pairs.Add($"{k}={values[0]}");
        }

        if (string.IsNullOrEmpty(tgHash))
        {
            error = "hash sign is missing";
            return false;
        }

        if (expIn > TimeSpan.Zero)
        {
            if (authDate is null)
            {
                error = "auth_date is missing";
                return false;
            }
            if (authDate.Value.Add(expIn) < DateTimeOffset.UtcNow)
            {
                error = "init data is expired";
                return false;
            }
        }

        pairs.Sort(StringComparer.Ordinal);
        var dcs = string.Join("\n", pairs);

        var calc = ComputeHashPublic(dcs, botToken);
        if (!HexEqual(calc, tgHash))
        {
            error = "hash sign is invalid";
            return false;
        }
        return true;
    }

    private static Dictionary<string, List<string>> TryParseQueryPublic(string query)
    {
        var parsed = QueryHelpers.ParseQuery(query);
        var dict = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var kv in parsed)
            dict[kv.Key] = kv.Value.ToArray().ToList()!;
        return dict;
    }

    private static byte[] SeedHmacWebAppData(string botToken)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("WebAppData"));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(botToken));
    }

    private static string ComputeHashPublic(string dataCheckString, string botToken)
    {
        var secret = SeedHmacWebAppData(botToken);
        using var h = new HMACSHA256(secret);
        var bytes = h.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool HexEqual(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        if (a.Length != b.Length) return false;

        int diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            int ca = a[i] | 0x20;
            int cb = b[i] | 0x20;
            diff |= (ca ^ cb);
        }
        return diff == 0;
    }
}