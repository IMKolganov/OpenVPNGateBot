using DataGateVPNBot.Extensions;
using Xunit;

namespace DataGateVPNBot.Tests;

public class WebhookUrlBuilderTests
{
    [Theory]
    [InlineData("tgbot.datagateapp.com", 443, "https://tgbot.datagateapp.com/api/bot")]
    [InlineData("example.com", 443, "https://example.com/api/bot")]
    [InlineData("host", 5050, "https://host:5050/api/bot")]
    [InlineData("foo.com", 80, "https://foo.com:80/api/bot")]
    public void Build_ReturnsExpectedUrl(string host, int port, string expected)
    {
        var result = WebhookUrlBuilder.Build(host, port);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Build_NullHost_Throws()
    {
        Assert.Throws<ArgumentException>(() => WebhookUrlBuilder.Build(null!, 443));
    }

    [Fact]
    public void Build_EmptyHost_Throws()
    {
        Assert.Throws<ArgumentException>(() => WebhookUrlBuilder.Build("", 443));
        Assert.Throws<ArgumentException>(() => WebhookUrlBuilder.Build("   ", 443));
    }
}
