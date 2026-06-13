using DataGateVPNBot.Extensions;
using Xunit;

namespace DataGateVPNBot.Tests;

public class OpenVpnProfileUrlsTests
{
    private const string Host = "tgbot.datagateapp.com";
    private const string Token = "abc+def/xyz";

    [Fact]
    public void BuildConnectImportUri_UsesConfiguredHostAndEscapesToken()
    {
        var uri = OpenVpnProfileUrls.BuildConnectImportUri(Host, Token);

        Assert.Equal(
            "openvpn://import-profile/https://tgbot.datagateapp.com/DownloadByToken?token=abc%2Bdef%2Fxyz",
            uri);
    }

    [Fact]
    public void BuildProfilePageUrl_MatchesOvpnFileServicePattern()
    {
        var url = OpenVpnProfileUrls.BuildProfilePageUrl($"https://{Host}/", Token);

        Assert.Equal(
            "https://tgbot.datagateapp.com/openvpn-api/profile?token=abc%2Bdef%2Fxyz",
            url);
    }

    [Fact]
    public void ToHttpsBase_EmptyHost_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => OpenVpnProfileUrls.ToHttpsBase("  "));
    }
}
