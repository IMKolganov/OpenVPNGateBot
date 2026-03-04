using DataGateVPNBot.Extensions;
using Xunit;

namespace DataGateVPNBot.Tests;

public class HostAddressNormalizerTests
{
    [Theory]
    [InlineData("tgbot.datagateapp.com", "tgbot.datagateapp.com")]
    [InlineData("tgbot.datagateapp.com/", "tgbot.datagateapp.com")]
    [InlineData("https://tgbot.datagateapp.com", "tgbot.datagateapp.com")]
    [InlineData("https://tgbot.datagateapp.com/", "tgbot.datagateapp.com")]
    [InlineData("http://tgbot.datagateapp.com/", "tgbot.datagateapp.com")]
    [InlineData("  https://foo.com/  ", "foo.com")]
    [InlineData("HTTP://EXAMPLE.COM/", "EXAMPLE.COM")]
    public void Normalize_RemovesProtocolAndTrailingSlash(string input, string expected)
    {
        var result = HostAddressNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_Null_ReturnsEmpty()
    {
        var result = HostAddressNormalizer.Normalize(null);
        Assert.Equal("", result);
    }

    [Fact]
    public void Normalize_WhitespaceOnly_ReturnsEmpty()
    {
        var result = HostAddressNormalizer.Normalize("   ");
        Assert.Equal("", result);
    }
}
