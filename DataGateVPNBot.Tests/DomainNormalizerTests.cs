using DataGateVPNBot.Extensions;
using Xunit;

namespace DataGateVPNBot.Tests;

public class DomainNormalizerTests
{
    [Theory]
    [InlineData("example.com", "example.com")]
    [InlineData("  foo.bar.com  ", "foo.bar.com")]
    public void Normalize_PlainDomain_ReturnsTrimmed(string input, string expected)
    {
        var result = DomainNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://tgbot.datagateapp.com", "tgbot.datagateapp.com")]
    [InlineData("http://example.com/path", "example.com")]
    [InlineData("HTTPS://FOO.COM/", "foo.com")]
    public void Normalize_WithProtocol_ExtractsHost(string input, string expected)
    {
        var result = DomainNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("example.com:8080", "example.com")]
    [InlineData("host:443", "host")]
    public void Normalize_WithPort_StripsPort(string input, string expected)
    {
        var result = DomainNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_Null_Throws()
    {
        Assert.Throws<ArgumentException>(() => DomainNormalizer.Normalize(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_Whitespace_Throws(string input)
    {
        Assert.Throws<ArgumentException>(() => DomainNormalizer.Normalize(input));
    }
}
