using DataGateVPNBot.Helpers;
using Xunit;

namespace DataGateVPNBot.Tests.Helpers;

public class AccountLinkCodeParserTests
{
    [Theory]
    [InlineData("ABCD2345", true, "ABCD2345")]
    [InlineData("abcd2345", true, "ABCD2345")]
    [InlineData("/link_account ABCD2345", false, "")]
    [InlineData("SHORT", false, "")]
    [InlineData("ABCD234!", false, "")]
    [InlineData("12345678", false, "")]
    public void TryExtract_ParsesEightCharacterCodes(string input, bool expected, string expectedCode)
    {
        var ok = AccountLinkCodeParser.TryExtract(input, out var code);

        Assert.Equal(expected, ok);
        Assert.Equal(expectedCode, code);
    }

    [Theory]
    [InlineData("ABCD2345 extra", true, "ABCD2345")]
    [InlineData("  abcd2345  ", true, "ABCD2345")]
    [InlineData("abc", false, "")]
    public void TryNormalizeToken_ParsesFirstToken(string input, bool expected, string expectedCode)
    {
        var ok = AccountLinkCodeParser.TryNormalizeToken(input, out var code);

        Assert.Equal(expected, ok);
        Assert.Equal(expectedCode, code);
    }

    [Theory]
    [InlineData("link_ABCD2345", true, "ABCD2345")]
    [InlineData("LINK_abcd2345", true, "ABCD2345")]
    [InlineData("link_SHORT", false, "")]
    [InlineData("ABCD2345", false, "")]
    [InlineData(null, false, "")]
    public void TryParseStartPayload_ParsesDeepLinkPayload(string? input, bool expected, string expectedCode)
    {
        var ok = AccountLinkCodeParser.TryParseStartPayload(input, out var code);

        Assert.Equal(expected, ok);
        Assert.Equal(expectedCode, code);
    }

    [Theory]
    [InlineData("ABCD2345", "[account-link-code-redacted]")]
    [InlineData("/link_account ABCD2345", "/link_account [redacted]")]
    [InlineData("/start link_ABCD2345", "/start link_[redacted]")]
    [InlineData("hello", "hello")]
    public void RedactSensitiveMessageText_RedactsLinkCodes(string input, string expected)
    {
        Assert.Equal(expected, AccountLinkCodeParser.RedactSensitiveMessageText(input));
    }
}
