using System.Security.Cryptography;
using System.Text;
using DataGateVPNBot.Helpers;
using Xunit;

namespace DataGateVPNBot.Tests;

public class TelegramInitDataValidatorTests
{
    private static string BuildValidInitData(string botToken, IReadOnlyDictionary<string, string> kv, long authDateUnix)
    {
        var dict = new Dictionary<string, string>(kv, StringComparer.Ordinal)
        {
            ["auth_date"] = authDateUnix.ToString()
        };
        var dataCheck = string.Join("\n",
            dict.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"));

        var secretKey = HmacSha256(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
        var hashBytes = HmacSha256(secretKey, Encoding.UTF8.GetBytes(dataCheck));
        var hashHex = ToHexLower(hashBytes);
        dict["hash"] = hashHex;

        return string.Join("&", dict.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
    }

    private static byte[] HmacSha256(byte[] key, byte[] data)
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

    [Fact]
    public void ValidateAndParse_Valid_InitData_Returns_Parsed_Data()
    {
        const string botToken = "test-bot-token";
        var authDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var kv = new Dictionary<string, string>
        {
            ["user"] = "{\"id\":123,\"first_name\":\"Test\",\"username\":\"testuser\"}"
        };
        var initData = BuildValidInitData(botToken, kv, authDate);
        var validator = new TelegramInitDataValidator(botToken);

        var result = validator.ValidateAndParse(initData, expiresInSeconds: 3600);

        Assert.NotNull(result);
        Assert.Equal(123, result.User?.Id);
        Assert.Equal("Test", result.User?.FirstName);
        Assert.Equal("testuser", result.User?.Username);
        Assert.Equal(authDate, result.AuthDateUnix);
    }

    [Fact]
    public void ValidateAndParse_Throws_When_Hash_Missing()
    {
        var validator = new TelegramInitDataValidator("token");
        var initData = "auth_date=1234567890"; // no hash

        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAndParse(initData, 3600));
        Assert.Contains("hash", ex.Message);
    }

    [Fact]
    public void ValidateAndParse_Throws_When_Hash_Invalid()
    {
        const string botToken = "test-bot-token";
        var authDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var kv = new Dictionary<string, string> { ["auth_date"] = authDate.ToString(), ["hash"] = "invalidhash" };
        var initData = $"auth_date={authDate}&hash=invalidhash";
        var validator = new TelegramInitDataValidator(botToken);

        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAndParse(initData, 3600));
        Assert.Contains("Invalid hash", ex.Message);
    }

    [Fact]
    public void ValidateAndParse_Throws_When_Expired()
    {
        const string botToken = "test-bot-token";
        var authDate = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds();
        var kv = new Dictionary<string, string>();
        var initData = BuildValidInitData(botToken, kv, authDate);
        var validator = new TelegramInitDataValidator(botToken);

        var ex = Assert.Throws<InvalidOperationException>(() => validator.ValidateAndParse(initData, expiresInSeconds: 3600));
        Assert.Contains("expired", ex.Message);
    }

    [Fact]
    public void ValidateAndParse_Accepts_When_Within_Expiry()
    {
        const string botToken = "test-bot-token";
        var authDate = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds();
        var kv = new Dictionary<string, string>();
        var initData = BuildValidInitData(botToken, kv, authDate);
        var validator = new TelegramInitDataValidator(botToken);

        var result = validator.ValidateAndParse(initData, expiresInSeconds: 3600);

        Assert.NotNull(result);
        Assert.Equal(authDate, result.AuthDateUnix);
    }
}
