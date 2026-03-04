using DataGateVPNBot.Extensions;
using Xunit;

namespace DataGateVPNBot.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("tgbot.datagateapp.com", true)]
    [InlineData("example.com", true)]
    [InlineData("sub.domain.example.org", true)]
    [InlineData("https://tgbot.datagateapp.com", true)]
    [InlineData("http://example.com/path", true)]
    public void IsDomainName_ValidDomain_ReturnsTrue(string input, bool _)
    {
        Assert.True(input.IsDomainName());
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("0.0.0.0")]
    [InlineData("::1")]
    public void IsDomainName_IPAddress_ReturnsFalse(string input)
    {
        Assert.False(input.IsDomainName());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsDomainName_NullOrWhitespace_ReturnsFalse(string? input)
    {
        Assert.False(input!.IsDomainName());
    }

    [Fact]
    public void IsDomainName_InvalidUrl_ReturnsFalse()
    {
        Assert.False("http://".IsDomainName());
    }
}
