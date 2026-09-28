using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DataGateVPNBot.Services.CryptoPay;

public static class CryptoPayWebhookSignature
{
    public const string HeaderName = "crypto-pay-api-signature";

    public static bool IsValid(string apiToken, string rawBody, string? signature)
    {
        if (string.IsNullOrWhiteSpace(apiToken) || rawBody is null || string.IsNullOrWhiteSpace(signature))
            return false;

        var expected = ComputeHex(apiToken, rawBody);
        if (!TryDecodeHex(expected, out var expectedBytes) ||
            !TryDecodeHex(signature.Trim(), out var actualBytes) ||
            expectedBytes.Length != actualBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static bool TryDecodeHex(string hex, out byte[] bytes)
    {
        bytes = [];
        try
        {
            bytes = Convert.FromHexString(hex);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string ComputeHex(string apiToken, string rawBody)
    {
        var secret = SHA256.HashData(Encoding.UTF8.GetBytes(apiToken));
        var hash = HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(rawBody));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool TryGetHeader(IHeaderDictionary headers, out string? signature)
    {
        signature = null;
        if (!headers.TryGetValue(HeaderName, out var values))
            return false;
        signature = values.ToString();
        return !string.IsNullOrWhiteSpace(signature);
    }

    public static string FormatUsd(decimal amount) =>
        amount.ToString("0.##", CultureInfo.InvariantCulture);
}
